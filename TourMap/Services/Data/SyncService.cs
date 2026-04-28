using System.Net.Http.Json;
using System.Text.Json;
using TourMap.Models;

namespace TourMap.Services;

/// <summary>
/// Sync Service — đồng bộ dữ liệu POI từ Admin Server (Backend API) về SQLite local.
/// Gọi API /api/sync/pois → parse JSON → upsert vào local DB.
/// </summary>
public class SyncService
{
    private readonly HttpClient _httpClient;
    private readonly DatabaseService _dbService;
    private readonly AuthService _authService;

    // BUG-W01 fix: Accept IHttpClientFactory via DI to prevent socket exhaustion
    public SyncService(IHttpClientFactory httpClientFactory, DatabaseService dbService, AuthService authService)
    {
        _dbService = dbService;
        _authService = authService;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
    }

    /// <summary>
    /// Đồng bộ toàn bộ POI từ server. Gọi khi mở app (nếu có mạng).
    /// </summary>
    public async Task<bool> SyncPoisFromServerAsync(string serverBaseUrl)
    {
        try
        {
            // Build URL with optional last-sync timestamp
            var lastSync = Preferences.Default.Get<string>("last_sync_time", string.Empty);
            var url = $"{serverBaseUrl.TrimEnd('/')}/api/v1/pois/sync/pois";
            if (!string.IsNullOrEmpty(lastSync))
            {
                url += $"?since={Uri.EscapeDataString(lastSync)}";
            }

            // Use per-request auth header to avoid thread-safety issues (SYS-C03 fix)
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(_authService.CurrentToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
            }

            Console.WriteLine($"[Sync] 🔄 Đang đồng bộ từ: {url}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var pois = ParsePoisFromJson(json);
            if (pois == null || pois.Count == 0)
            {
                Console.WriteLine("[Sync] ⚠️ Không có dữ liệu mới từ Server");
                return true; // Thành công (không có gì để update)
            }

            int count = 0;
            foreach (var dto in pois)
            {
                var poi = new Poi
                {
                    Id = dto.Id ?? Guid.NewGuid().ToString(),
                    Title = dto.Title ?? string.Empty,
                    Description = dto.Description ?? string.Empty,
                    Latitude = dto.Latitude,
                    Longitude = dto.Longitude,
                    RadiusMeters = dto.RadiusMeters > 0 ? dto.RadiusMeters : 50,
                    Priority = dto.Priority,
                    IsActive = true,
                    ImageUrl = dto.ImageUrl,
                    AudioUrl = dto.AudioUrl,
                    MapLink = dto.MapLink,
                    DescriptionEn = dto.DescriptionEn,
                    AudioUrlEn = dto.AudioUrlEn,
                    DescriptionZh = dto.DescriptionZh,
                    AudioUrlZh = dto.AudioUrlZh,
                    DescriptionKo = dto.DescriptionKo,
                    AudioUrlKo = dto.AudioUrlKo,
                    DescriptionJa = dto.DescriptionJa,
                    AudioUrlJa = dto.AudioUrlJa,
                    DescriptionFr = dto.DescriptionFr,
                    AudioUrlFr = dto.AudioUrlFr,
                    TtsScriptVi = dto.TtsScriptVi,
                    TtsScriptEn = dto.TtsScriptEn,
                    TtsScriptZh = dto.TtsScriptZh,
                    TtsScriptKo = dto.TtsScriptKo,
                    TtsScriptJa = dto.TtsScriptJa,
                    TtsScriptFr = dto.TtsScriptFr
                };

                // Tải audio file về local nếu có URL (tiếng Việt)
                if (!string.IsNullOrEmpty(poi.AudioUrl))
                {
                    var localPath = await DownloadAudioAsync(poi.AudioUrl, serverBaseUrl, poi.Id);
                    if (localPath != null)
                        poi.AudioLocalPath = localPath;
                }

                // Tải audio đa ngôn ngữ về local
                Console.WriteLine($"[Sync] Đang tải audio đa ngôn ngữ cho POI: {poi.Title} ({poi.Id})");
                Console.WriteLine($"[Sync]   AudioUrlEn: {poi.AudioUrlEn}");
                Console.WriteLine($"[Sync]   AudioUrlZh: {poi.AudioUrlZh}");
                Console.WriteLine($"[Sync]   AudioUrlKo: {poi.AudioUrlKo}");
                Console.WriteLine($"[Sync]   AudioUrlJa: {poi.AudioUrlJa}");
                Console.WriteLine($"[Sync]   AudioUrlFr: {poi.AudioUrlFr}");
                
                poi.AudioLocalPathEn = await DownloadAudioAsync(poi.AudioUrlEn, serverBaseUrl, $"{poi.Id}_en");
                poi.AudioLocalPathZh = await DownloadAudioAsync(poi.AudioUrlZh, serverBaseUrl, $"{poi.Id}_zh");
                poi.AudioLocalPathKo = await DownloadAudioAsync(poi.AudioUrlKo, serverBaseUrl, $"{poi.Id}_ko");
                poi.AudioLocalPathJa = await DownloadAudioAsync(poi.AudioUrlJa, serverBaseUrl, $"{poi.Id}_ja");
                poi.AudioLocalPathFr = await DownloadAudioAsync(poi.AudioUrlFr, serverBaseUrl, $"{poi.Id}_fr");
                
                Console.WriteLine($"[Sync]   LocalPathEn: {poi.AudioLocalPathEn}");
                Console.WriteLine($"[Sync]   LocalPathZh: {poi.AudioLocalPathZh}");
                Console.WriteLine($"[Sync]   LocalPathKo: {poi.AudioLocalPathKo}");
                Console.WriteLine($"[Sync]   LocalPathJa: {poi.AudioLocalPathJa}");
                Console.WriteLine($"[Sync]   LocalPathFr: {poi.AudioLocalPathFr}");
                
                // Debug: Log TTS scripts
                Console.WriteLine($"[Sync]   TTS VI: {(poi.TtsScriptVi?.Substring(0, Math.Min(30, poi.TtsScriptVi?.Length ?? 0)) ?? "null")}...");
                Console.WriteLine($"[Sync]   TTS EN: {(poi.TtsScriptEn?.Substring(0, Math.Min(30, poi.TtsScriptEn?.Length ?? 0)) ?? "null")}...");

                await _dbService.UpsertPoiAsync(poi);
                count++;
            }

            Console.WriteLine($"[Sync] ✅ Đồng bộ thành công {count} POI");

            // Lưu thời gian sync
            Preferences.Default.Set("last_sync_time", DateTime.UtcNow.ToString("O"));

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sync] ❌ Lỗi đồng bộ: {ex.Message}");
            // Log full exception for debugging
            Console.WriteLine($"[Sync] Stack trace: {ex.StackTrace}");
            
            // Consider different error types for better handling
            if (ex is HttpRequestException httpEx)
            {
                Console.WriteLine($"[Sync] Network error: {httpEx.StatusCode}");
            }
            else if (ex is TaskCanceledException)
            {
                Console.WriteLine($"[Sync] Request timeout");
            }
            
            return false;
        }
    }

    /// <summary>
    /// Tải file audio MP3 từ server về local storage.
    /// </summary>
    private async Task<string?> DownloadAudioAsync(string audioUrl, string serverBaseUrl, string poiId)
    {
        try
        {
            // Nếu audioUrl là relative path → ghép với serverBaseUrl
            var fullUrl = audioUrl.StartsWith("http")
                ? audioUrl
                : $"{serverBaseUrl.TrimEnd('/')}{audioUrl}";

            var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
            Directory.CreateDirectory(audioFolder);

            var extension = Path.GetExtension(audioUrl);
            if (string.IsNullOrEmpty(extension)) extension = ".mp3";
            var localPath = Path.Combine(audioFolder, $"{poiId}{extension}");

            // Nếu file đã tồn tại → bỏ qua (dùng cache)
            if (File.Exists(localPath))
            {
                Console.WriteLine($"[Sync] 📁 Audio đã cache: {Path.GetFileName(localPath)}");
                return localPath;
            }

            var bytes = await _httpClient.GetByteArrayAsync(fullUrl);
            await File.WriteAllBytesAsync(localPath, bytes);

            Console.WriteLine($"[Sync] ⬇️ Tải audio: {Path.GetFileName(localPath)} ({bytes.Length / 1024}KB)");
            return localPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sync] ⚠️ Không tải được audio: {ex.Message}");
            
            // Handle specific error types
            if (ex is HttpRequestException httpEx)
            {
                Console.WriteLine($"[Sync] Audio download network error: {httpEx.StatusCode}");
            }
            else if (ex is TaskCanceledException)
            {
                Console.WriteLine($"[Sync] Audio download timeout");
            }
            else if (ex is UnauthorizedAccessException)
            {
                Console.WriteLine($"[Sync] Audio download - permission denied");
            }
            else if (ex is IOException ioEx)
            {
                Console.WriteLine($"[Sync] Audio download IO error: {ioEx.Message}");
            }
            
            return null;
        }
    }

    private static List<SyncPoiDto>? ParsePoisFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<SyncPoiDto>();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            // Hỗ trợ format mới: { serverTimeUtc, pois: [...] }
            var wrapped = JsonSerializer.Deserialize<SyncPoisResponse>(json, options);
            if (wrapped?.Pois != null)
                return wrapped.Pois;
        }
        catch
        {
            // fallback parse list thuần
        }

        return JsonSerializer.Deserialize<List<SyncPoiDto>>(json, options);
    }

    /// <summary>
    /// Đồng bộ danh sách Tours từ server.
    /// </summary>
    public async Task<bool> SyncToursFromServerAsync(string serverBaseUrl)
    {
        try
        {
            var url = $"{serverBaseUrl.TrimEnd('/')}/api/v1/tours/sync/tours";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(_authService.CurrentToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
            }

            Console.WriteLine($"[Sync] 🔄 Đang đồng bộ Tours từ: {url}");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var tours = ParseToursFromJson(json);
            if (tours == null || tours.Count == 0)
            {
                Console.WriteLine("[Sync] ⚠️ Không có Tour mới từ Server");
                return true;
            }

            int count = 0;
            foreach (var dto in tours)
            {
                // Lưu Tour
                var tour = new Tour
                {
                    Id = dto.Id ?? Guid.NewGuid().ToString(),
                    Name = dto.Name ?? string.Empty,
                    Description = dto.Description,
                    IsActive = dto.IsActive,
                    ThumbnailUrl = dto.ThumbnailUrl,
                    UpdatedAt = dto.UpdatedAt
                };

                await _dbService.UpsertTourAsync(tour);
                Console.WriteLine($"[Sync] ✅ Tour: {tour.Name}");

                // Lưu TourPoiMappings
                if (dto.PoiMappings?.Any() == true)
                {
                    foreach (var mapping in dto.PoiMappings)
                    {
                        var tourMapping = new TourPoiMapping
                        {
                            Id = mapping.Id,
                            TourId = tour.Id,
                            PoiId = mapping.PoiId ?? string.Empty,
                            OrderIndex = mapping.OrderIndex
                        };
                        await _dbService.UpsertTourPoiMappingAsync(tourMapping);
                    }
                    Console.WriteLine($"[Sync]   └── {dto.PoiMappings.Count} POIs");
                }

                count++;
            }

            Console.WriteLine($"[Sync] ✅ Đồng bộ thành công {count} Tours");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Sync] ❌ Lỗi đồng bộ Tours: {ex.Message}");
            return false;
        }
    }

    private static List<SyncTourDto>? ParseToursFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<SyncTourDto>();

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        try
        {
            var wrapped = JsonSerializer.Deserialize<SyncToursResponse>(json, options);
            if (wrapped?.Tours != null)
                return wrapped.Tours;
        }
        catch { }

        return JsonSerializer.Deserialize<List<SyncTourDto>>(json, options);
    }
}

/// <summary>DTO nhận từ Admin Server API (khớp với model Admin Web).</summary>
public class SyncPoiDto
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public int Priority { get; set; }
    public string? ImageUrl { get; set; }
    public string? AudioUrl { get; set; }
    public string? MapLink { get; set; }
    public string? DescriptionEn { get; set; }
    public string? AudioUrlEn { get; set; }
    public string? DescriptionZh { get; set; }
    public string? AudioUrlZh { get; set; }
    public string? DescriptionKo { get; set; }
    public string? AudioUrlKo { get; set; }
    public string? DescriptionJa { get; set; }
    public string? AudioUrlJa { get; set; }
    public string? DescriptionFr { get; set; }
    public string? AudioUrlFr { get; set; }
    public string? TtsScriptVi { get; set; }
    public string? TtsScriptEn { get; set; }
    public string? TtsScriptZh { get; set; }
    public string? TtsScriptKo { get; set; }
    public string? TtsScriptJa { get; set; }
    public string? TtsScriptFr { get; set; }
}

public class SyncPoisResponse
{
    public DateTime ServerTimeUtc { get; set; }
    public List<SyncPoiDto> Pois { get; set; } = new();
}

/// <summary>DTO nhận từ Admin Server API cho Tour.</summary>
public class SyncTourDto
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? ThumbnailUrl { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<SyncTourPoiMappingDto> PoiMappings { get; set; } = new();
}

public class SyncTourPoiMappingDto
{
    public int Id { get; set; }
    public string? TourId { get; set; }
    public string? PoiId { get; set; }
    public int OrderIndex { get; set; }
}

public class SyncToursResponse
{
    public DateTime ServerTimeUtc { get; set; }
    public List<SyncTourDto> Tours { get; set; } = new();
}
