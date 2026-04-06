using Plugin.Maui.Audio;

namespace TourMap.Services;

/// <summary>
/// Audio Player dùng Plugin.Maui.Audio — phát file MP3 từ local storage.
/// </summary>
public class AudioPlayerService : IAudioPlayerService, IDisposable
{
    private IAudioPlayer? _player;
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
            _player = AudioManager.Current.CreatePlayer(stream);
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
        Stop();
    }
}
