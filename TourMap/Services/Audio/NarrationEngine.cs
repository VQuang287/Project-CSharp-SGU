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
public class NarrationEngine : IDisposable
{
    private readonly ITtsService _ttsService;
    private readonly IAudioPlayerService _audioPlayer;
    private readonly DatabaseService _databaseService;
    private NarrationState _state = NarrationState.Idle;
    private Poi? _currentPoi;
    private string _currentTriggerType = "Unknown";
    private string _currentAudioSource = "TTS";
    private float _speed = 1.0f;
    private CancellationTokenSource? _cooldownCts;
    private bool _disposed;

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

    /// <summary>Lấy đường dẫn file audio local theo ngôn ngữ.</summary>
    private static string? GetAudioLocalPath(Poi poi, string lang) => lang switch {
        "en" => poi.AudioLocalPathEn,
        "zh" => poi.AudioLocalPathZh,
        "ko" => poi.AudioLocalPathKo,
        "ja" => poi.AudioLocalPathJa,
        "fr" => poi.AudioLocalPathFr,
        _ => poi.AudioLocalPath
    };

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
        
        // Debug: Log thông tin POI và audio paths
        Console.WriteLine($"[Narration] OnPOITriggered: POI={poi.Title}, Lang={lang}");
        Console.WriteLine($"[Narration]   AudioLocalPath={poi.AudioLocalPath}");
        Console.WriteLine($"[Narration]   AudioLocalPathEn={poi.AudioLocalPathEn}");
        Console.WriteLine($"[Narration]   AudioLocalPathZh={poi.AudioLocalPathZh}");
        Console.WriteLine($"[Narration]   AudioLocalPathKo={poi.AudioLocalPathKo}");
        Console.WriteLine($"[Narration]   AudioLocalPathJa={poi.AudioLocalPathJa}");
        Console.WriteLine($"[Narration]   AudioLocalPathFr={poi.AudioLocalPathFr}");
        
        // Ưu tiên 1: File audio MP3 đã download về local (từ Admin TTS API)
        var audioPath = GetAudioLocalPath(poi, lang);
        Console.WriteLine($"[Narration] GetAudioLocalPath({lang}) = {audioPath}");
        if (!string.IsNullOrEmpty(audioPath))
        {
            bool fileExists = File.Exists(audioPath);
            Console.WriteLine($"[Narration] File.Exists({audioPath}) = {fileExists}");
            if (fileExists)
            {
                _currentAudioSource = "AI-Audio-File";
                Console.WriteLine($"[Narration] 🔊 Phát AI Audio: {Path.GetFileName(audioPath)} ({lang})");
                await _audioPlayer.PlayAsync(audioPath);
                return;
            }
        }

        // Ưu tiên 2: TTS script từ database (nếu có)
        var ttsScript = await _databaseService.GetPoiTtsScriptAsync(poi.Id, lang);
        if (!string.IsNullOrWhiteSpace(ttsScript))
        {
            _currentAudioSource = "TTS-DB";
            Console.WriteLine($"[Narration] 🔊 Phát TTS từ DB: \"{ttsScript.Substring(0, Math.Min(50, ttsScript.Length))}...\" ({lang})");
            await _ttsService.SpeakAsync(ttsScript, lang);
            return;
        }

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

    /// <summary>Dừng phát thủ công — hoạt động ở MỌI trạng thái.</summary>
    public void Stop()
    {
        // Cancel cooldown timer nếu đang chờ
        _cooldownCts?.Cancel();

        if (_state == NarrationState.Playing)
        {
            StopCurrent();
        }

        _currentPoi = null;
        SetState(NarrationState.Idle);
    }
    
    /// <summary>Set language for TTS service dynamically</summary>
    public async Task SetLanguageAsync(string languageCode)
    {
        if (_ttsService is TtsService_Android androidTts)
        {
            await androidTts.SetLanguageAsync(languageCode);
        }
    }
    
    /// <summary>Play specific POI with specified language (dùng cho manual play từ UI).</summary>
    public async Task PlayPoiAsync(Poi poi, string languageCode)
    {
        // Force-reset: dừng mọi thứ đang chạy (bao gồm cooldown)
        _cooldownCts?.Cancel();
        StopCurrent();

        _currentPoi = poi;
        _currentTriggerType = "Manual";
        SetState(NarrationState.Playing);

#if ANDROID
        RequestAudioFocus();
#endif

        // Ưu tiên 1: File audio MP3 đã download về local (từ Admin TTS API)
        var audioPath = GetAudioLocalPath(poi, languageCode);
        if (!string.IsNullOrEmpty(audioPath) && File.Exists(audioPath))
        {
            _currentAudioSource = "AI-Audio-File";
            Console.WriteLine($"[Narration] Phát AI Audio: {Path.GetFileName(audioPath)} ({languageCode})");
            await _audioPlayer.PlayAsync(audioPath);
            return;
        }
        
        // Ưu tiên 2: TTS script từ database (nếu có)
        var ttsScript = await _databaseService.GetPoiTtsScriptAsync(poi.Id, languageCode);
        if (!string.IsNullOrWhiteSpace(ttsScript))
        {
            _currentAudioSource = "TTS-DB";
            Console.WriteLine($"[Narration] Phát TTS từ DB: \"{ttsScript.Substring(0, Math.Min(50, ttsScript.Length))}...\" ({languageCode})");
            await _ttsService.SpeakAsync(ttsScript, languageCode);
            return;
        }

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
        // Guard: chỉ xử lý khi đang thực sự Playing
        if (_disposed || _state != NarrationState.Playing) return;

        // Delegate sang async method với exception guard
        _ = OnPlaybackCompletedCoreAsync();
    }

    private async Task OnPlaybackCompletedCoreAsync()
    {
        var completedPoi = _currentPoi;

        try
        {
            await SavePlaybackHistoryAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Narration] Error saving playback history: {ex.Message}");
        }

        Console.WriteLine($"[Narration] ✅ Phát xong POI \"{completedPoi?.Title}\" → COOLDOWN");
        SetState(NarrationState.Cooldown);

        try
        {
            // Cancellable cooldown delay — bị hủy khi user bấm Play/Stop
            _cooldownCts?.Cancel();
            _cooldownCts = new CancellationTokenSource();
            await Task.Delay(TimeSpan.FromSeconds(10), _cooldownCts.Token);

            // Chỉ chuyển Idle nếu vẫn đang Cooldown (chưa ai can thiệp)
            if (_state == NarrationState.Cooldown)
            {
                _currentPoi = null;
                SetState(NarrationState.Idle);
                Console.WriteLine("[Narration] 🔄 Cooldown xong → IDLE, sẵn sàng nhận trigger mới");
            }
        }
        catch (TaskCanceledException)
        {
            // Cooldown cancelled by user Play/Stop — expected, do nothing
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Narration] Error in cooldown transition: {ex.Message}");
        }
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

    // SYS-H01 + SYS-W02 fix: Deterministic cleanup
    public void Dispose()
    {
        if (_disposed) return;

        _ttsService.SpeechCompleted -= OnPlaybackCompleted;
        _audioPlayer.AudioCompleted -= OnPlaybackCompleted;
        _cooldownCts?.Cancel();
        _cooldownCts?.Dispose();
        StopCurrent();
        _disposed = true;
    }
}
