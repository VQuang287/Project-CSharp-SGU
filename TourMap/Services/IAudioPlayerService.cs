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

    /// <summary>Tốc độ phát (0.75 = 75%, 1.0 = 100%, 1.5 = 150%).</summary>
    float Speed { get; set; }

    /// <summary>Sự kiện khi phát xong.</summary>
    event Action? AudioCompleted;
}
