namespace TourMap.Services;

/// <summary>
/// Audio Player service — phát file MP3/M4A từ local hoặc URL.
/// </summary>
public interface IAudioPlayerService
{
    /// <summary>Phát file audio từ đường dẫn local.</summary>
    Task PlayAsync(string filePath);

    /// <summary>Dừng phát.</summary>
    void Stop();

    /// <summary>Đang phát hay không.</summary>
    bool IsPlaying { get; }

    /// <summary>Sự kiện khi phát xong.</summary>
    event Action? AudioCompleted;
}
