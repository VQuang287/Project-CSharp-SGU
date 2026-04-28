using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class QrController : BaseAdminController
{
    private readonly IConfiguration _config;

    public QrController(AdminDbContext context, IConfiguration config) : base(context)
    {
        _config = config;
    }

    public async Task<IActionResult> Index()
    {
        var items = await _context.QrCodeEntries
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return View(items);
    }

    public async Task<IActionResult> Generate()
    {
        await BindPois();
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string poiId)
    {
        if (string.IsNullOrWhiteSpace(poiId))
        {
            ModelState.AddModelError(string.Empty, "Vui lòng chọn POI.");
            await BindPois();
            return View();
        }

        var poi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poiId);
        if (poi == null) return NotFound();

        // Use direct custom scheme so both in-app scanner and device camera can open POI directly.
        var deepLink = $"audiotour://poi/{poi.Id}";
        var encoded = WebUtility.UrlEncode(deepLink);
        var qrUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={encoded}";

        _context.QrCodeEntries.Add(new QrCodeEntry
        {
            PoiId = poi.Id,
            DeepLink = deepLink,
            QrImageUrl = qrUrl,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var entry = await _context.QrCodeEntries.FirstOrDefaultAsync(x => x.Id == id);
        if (entry != null)
        {
            _context.QrCodeEntries.Remove(entry);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // Landing page for QR links. Tries to open the app, falls back to store or web POI details.
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

    private async Task BindPois()
    {
        ViewBag.Pois = await _context.Pois.AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new SelectListItem(x.Title, x.Id))
            .ToListAsync();
    }
}
