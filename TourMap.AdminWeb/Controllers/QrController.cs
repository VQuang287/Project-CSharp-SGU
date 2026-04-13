using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class QrController : Controller
{
    private readonly AdminDbContext _context;

    public QrController(AdminDbContext context)
    {
        _context = context;
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

    private async Task BindPois()
    {
        ViewBag.Pois = await _context.Pois.AsNoTracking()
            .OrderBy(x => x.Title)
            .Select(x => new SelectListItem(x.Title, x.Id))
            .ToListAsync();
    }
}
