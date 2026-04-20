using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;
using TourMap.AdminWeb.Models.ViewModels;
using static TourMap.AdminWeb.Models.ConnectionState;

namespace TourMap.AdminWeb.Controllers;

public class HomeController : Controller
{
    private readonly AdminDbContext _dbContext;

    public HomeController(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    private async Task<int> GetOnlineDeviceCountAsync()
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        return await _dbContext.DeviceConnections
            .CountAsync(d => d.LastHeartbeatAt > fiveMinutesAgo && d.State != ConnectionState.Offline);
    }

    public async Task<IActionResult> Index()
    {
        var topPois = await _dbContext.PlaybackHistories
            .AsNoTracking()
            .GroupBy(x => x.PoiId)
            .Select(x => new { PoiId = x.Key, Plays = x.Count() })
            .OrderByDescending(x => x.Plays)
            .Take(5)
            .ToListAsync();

        var poiMap = await _dbContext.Pois
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Title);

        // Count devices online in last 5 minutes
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        var onlineDevices = await _dbContext.DeviceConnections
            .CountAsync(d => d.LastHeartbeatAt > fiveMinutesAgo && d.State != ConnectionState.Offline);

        var model = new AdminDashboardViewModel
        {
            TotalPois = await _dbContext.Pois.CountAsync(),
            ActiveTours = await _dbContext.Tours.CountAsync(x => x.IsActive),
            TotalPlays = await _dbContext.PlaybackHistories.CountAsync(),
            ActiveMobileUsers = await _dbContext.MobileUsers.CountAsync(),
            TotalQrCodes = await _dbContext.QrCodeEntries.CountAsync(),
            OnlineDevices = onlineDevices,
            TopPois = topPois.Select(x => new PoiPlaybackItem
            {
                PoiId = x.PoiId,
                PoiTitle = poiMap.TryGetValue(x.PoiId, out var title) ? title : "Unknown POI",
                PlayCount = x.Plays
            }).ToList()
        };

        ViewData["OnlineDevices"] = model.OnlineDevices;
        return View(model);
    }

    public async Task<IActionResult> Privacy()
    {
        ViewData["OnlineDevices"] = await GetOnlineDeviceCountAsync();
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
