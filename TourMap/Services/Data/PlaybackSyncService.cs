using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TourMap.Services;

public sealed class PlaybackSyncService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public PlaybackSyncService(IHttpClientFactory httpClientFactory, AuthService authService)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
        _authService = authService;
    }

    public async Task<bool> LogPlaybackAsync(PlaybackLogRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PoiId))
            return false;

        if (Connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine("[PlaybackSync] Skip log - no internet");
            return false;
        }

        await EnsureAuthenticatedAsync();

        foreach (var baseUrl in BackendEndpoints.GetCandidateServerBaseUrls())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = $"{baseUrl.TrimEnd('/')}/api/v1/analytics/play";
            try
            {
                using var message = new HttpRequestMessage(HttpMethod.Post, url)
                {
                    Content = JsonContent.Create(request)
                };

                if (!string.IsNullOrEmpty(_authService.CurrentToken))
                {
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _authService.CurrentToken);
                }

                var response = await _httpClient.SendAsync(message, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    BackendEndpoints.RememberWorkingServerFromUrl(baseUrl);
                    Console.WriteLine($"[PlaybackSync] Logged play to {baseUrl}");
                    return true;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                Console.WriteLine($"[PlaybackSync] Log failed at {baseUrl}: {(int)response.StatusCode} {responseBody}");
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine($"[PlaybackSync] Log timeout at {baseUrl}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"[PlaybackSync] Log request failed at {baseUrl}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PlaybackSync] Log error at {baseUrl}: {ex.Message}");
            }
        }

        return false;
    }

    private async Task EnsureAuthenticatedAsync()
    {
        if (_authService.IsAuthenticated)
            return;

        var refreshed = await _authService.RefreshTokenAsync();
        if (refreshed)
            return;

        var loginResult = await _authService.LoginAnonymousAsync();
        if (!loginResult.Success)
        {
            Console.WriteLine($"[PlaybackSync] Anonymous login failed: {loginResult.ErrorMessage}");
        }
    }

    public static string GetOrCreateDeviceId()
    {
        var id = Preferences.Default.Get<string>("device_uuid", string.Empty);
        if (string.IsNullOrWhiteSpace(id))
        {
            id = Guid.NewGuid().ToString();
            Preferences.Default.Set("device_uuid", id);
        }
        return id;
    }
}

public sealed class PlaybackLogRequest
{
    public string PoiId { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
    public string TriggerType { get; set; } = "GPS";
    public int DurationSeconds { get; set; }
    public bool IsCompleted { get; set; } = true;
}
