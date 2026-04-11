#pragma warning disable CA1422 // 'Locale' is obsolete on Android 36.0+ - Project targets API 31

using Android.Speech.Tts;
using TourMap.Services;
using JavaLocale = Java.Util.Locale;
// Alias để tránh xung đột giữa MAUI TextToSpeech và Android TextToSpeech
using AndroidTts = Android.Speech.Tts.TextToSpeech;

namespace TourMap;

/// <summary>
/// Android native TextToSpeech — phát giọng đọc tiếng Việt.
/// Dùng Android.Speech.Tts.TextToSpeech engine có sẵn trên thiết bị.
/// </summary>
public class TtsService_Android : Java.Lang.Object, ITtsService, AndroidTts.IOnInitListener
{
    private AndroidTts? _tts;
    private bool _initialized;
    private TaskCompletionSource<bool>? _speakTcs;
    private readonly LocalizationService _loc;

    public bool IsSpeaking { get; private set; }
    public event Action? SpeechCompleted;

    public TtsService_Android()
    {
        _loc = LocalizationService.Current;
        _loc.LanguageChanged += OnLanguageChanged;
        
        try
        {
            // Khai báo context và validate
            var context = Android.App.Application.Context 
                ?? throw new InvalidOperationException("Android application context is not available");
            
            // Khai báo engine TTS và catch Java exceptions
            _tts = new AndroidTts(context, this);
            Console.WriteLine("[TTS] TTS engine constructor completed");
        }
        catch (Java.Lang.Exception jex)
        {
            Console.WriteLine($"[TTS] Java exception during TTS initialization: {jex.Message}");
            Console.WriteLine($"[TTS] Java exception type: {jex.GetType().Name}");
            throw new InvalidOperationException("Failed to initialize TTS engine due to Java exception", jex);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Exception during TTS initialization: {ex.Message}");
            Console.WriteLine($"[TTS] Exception type: {ex.GetType().Name}");
            throw;
        }
    }

    private void OnLanguageChanged()
    {
        // Reinitialize TTS with new language
        _initialized = false;
        _ = InitializeTtsAsync();
    }
    
    private async Task InitializeTtsAsync()
    {
        if (_tts == null) return;
        
        try
        {
            var langCode = _loc.CurrentLanguage switch
            {
                "vi" => "vi-VN",
                "en" => "en-US", 
                "zh" => "zh-CN",
                "ko" => "ko-KR",
                "ja" => "ja-JP",
                "fr" => "fr-FR",
                _ => "vi-VN"
            };
            
            var locale = new JavaLocale(langCode);
            var result = _tts.SetLanguage(locale);
            
            Console.WriteLine($"[TTS] Language set to {langCode}, result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Failed to set language: {ex.Message}");
        }
    }

    /// <summary>Callback khi TTS engine khởi tạo xong.</summary>
    public void OnInit(OperationResult status)
    {
        try
        {
            if (status == OperationResult.Success && _tts != null)
            {
                // Mặc định: tiếng Việt
                var result = _tts.SetLanguage(new JavaLocale("vi", "VN"));

                // Nếu thiết bị không hỗ trợ tiếng Việt → fallback sang English
                if (result == LanguageAvailableResult.MissingData ||
                    result == LanguageAvailableResult.NotSupported)
                {
                    _tts.SetLanguage(JavaLocale.Us);
                    Console.WriteLine("[TTS] Tiếng Việt không khả dụng, fallback sang English");
                }

                // Tốc độ đọc bình thường
                _tts.SetSpeechRate(1.0f);
                _tts.SetPitch(1.0f);

                // Lắng nghe sự kiện đọc xong
                _tts.SetOnUtteranceProgressListener(new TtsProgressListener(this));

                _initialized = true;
                Console.WriteLine("[TTS] ✅ Engine khởi tạo thành công");
            }
            else
            {
                Console.WriteLine("[TTS] ❌ Không thể khởi tạo TTS engine");
            }
        }
        catch (Java.Lang.Exception jex)
        {
            Console.WriteLine($"[TTS] Java exception in OnInit: {jex.Message}");
            Console.WriteLine($"[TTS] Java exception type: {jex.GetType().Name}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Exception in OnInit: {ex.Message}");
            Console.WriteLine($"[TTS] Exception type: {ex.GetType().Name}");
        }
    }

    public async Task<bool> SetLanguageAsync(string languageCode)
    {
        if (_tts == null) return false;
        
        try
        {
            var locale = languageCode switch
            {
                "vi" => new JavaLocale("vi", "VN"),
                "en" => JavaLocale.Us,
                "zh" => new JavaLocale("zh", "CN"),
                "ko" => new JavaLocale("ko", "KR"),
                "ja" => new JavaLocale("ja", "JP"),
                "fr" => new JavaLocale("fr", "FR"),
                _ => new JavaLocale("vi", "VN")
            };
            
            var result = _tts.SetLanguage(locale);
            return result == LanguageAvailableResult.Available || 
                   result == LanguageAvailableResult.CountryAvailable;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Set language failed: {ex.Message}");
            return false;
        }
    }

    public async Task SpeakAsync(string text, string langCode = "vi")
    {
        if (!_initialized || _tts == null || string.IsNullOrWhiteSpace(text))
            return;

        var locale = langCode switch {
            "en" => JavaLocale.Us,
            "zh" => JavaLocale.China,
            "ko" => JavaLocale.Korea,
            "ja" => JavaLocale.Japan,
            "fr" => JavaLocale.France,
            _ => new JavaLocale("vi", "VN")
        };
        _tts.SetLanguage(locale);

        // Dừng phát cũ nếu đang nói
        Stop();

        IsSpeaking = true;
        _speakTcs = new TaskCompletionSource<bool>();

        var utteranceId = Guid.NewGuid().ToString();
        var param = new Android.OS.Bundle();
        _tts.Speak(text, QueueMode.Flush, param, utteranceId);

        // Đợi TTS đọc xong
        await _speakTcs.Task;
    }

    public void Stop()
    {
        if (_tts?.IsSpeaking == true)
        {
            _tts.Stop();
        }
        IsSpeaking = false;
        _speakTcs?.TrySetResult(false);
    }

    /// <summary>Set TTS speech rate (speed). 0.75 = 75%, 1.0 = 100%, 1.5 = 150%</summary>
    public void SetSpeed(float speed)
    {
        if (_tts == null) return;
        
        try
        {
            // Android SetSpeechRate: 1.0 = normal, 0.5 = half speed, 2.0 = double speed
            _tts.SetSpeechRate(speed);
            Console.WriteLine($"[TTS] ⚡ Speech rate set to {speed}x");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS] Failed to set speech rate: {ex.Message}");
        }
    }

    internal void OnUtteranceDone()
    {
        IsSpeaking = false;
        _speakTcs?.TrySetResult(true);
        MainThread.BeginInvokeOnMainThread(() => SpeechCompleted?.Invoke());
    }

    /// <summary>Listener theo dõi tiến trình đọc TTS.</summary>
    private class TtsProgressListener : UtteranceProgressListener
    {
        private readonly TtsService_Android _service;

        public TtsProgressListener(TtsService_Android service)
        {
            _service = service;
        }

        public override void OnStart(string? utteranceId)
        {
            try
            {
                Console.WriteLine($"[TTS] Bắt đầu đọc...");
            }
            catch (Java.Lang.Exception jex)
            {
                Console.WriteLine($"[TTS] Java exception in OnStart: {jex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Exception in OnStart: {ex.Message}");
            }
        }

        public override void OnDone(string? utteranceId)
        {
            try
            {
                Console.WriteLine($"[TTS] Đọc xong.");
                _service.OnUtteranceDone();
            }
            catch (Java.Lang.Exception jex)
            {
                Console.WriteLine($"[TTS] Java exception in OnDone: {jex.Message}");
                _service.OnUtteranceDone(); // Still release lock
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Exception in OnDone: {ex.Message}");
                _service.OnUtteranceDone(); // Still release lock
            }
        }

        [Obsolete("Overrides obsolete member")]
        public override void OnError(string? utteranceId)
        {
            try
            {
                Console.WriteLine($"[TTS] Lỗi khi đọc.");
            }
            catch (Java.Lang.Exception jex)
            {
                Console.WriteLine($"[TTS] Java exception in OnError: {jex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS] Exception in OnError: {ex.Message}");
            }
            finally
            {
                _service.OnUtteranceDone(); // Always release lock
            }
        }
    }
}
