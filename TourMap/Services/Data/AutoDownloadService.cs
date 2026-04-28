namespace TourMap.Services;

/// <summary>
/// Tự động tải audio khi có WiFi và user đang trong tour
/// Option D: Thay thế tab Offline bằng auto-download thông minh
/// </summary>
public class AutoDownloadService
{
    private readonly ILoggerService _logger;
    private readonly DatabaseService _db;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _downloadLock = new(1, 1);
    private CancellationTokenSource? _currentCts;

    // Settings keys
    private const string AutoDownloadEnabledKey = "AutoDownloadEnabled";
    private const string DownloadOnWifiOnlyKey = "DownloadOnWifiOnly";

    public AutoDownloadService(
        ILoggerService logger,
        DatabaseService db,
        HttpClient httpClient)
    {
        _logger = logger;
        _db = db;
        _httpClient = httpClient;

        // Mặc định: Bật auto-download, chỉ tải khi có WiFi
        if (!Preferences.ContainsKey(AutoDownloadEnabledKey))
            Preferences.Set(AutoDownloadEnabledKey, true);
        if (!Preferences.ContainsKey(DownloadOnWifiOnlyKey))
            Preferences.Set(DownloadOnWifiOnlyKey, true);
    }

    /// <summary>
    /// Kiểm tra setting auto-download có bật không
    /// </summary>
    public bool IsAutoDownloadEnabled
    {
        get => Preferences.Get(AutoDownloadEnabledKey, true);
        set => Preferences.Set(AutoDownloadEnabledKey, value);
    }

    /// <summary>
    /// Chỉ tải khi có WiFi (tiết kiệm data di động)
    /// </summary>
    public bool DownloadOnWifiOnly
    {
        get => Preferences.Get(DownloadOnWifiOnlyKey, true);
        set => Preferences.Set(DownloadOnWifiOnlyKey, value);
    }

    /// <summary>
    /// Kiểm tra điều kiện mạng có phù hợp để auto-download không
    /// </summary>
    public bool ShouldAutoDownload()
    {
        if (!IsAutoDownloadEnabled) return false;

        var current = Connectivity.NetworkAccess;
        var profiles = Connectivity.ConnectionProfiles;

        bool hasInternet = current == NetworkAccess.Internet;
        bool isWifi = profiles.Contains(ConnectionProfile.WiFi);

        if (!hasInternet) return false;

        // Nếu setting chỉ tải WiFi mà đang dùng mobile data → skip
        if (DownloadOnWifiOnly && !isWifi) return false;

        return true;
    }

    /// <summary>
    /// Tải audio của POI nếu chưa có (gọi khi user vào POI detail hoặc geofence trigger)
    /// </summary>
    public async Task EnsurePoiAudioDownloadedAsync(string poiId, string? audioUrl, string? languageCode = null)
    {
        if (!ShouldAutoDownload()) return;
        if (string.IsNullOrEmpty(audioUrl)) return;

        // Kiểm tra file đã tải chưa
        var localPath = GetAudioLocalPath(poiId, languageCode);
        if (File.Exists(localPath))
        {
            _logger.LogDebug("Audio already exists for POI {PoiId}, skipping download", poiId);
            return;
        }

        await _downloadLock.WaitAsync();
        try
        {
            // Double-check sau khi lock
            if (File.Exists(localPath)) return;

            _currentCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            
            _logger.LogInformation("Auto-downloading audio for POI {PoiId} from {Url}", poiId, audioUrl);
            
            await DownloadAudioAsync(audioUrl, localPath, _currentCts.Token);
            
            _logger.LogInformation("Successfully downloaded audio for POI {PoiId}", poiId);
            
            // Cập nhật last sync time
            Preferences.Set("LastOfflineSync", DateTime.UtcNow);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Download cancelled for POI {PoiId}", poiId);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to download audio for POI {poiId}", ex);
        }
        finally
        {
            _downloadLock.Release();
            _currentCts?.Dispose();
            _currentCts = null;
        }
    }

    /// <summary>
    /// Tải toàn bộ audio của tất cả POIs (gọi khi user bật setting hoặc vào tour)
    /// </summary>
    public async Task DownloadAllPoiAudiosAsync(IProgress<double>? progress = null)
    {
        if (!ShouldAutoDownload())
        {
            _logger.LogWarning("Auto-download conditions not met (WiFi/Data requirements)");
            return;
        }

        var pois = await _db.GetPoisAsync();
        var missingAudios = pois
            .Where(p => !string.IsNullOrEmpty(p.AudioUrl) && !File.Exists(GetAudioLocalPath(p.Id)))
            .ToList();

        if (missingAudios.Count == 0)
        {
            _logger.LogInformation("All POI audios are already downloaded");
            return;
        }

        _logger.LogInformation("Starting batch download for {Count} POIs", missingAudios.Count);
        
        int completed = 0;
        foreach (var poi in missingAudios)
        {
            if (!ShouldAutoDownload())
            {
                _logger.LogWarning("Network conditions changed, stopping batch download");
                break;
            }

            await EnsurePoiAudioDownloadedAsync(poi.Id, poi.AudioUrl);
            completed++;
            progress?.Report((double)completed / missingAudios.Count);
        }

        _logger.LogInformation("Batch download completed: {Completed}/{Total}", completed, missingAudios.Count);
    }

    /// <summary>
    /// Xóa tất cả audio đã tải (khi user muốn giải phóng dung lượng)
    /// </summary>
    public Task DeleteAllDownloadsAsync()
    {
        return Task.Run(() =>
        {
            try
            {
                var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
                if (Directory.Exists(audioFolder))
                {
                    Directory.Delete(audioFolder, true);
                    _logger.LogInformation("Deleted all offline audio files");
                }
                Preferences.Remove("LastOfflineSync");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to delete downloads", ex);
            }
        });
    }

    /// <summary>
    /// Lấy dung lượng đã dùng cho offline audio
    /// </summary>
    public long GetOfflineStorageBytes()
    {
        var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
        if (!Directory.Exists(audioFolder)) return 0;

        return Directory.GetFiles(audioFolder)
            .Sum(f => new FileInfo(f).Length);
    }

    /// <summary>
    /// Lấy đường dẫn local cho audio file
    /// </summary>
    public static string GetAudioLocalPath(string poiId, string? languageCode = null)
    {
        var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
        Directory.CreateDirectory(audioFolder);
        
        var langSuffix = string.IsNullOrEmpty(languageCode) ? "" : $"_{languageCode}";
        var fileName = $"{poiId}{langSuffix}.mp3";
        return Path.Combine(audioFolder, fileName);
    }

    /// <summary>
    /// Kiểm tra audio đã tải chưa
    /// </summary>
    public static bool IsAudioDownloaded(string poiId, string? languageCode = null)
    {
        return File.Exists(GetAudioLocalPath(poiId, languageCode));
    }

    private async Task DownloadAudioAsync(string url, string localPath, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var tempPath = localPath + ".tmp";
        try
        {
            await using var fileStream = File.Create(tempPath);
            await response.Content.CopyToAsync(fileStream, ct);
            await fileStream.FlushAsync(ct);
            
            // Atomic move
            if (File.Exists(localPath))
                File.Delete(localPath);
            File.Move(tempPath, localPath);
        }
        catch
        {
            // Cleanup temp file on error
            if (File.Exists(tempPath))
                File.Delete(tempPath);
            throw;
        }
    }

    /// <summary>
    /// Hủy download hiện tại đang chạy
    /// </summary>
    public void CancelCurrentDownload()
    {
        _currentCts?.Cancel();
    }
}
