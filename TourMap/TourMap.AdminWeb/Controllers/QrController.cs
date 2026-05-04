using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class QrController : BaseAdminController
{
    private readonly IConfiguration _config;

    // QR code expiry time in minutes
    private const int QrExpiryMinutes = 30;

    public QrController(AdminDbContext context, IConfiguration config) : base(context)
    {
        _config = config;
    }

    public IActionResult Index()
    {
        // Generate time-limited QR code for APK download
        var downloadUrl = _config["AppLinks:ApkDownloadUrl"] ?? Url.Action("Download", "Qr", null, Request.Scheme)!;
        var expiresAt = DateTime.UtcNow.AddMinutes(QrExpiryMinutes);
        var token = GenerateQrToken(downloadUrl, expiresAt);

        var encoded = WebUtility.UrlEncode(downloadUrl + "?token=" + token);
        var qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encoded}";

        var model = new QrDownloadViewModel
        {
            QrImageUrl = qrImageUrl,
            DownloadUrl = downloadUrl,
            ExpiresAt = expiresAt,
            ExpiryMinutes = QrExpiryMinutes,
            GeneratedAt = DateTime.UtcNow
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Regenerate()
    {
        // Just redirect to Index to generate new QR with fresh expiry
        return RedirectToAction(nameof(Index));
    }

    // APK Download page - validates token and shows download button
    [HttpGet("qr/download")]
    [AllowAnonymous]
    public IActionResult Download(string? token)
    {
        var apkUrl = _config["AppLinks:ApkDownloadUrl"] ?? "/apk/TourMap.apk";

        // Validate token
        if (string.IsNullOrWhiteSpace(token))
        {
            return View("~/Views/Qr/Expired.cshtml", new QrExpiredViewModel
            {
                Message = "Mã QR không hợp lệ.",
                AdminWebUrl = Url.Action("Index", "Qr", null, Request.Scheme)
            });
        }

        if (!ValidateQrToken(token, out var expiryTime) || expiryTime < DateTime.UtcNow)
        {
            return View("~/Views/Qr/Expired.cshtml", new QrExpiredViewModel
            {
                Message = "Mã QR đã hết hạn. Vui lòng quét lại mã QR mới từ Admin.",
                AdminWebUrl = Url.Action("Index", "Qr", null, Request.Scheme)
            });
        }

        var remainingMinutes = (int)(expiryTime - DateTime.UtcNow).TotalMinutes;

        var model = new QrDownloadPageViewModel
        {
            ApkUrl = apkUrl,
            ExpiresAt = expiryTime,
            RemainingMinutes = remainingMinutes,
            AppName = _config["AppLinks:AppName"] ?? "TourMap Audio Guide"
        };

        return View("~/Views/Qr/Download.cshtml", model);
    }

    // Landing page for legacy QR links (backward compatibility)
    [HttpGet("Launch/{poiId}")]
    [AllowAnonymous]
    public async Task<IActionResult> Launch(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId)) return NotFound();
        var poi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poiId);
        if (poi == null) return NotFound();

        var androidPackage = _config["AppLinks:AndroidPackage"] ?? "com.companyname.tourmap";
        var appStoreId = _config["AppLinks:AppStoreId"] ?? string.Empty;

        var playStoreUrl = $"https://play.google.com/store/apps/details?id={androidPackage}&referrer={WebUtility.UrlEncode($"deep_link=audiotour://poi/{poi.Id}")}";
        var appStoreUrl = string.IsNullOrEmpty(appStoreId) ? "https://apps.apple.com/" : $"https://apps.apple.com/app/id{appStoreId}";

        var model = new
        {
            Id = poi.Id,
            Title = poi.Title,
            Description = poi.Description,
            LaunchScheme = $"audiotour://poi/{poi.Id}",
            PlayStoreUrl = playStoreUrl,
            AppStoreUrl = appStoreUrl,
            AndroidPackage = androidPackage,
            PoiWebUrl = Url.Action("Details", "Pois", new { id = poi.Id }, Request.Scheme)
        };

        return View("~/Views/Qr/Launch.cshtml", model);
    }

    // Simple token generation for QR expiry validation
    private static string GenerateQrToken(string url, DateTime expiry)
    {
        var data = url + expiry.ToString("yyyyMMddHHmm");
        var hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).Substring(0, 16);
    }

    private static bool ValidateQrToken(string token, out DateTime expiry)
    {
        expiry = DateTime.MinValue;
        // Simple validation: token is valid if it matches the format
        return token.Length == 16;
    }
}

// View Models for QR
public class QrDownloadViewModel
{
    public string QrImageUrl { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int ExpiryMinutes { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class QrDownloadPageViewModel
{
    public string ApkUrl { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public int RemainingMinutes { get; set; }
    public string AppName { get; set; } = string.Empty;
}

public class QrExpiredViewModel
{
    public string Message { get; set; } = string.Empty;
    public string? AdminWebUrl { get; set; }
}
