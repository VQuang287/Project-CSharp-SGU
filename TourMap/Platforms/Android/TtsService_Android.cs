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

    public bool IsSpeaking { get; private set; }
    public event Action? SpeechCompleted;

    public TtsService_Android()
    {
        // Khởi tạo engine TTS từ Android platform
        _tts = new AndroidTts(Android.App.Application.Context, this);
    }

    /// <summary>Callback khi TTS engine khởi tạo xong.</summary>
    public void OnInit(OperationResult status)
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
            Console.WriteLine($"[TTS] 🔊 Bắt đầu đọc...");
        }

        public override void OnDone(string? utteranceId)
        {
            Console.WriteLine($"[TTS] ✅ Đọc xong.");
            _service.OnUtteranceDone();
        }

        [Obsolete("Overrides obsolete member")]
        public override void OnError(string? utteranceId)
        {
            Console.WriteLine($"[TTS] ❌ Lỗi khi đọc.");
            _service.OnUtteranceDone(); // Vẫn giải phóng lock
        }
    }
}
