using AVFoundation;
using Foundation;
using TourMap.Services;

namespace TourMap;

/// <summary>
/// iOS native TextToSpeech — dùng AVSpeechSynthesizer.
/// </summary>
public class TtsService_iOS : ITtsService
{
    private readonly AVSpeechSynthesizer _synthesizer;
    private readonly object _lock = new();
    private TaskCompletionSource<bool>? _speakTcs;

    public bool IsSpeaking { get; private set; }
    public event Action? SpeechCompleted;

    public TtsService_iOS()
    {
        _synthesizer = new AVSpeechSynthesizer();
        _synthesizer.DidFinishSpeechUtterance += OnDidFinishSpeechUtterance;
        Console.WriteLine("[TTS-iOS] ✅ AVSpeechSynthesizer initialized");
    }

    public async Task SpeakAsync(string text, string langCode = "vi")
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine("[TTS-iOS] ⚠️ SpeakAsync skipped: empty text");
            return;
        }

        // Dừng phát cũ nếu đang nói
        Stop();

        IsSpeaking = true;
        var newTcs = new TaskCompletionSource<bool>();
        lock (_lock)
        {
            _speakTcs = newTcs;
        }

        // Chuyển mã ngôn ngữ sang iOS locale
        var voiceLanguage = LangCodeToVoiceLanguage(langCode);

        Console.WriteLine($"[TTS-iOS] 🔊 SpeakAsync: lang={langCode}, voice={voiceLanguage}, text=\"{text.Substring(0, Math.Min(50, text.Length))}...\"");

        // Chạy trên MainThread vì AVSpeechSynthesizer cần main queue
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            try
            {
                var utterance = new AVSpeechUtterance(text);
                utterance.Voice = AVSpeechSynthesisVoice.FromLanguage(voiceLanguage);
                utterance.Rate = AVSpeechUtterance.DefaultSpeechRate; // 0.5 = normal
                utterance.PitchMultiplier = 1.0f;
                utterance.Volume = 1.0f;

                _synthesizer.SpeakUtterance(utterance);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TTS-iOS] Error speaking: {ex.Message}");
                _speakTcs?.TrySetResult(false);
            }
        });

        // Đợi phát xong
        await newTcs.Task;
    }

    public void Stop()
    {
        if (_synthesizer.Speaking)
        {
            _synthesizer.StopSpeaking(AVSpeechBoundary.Immediate);
        }
        IsSpeaking = false;
        lock (_lock)
        {
            _speakTcs?.TrySetResult(false);
            _speakTcs = null;
        }
    }

    /// <summary>
    /// Chuyển mã ngôn ngữ sang iOS BCP-47 language code.
    /// </summary>
    private static string LangCodeToVoiceLanguage(string langCode) => langCode.ToLowerInvariant() switch
    {
        "en" => "en-US",
        "zh" => "zh-CN",
        "ko" => "ko-KR",
        "ja" => "ja-JP",
        "fr" => "fr-FR",
        _ => "vi-VN"  // mặc định tiếng Việt
    };

    private void OnDidFinishSpeechUtterance(object? sender, AVSpeechSynthesizerUteranceEventArgs e)
    {
        Console.WriteLine("[TTS-iOS] ✅ Speech completed");
        IsSpeaking = false;
        TaskCompletionSource<bool>? tcs;
        lock (_lock)
        {
            tcs = _speakTcs;
            _speakTcs = null;
        }
        tcs?.TrySetResult(true);
        
        // Fire event trên main thread
        MainThread.BeginInvokeOnMainThread(() => SpeechCompleted?.Invoke());
    }
}
