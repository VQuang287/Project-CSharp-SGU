using TourMap.Models;

#if ANDROID
using Android.Media;
using Android.Content;
#endif

namespace TourMap.Services;

/// <summary>
/// Trạng thái Narration Engine (theo PRD).
/// </summary>
public enum NarrationState
{
    /// <summary>Không phát gì — sẵn sàng nhận trigger.</summary>
    Idle,

    /// <summary>Đang phát TTS/audio cho 1 POI.</summary>
    Playing,

    /// <summary>Đã phát xong — đang trong thời gian chờ.</summary>
    Cooldown
}

/// <summary>
/// Narration Engine — quản lý vòng đời audio.
/// Nhận tín hiệu từ Geofence → phát Audio file hoặc TTS → chuyển trạng thái.
/// Ưu tiên: Audio file MP3 (nếu có) → fallback TTS.
/// </summary>
public class NarrationEngine
{
    private readonly ITtsService _ttsService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly DatabaseService _databaseService;
    private NarrationState _state = NarrationState.Idle;
    private Poi? _currentPoi;
    private string _currentTriggerType = "Unknown";
    private string _currentAudioSource = "TTS";

    /// <summary>Sự kiện khi trạng thái thay đổi (cho UI cập nhật).</summary>
    public event Action<NarrationState, Poi?>? StateChanged;

    public NarrationState CurrentState => _state;
    public Poi? CurrentPoi => _currentPoi;

    public NarrationEngine(
        ITtsService ttsService,
        IAudioPlayerService audioPlayer,
        DatabaseService databaseService)
    {
        _ttsService = ttsService;
        _audioPlayer = audioPlayer;
        _databaseService = databaseService;

        _ttsService.SpeechCompleted += OnPlaybackCompleted;
        _audioPlayer.AudioCompleted += OnPlaybackCompleted;

#if ANDROID
        // Đăng ký AudioFocus listener để tự động Pause khi có cuộc gọi / ứng dụng khác lấy focus
        InitAudioFocus();
#endif
    }

    /// <summary>
    /// Gọi khi Geofence Engine phát hiện user đi vào vùng POI.
    /// </summary>
    public async Task OnPOITriggeredAsync(Poi poi, string triggerType = "GPS")
    {
        // Nếu đang PLAYING → chỉ chấp nhận POI có priority cao hơn
        if (_state == NarrationState.Playing)
        {
            if (_currentPoi != null && poi.Priority > _currentPoi.Priority)
            {
                Console.WriteLine($"[Narration] ⏩ Ngắt POI \"{_currentPoi.Title}\" " +
                                  $"→ chuyển sang POI ưu tiên cao hơn: \"{poi.Title}\"");
                StopCurrent();
            }
            else
            {
                Console.WriteLine($"[Narration] ⏭️ Đang phát \"{_currentPoi?.Title}\", " +
                                  $"bỏ qua POI \"{poi.Title}\" (priority thấp hơn)");
                return;
            }
        }

        // Nếu đang COOLDOWN → bỏ qua
        if (_state == NarrationState.Cooldown)
        {
            Console.WriteLine($"[Narration] ⏳ Đang cooldown, bỏ qua POI \"{poi.Title}\"");
            return;
        }

        // === Phát audio ===
        _currentPoi = poi;
        _currentTriggerType = triggerType;
        SetState(NarrationState.Playing);

#if ANDROID
        RequestAudioFocus();
#endif

        var lang = LocalizationService.Current.CurrentLanguage;
        
        bool playedFile = false;
        // Ưu tiên 1: File audio MP3 đã cache trên thiết bị (Chỉ dùng khi ngôn ngữ là vi)
        if (lang == "vi" && !string.IsNullOrEmpty(poi.AudioLocalPath) && File.Exists(poi.AudioLocalPath))
        {
            _currentAudioSource = "AudioFile";
            Console.WriteLine($"[Narration] 🔊 Phát Audio File: {Path.GetFileName(poi.AudioLocalPath)}");
            await _audioPlayer.PlayAsync(poi.AudioLocalPath);
            playedFile = true;
        }

        if (!playedFile)
        {
            // Fallback: TTS từ Description được dịch
            _currentAudioSource = "TTS";
            var localizedDesc = lang switch {
                "en" => poi.DescriptionEn,
                "zh" => poi.DescriptionZh,
                "ko" => poi.DescriptionKo,
                "ja" => poi.DescriptionJa,
                "fr" => poi.DescriptionFr,
                _ => poi.Description
            } ?? poi.Description;

            var speechText = $"{poi.Title}. {localizedDesc}";
            Console.WriteLine($"[Narration] 🔊 Phát TTS: \"{speechText}\" ({lang})");
            await _ttsService.SpeakAsync(speechText, lang);
        }
    }

    /// <summary>Dừng phát thủ công.</summary>
    public void Stop()
    {
        if (_state == NarrationState.Playing)
        {
            StopCurrent();
            SetState(NarrationState.Idle);
            _currentPoi = null;
        }
    }

    private void StopCurrent()
    {
        _ttsService.Stop();
        _audioPlayer.Stop();
#if ANDROID
        AbandonAudioFocus();
#endif
    }

    /// <summary>Callback khi audio hoặc TTS phát xong.</summary>
    private void OnPlaybackCompleted()
    {
        if (_state != NarrationState.Playing) return; // Tránh gọi trùng

        _ = SavePlaybackHistoryAsync();
        Console.WriteLine($"[Narration] ✅ Phát xong POI \"{_currentPoi?.Title}\" → COOLDOWN");
        SetState(NarrationState.Cooldown);

        // Sau 10 giây cooldown → quay lại IDLE.
        // Lưu ý: cooldown 10 phút thực sự đã được xử lý bởi GeofenceEngine.
        _ = Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith(_ =>
        {
            if (_state == NarrationState.Cooldown)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    _currentPoi = null;
                    SetState(NarrationState.Idle);
                    Console.WriteLine("[Narration] 🔄 Cooldown xong → IDLE, sẵn sàng nhận trigger mới");
                });
            }
        });
    }

    private void SetState(NarrationState newState)
    {
        _state = newState;
        StateChanged?.Invoke(_state, _currentPoi);
    }

    private async Task SavePlaybackHistoryAsync()
    {
        if (_currentPoi == null) return;

        try
        {
            await _databaseService.AddPlaybackHistoryAsync(new PlaybackHistoryEntry
            {
                PoiId = _currentPoi.Id,
                PoiTitle = _currentPoi.Title,
                TriggerType = _currentTriggerType,
                AudioSource = _currentAudioSource,
                PlayedAtUtc = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Narration] ⚠️ Không lưu được playback history: {ex.Message}");
        }
    }

#if ANDROID
    private AudioFocusRequestClass? _focusRequest;
    private AudioManager? _audioManager;

    private void InitAudioFocus()
    {
        _audioManager = (AudioManager?)Android.App.Application.Context.GetSystemService(Context.AudioService);
    }

    private void RequestAudioFocus()
    {
        if (_audioManager == null) return;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            _focusRequest = new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetOnAudioFocusChangeListener(new AudioFocusListener(this))
                .Build();
            _audioManager.RequestAudioFocus(_focusRequest!);
        }
    }

    private void AbandonAudioFocus()
    {
        if (_audioManager == null || _focusRequest == null) return;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            _audioManager.AbandonAudioFocusRequest(_focusRequest);
        }
    }

    internal void OnAudioFocusLost()
    {
        if (_state == NarrationState.Playing)
        {
            Console.WriteLine("[Narration] ⏸️ AudioFocus mất — tạm dừng audio");
            StopCurrent();
            SetState(NarrationState.Idle);
        }
    }

    private class AudioFocusListener : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        private readonly NarrationEngine _engine;
        public AudioFocusListener(NarrationEngine engine) => _engine = engine;

        public void OnAudioFocusChange(AudioFocus focusChange)
        {
            if (focusChange == AudioFocus.Loss || focusChange == AudioFocus.LossTransient)
            {
                MainThread.BeginInvokeOnMainThread(() => _engine.OnAudioFocusLost());
            }
        }
    }
#endif
}
