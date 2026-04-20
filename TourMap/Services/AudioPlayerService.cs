using Plugin.Maui.Audio;

namespace TourMap.Services;

/// <summary>
/// Audio Player dùng Plugin.Maui.Audio — phát file MP3 từ local storage.
/// </summary>
public class AudioPlayerService : IAudioPlayerService, IDisposable
{
    private IAudioPlayer? _player;
    private FileStream? _audioStream;
    private bool _disposed = false;
    private float _speed = 1.0f;
    
    public bool IsPlaying => _player?.IsPlaying ?? false;
    
    /// <summary>Tốc độ phát: 0.75x, 1.0x, 1.25x, 1.5x</summary>
    public float Speed 
    { 
        get => (float)(_player?.Speed ?? _speed);
        set
        {
            _speed = value;
            if (_player != null)
            {
                _player.Speed = value;
                Console.WriteLine($"[AudioPlayer] ⚡ Speed set to {value}x");
            }
        }
    }
    
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

            _audioStream = File.OpenRead(filePath);
            _player = Plugin.Maui.Audio.AudioManager.Current.CreatePlayer(_audioStream);
            
            // Apply current speed setting
            _player.Speed = _speed;

            // Use TaskCompletionSource instead of busy-wait polling
            var tcs = new TaskCompletionSource<bool>();

            void OnEnded(object? s, EventArgs e)
            {
                if (_player != null)
                    _player.PlaybackEnded -= OnEnded;
                tcs.TrySetResult(true);
                MainThread.BeginInvokeOnMainThread(() => AudioCompleted?.Invoke());
            }

            _player.PlaybackEnded += OnEnded;
            _player.Play();

            Console.WriteLine($"[AudioPlayer] 🔊 Đang phát: {Path.GetFileName(filePath)}");

            // Đợi non-blocking cho đến khi phát xong
            await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AudioPlayer] ❌ Lỗi phát audio: {ex.Message}");
            Console.WriteLine($"[AudioPlayer] Stack trace: {ex.StackTrace}");
            
            // Handle specific error types
            if (ex is FileNotFoundException fileEx)
            {
                Console.WriteLine($"[AudioPlayer] Audio file not found: {fileEx.FileName}");
            }
            else if (ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"[AudioPlayer] Permission denied accessing audio file");
            }
            else if (ex is System.PlatformNotSupportedException)
            {
                Console.WriteLine($"[AudioPlayer] Audio format not supported on this platform");
            }
            else if (ex is InvalidOperationException invalidEx)
            {
                Console.WriteLine($"[AudioPlayer] Invalid audio operation: {invalidEx.Message}");
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

    private void DisposePlayer()
    {
        if (_player != null)
        {
            _player.Dispose();
            _player = null;
        }
        if (_audioStream != null)
        {
            _audioStream.Dispose();
            _audioStream = null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        Stop();
        _disposed = true;
    }
}
