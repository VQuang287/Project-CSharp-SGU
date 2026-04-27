using Microsoft.Maui.Storage;

namespace TourMap.Services;

/// <summary>
/// Central place for AdminWeb endpoint discovery.
/// Keeps Mobile base URLs consistent across Auth, Sync and SignalR.
/// </summary>
public static class BackendEndpoints
{
    // Expected format: "http(s)://host:port" (path is ignored)
    public const string ServerBaseUrlPreferenceKey = "server_base_url";

    /// <summary>
    /// Returns candidate server base URLs (scheme + host + port).
    /// Order matters: user override first, then env var, then known dev defaults.
    /// </summary>
    public static IReadOnlyList<string> GetCandidateServerBaseUrls()
    {
        var candidates = new List<string>();

        AddAuthorityFromUrl(candidates, Preferences.Default.Get(ServerBaseUrlPreferenceKey, string.Empty));
        AddAuthorityFromUrl(candidates, Environment.GetEnvironmentVariable("TOURMAP_SERVER_BASE_URL"));

        // Dev defaults - UPDATE THIS to match your PC's IP address
        // Find your IP: cmd -> ipconfig -> IPv4 Address (e.g., 192.168.1.5)
        AddAuthorityFromUrl(candidates, "https://nectar-fade-repose.ngrok-free.dev"); // ngrok tunnel (cross-network)
        AddAuthorityFromUrl(candidates, "http://192.168.1.6:5042"); // LAN IP
        AddAuthorityFromUrl(candidates, "http://10.0.2.2:5042"); // Android emulator -> host machine
        AddAuthorityFromUrl(candidates, "http://localhost:5042");
        AddAuthorityFromUrl(candidates, "http://127.0.0.1:5042");

        return candidates
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> GetAuthBaseUrls() =>
        GetCandidateServerBaseUrls()
            .Select(server => $"{server.TrimEnd('/')}/api/v1/auth")
            .ToArray();

    public static IReadOnlyList<string> GetDeviceHubUrls() =>
        GetCandidateServerBaseUrls()
            .Select(server => $"{server.TrimEnd('/')}/hubs/devices")
            .ToArray();

    public static void RememberWorkingServerFromUrl(string anyUrl)
    {
        if (!TryGetAuthority(anyUrl, out var authority))
            return;

        Preferences.Default.Set(ServerBaseUrlPreferenceKey, authority);
    }

    private static void AddAuthorityFromUrl(List<string> list, string? url)
    {
        if (!TryGetAuthority(url, out var authority))
            return;

        list.Add(authority);
    }

    private static bool TryGetAuthority(string? url, out string authority)
    {
        authority = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();

        // Allow users to input "192.168.1.7:5042" (no scheme)
        if (!trimmed.Contains("://", StringComparison.Ordinal))
        {
            trimmed = "http://" + trimmed;
        }

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        authority = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }
}
