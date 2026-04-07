using Plugin.Maui.Audio;

namespace TourMap.Services;

/// <summary>
/// Audio Player dùng Plugin.Maui.Audio — phát file MP3 từ local storage.
/// </summary>
public class AudioPlayerService : IAudioPlayerService, IDisposable
{
    private IAudioPlayer? _player;
    private bool _disposed = false;
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public event Action? AudioCompleted;

    public async Task PlayAsync(string filePath)
    {
        Stop(); // Dừng audio cũ nếu đang phát

        try
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[AudioPlayer] ❌ File không tồn tại: {filePath}");
                return;
            }

            var stream = File.OpenRead(filePath);
            _player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(stream);
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();

            Console.WriteLine($"[AudioPlayer] 🔊 Đang phát: {Path.GetFileName(filePath)}");

            // Đợi cho đến khi phát xong
            while (_player?.IsPlaying == true)
            {
                await Task.Delay(250);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioPlayer] ❌ Lỗi phát audio: {ex.Message}");
            Console.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
            
            // Handle specific error types
            if (ex is FileNotFoundException fileEx)
            {
                Console.WriteLine($"[AudioPlayer] Audio file not found: {fileEx.FileName}");
                Console.WriteLine($"[AudioPlayer] Check if audio file was downloaded successfully");
            }
            else if (ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"[AudioPlayer] Permission denied accessing audio file");
                Console.WriteLine($"[AudioPlayer] Check app storage permissions");
            }
            else if (ex is System.PlatformNotSupportedException)
            {
                Console.WriteLine($"[AudioPlayer] Audio format not supported on this platform");
            }
            else if (ex is InvalidOperationException invalidEx)
            {
                Console.WriteLine($"[AudioPlayer] Invalid audio operation: {invalidEx.Message}");
                Console.WriteLine($"[AudioPlayer] Audio player may be in invalid state");
            }
            
            // Notify completion even on error to prevent UI hanging
            AudioCompleted?.Invoke();
        }
    }

    public void Stop()
    {
        if (_player?.IsPlaying == true)
        {
            _player.Stop();
        }
        DisposePlayer();
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        Console.WriteLine("[AudioPlayer] ✅ Phát xong.");
        MainThread.BeginInvokeOnMainThread(() => AudioCompleted?.Invoke());
    }

    private void DisposePlayer()
    {
        if (_player != null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
            _player.Dispose();
            _player = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        Stop();
        _disposed = true;
    }
}
