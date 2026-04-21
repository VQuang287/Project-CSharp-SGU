using System.Net;
using Microsoft.Maui.Storage;

namespace TourMap.Services;

public static class DeepLinkHelper
{
    private const string PendingDeepLinkKey = "pending_deeplink";

    public static string? PeekPendingPoiId()
    {
        var pending = Preferences.Default.Get(PendingDeepLinkKey, string.Empty);
        return ExtractPoiId(pending);
    }

    public static string? ConsumePendingPoiId()
    {
        var pending = Preferences.Default.Get(PendingDeepLinkKey, string.Empty);
        if (string.IsNullOrWhiteSpace(pending))
        {
            return null;
        }

        Preferences.Default.Remove(PendingDeepLinkKey);
        return ExtractPoiId(pending);
    }

    public static string? ExtractPoiId(string? rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return null;
        }

        var value = rawValue.Trim();

        if (value.StartsWith("audiotour://", StringComparison.OrdinalIgnoreCase)
            && Uri.TryCreate(value, UriKind.Absolute, out var appUri)
            && appUri.Host.Equals("poi", StringComparison.OrdinalIgnoreCase))
        {
            var poiId = appUri.AbsolutePath.Trim('/');
            return string.IsNullOrWhiteSpace(poiId) ? null : poiId;
        }

        if (!Uri.TryCreate(value, UriKind.Absolute, out var webUri))
        {
            return null;
        }

        var queryPoiId = GetQueryValue(webUri.Query, "poiId") ?? GetQueryValue(webUri.Query, "id");
        if (!string.IsNullOrWhiteSpace(queryPoiId))
        {
            return queryPoiId.Trim();
        }

        var segments = webUri.AbsolutePath
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        var lastSegment = segments[^1].Trim();
        return string.IsNullOrWhiteSpace(lastSegment) ? null : lastSegment;
    }

    private static string? GetQueryValue(string query, string key)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var trimmed = query.TrimStart('?');
        foreach (var part in trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            if (!kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return WebUtility.UrlDecode(kv[1]);
        }

        return null;
    }
}
