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
    private float _speed = 1.0f;

    /// <summary>Sự kiện khi trạng thái thay đổi (cho UI cập nhật).</summary>
    public event Action<NarrationState, Poi?>? StateChanged;

    public NarrationState CurrentState => _state;
    public Poi? CurrentPoi => _currentPoi;
    
    /// <summary>Tốc độ phát audio: 0.75x, 1.0x, 1.25x, 1.5x</summary>
    public float Speed
    {
        get => _speed;
        set
        {
            _speed = value;
            // Apply to audio player immediately if playing
            _audioPlayer.Speed = value;
            // Apply to TTS if available
            if (_ttsService is TtsService_Android androidTts)
            {
                androidTts.SetSpeed(value);
            }
            Console.WriteLine($"[Narration] ⚡ Speed set to {value}x");
        }
    }

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
        
        // 0: Ưu tiên cao nhất - TTS script từ database (nếu có)
        var ttsScript = await _databaseService.GetPoiTtsScriptAsync(poi.Id, lang);
        if (!string.IsNullOrWhiteSpace(ttsScript))
        {
            _currentAudioSource = "TTS-DB";
            Console.WriteLine($"[Narration] 🔊 Phát TTS từ DB: \"{ttsScript.Substring(0, Math.Min(50, ttsScript.Length))}...\" ({lang})");
            await _ttsService.SpeakAsync(ttsScript, lang);
            return;
        }
        
        bool playedFile = false;
        // 1: File audio MP3 đã cache trên thiết bị (Chỉ dùng khi ngôn ngữ là vi)
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
    
    /// <summary>Set language for TTS service dynamically</summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        if (_ttsService is TtsService_Android androidTts)
        {
            await androidTts.SetLanguageAsync(languageCode);
        }
    }
    
    /// <summary>Play specific POI with specified language</summary>
    public async Task PlayPoiAsync(Poi poi, string languageCode)
    {
        _currentPoi = poi;
        _currentTriggerType = "Manual";
        SetState(NarrationState.Playing);

#if ANDROID
        RequestAudioFocus();
#endif

        bool playedFile = false;
        
        // 0: Ưu tiên cao nhất - TTS script từ database (nếu có)
        var ttsScript = await _databaseService.GetPoiTtsScriptAsync(poi.Id, languageCode);
        if (!string.IsNullOrWhiteSpace(ttsScript))
        {
            _currentAudioSource = "TTS-DB";
            Console.WriteLine($"[Narration] Phát TTS từ DB: \"{ttsScript.Substring(0, Math.Min(50, ttsScript.Length))}...\" ({languageCode})");
            await _ttsService.SpeakAsync(ttsScript, languageCode);
            return;
        }
        
        // 1: File audio MP3 cache (Chí dùng khi ngôn ngữ là vi)
        if (languageCode == "vi" && !string.IsNullOrEmpty(poi.AudioLocalPath) && File.Exists(poi.AudioLocalPath))
        {
            _currentAudioSource = "AudioFile";
            Console.WriteLine($"[Narration] Phát Audio File: {Path.GetFileName(poi.AudioLocalPath)}");
            await _audioPlayer.PlayAsync(poi.AudioLocalPath);
            playedFile = true;
        }

        if (!playedFile)
        {
            // Fallback: TTS from localized description
            _currentAudioSource = "TTS";
            var localizedDesc = languageCode switch {
                "en" => poi.DescriptionEn,
                "zh" => poi.DescriptionZh,
                "ko" => poi.DescriptionKo,
                "ja" => poi.DescriptionJa,
                "fr" => poi.DescriptionFr,
                _ => poi.Description
            } ?? poi.Description;

            var speechText = $"{poi.Title}. {localizedDesc}";
            Console.WriteLine($"[Narration] Phát TTS: \"{speechText}\" ({languageCode})");
            await _ttsService.SpeakAsync(speechText, languageCode);
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
    private async void OnPlaybackCompleted()
    {
        if (_state != NarrationState.Playing) return; // Tránh gọi trùng

        try
        {
            var historyTask = SavePlaybackHistoryAsync();
            await historyTask; // Wait for completion to ensure data is saved
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Narration] Error saving playback history: {ex.Message}");
        }
        Console.WriteLine($"[Narration] ✅ Phát xong POI \"{_currentPoi?.Title}\" → COOLDOWN");
        SetState(NarrationState.Cooldown);

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(10));
            if (_state == NarrationState.Cooldown)
            {
                SetState(NarrationState.Idle);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Narration] Error in cooldown transition: {ex.Message}");
        }
        _currentPoi = null;
        Console.WriteLine("[Narration] 🔄 Cooldown xong → IDLE, sẵn sàng nhận trigger mới");
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
            Console.WriteLine($"[Narration] Stack trace: {ex.StackTrace}");
            
            if (_currentPoi != null)
            {
                Console.WriteLine($"[Narration] Failed POI ID: {_currentPoi.Id}, Title: {_currentPoi.Title}");
            }
            
            // Handle specific error types
            if (ex is SQLite.SQLiteException sqliteEx)
            {
                Console.WriteLine($"[Narration] SQLite error: {sqliteEx.Result}");
                Console.WriteLine($"[Narration] Database may be locked or corrupted");
            }
            else if (ex is System.IO.IOException ioEx)
            {
                Console.WriteLine($"[Narration] IO error saving history: {ioEx.Message}");
                Console.WriteLine($"[Narration] Check storage space and permissions");
            }
            else if (ex is InvalidOperationException invalidEx)
            {
                Console.WriteLine($"[Narration] Invalid operation saving history: {invalidEx.Message}");
                Console.WriteLine($"[Narration] Database may not be initialized properly");
            }
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
