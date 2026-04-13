namespace TourMap.Services;

/// <summary>
/// Text-to-Speech service interface — phát giọng đọc từ văn bản.
/// </summary>
public interface ITtsService
{
    /// <summary>Phát giọng đọc văn bản. Trả về khi đọc xong.</summary>
    Task SpeakAsync(string text, string langCode = "vi");

    /// <summary>Dừng phát ngay lập tức.</summary>
    void Stop();

    /// <summary>Đang phát hay không.</summary>
    bool IsSpeaking { get; }

    /// <summary>Sự kiện khi đọc xong 1 đoạn.</summary>
    event Action? SpeechCompleted;
}
