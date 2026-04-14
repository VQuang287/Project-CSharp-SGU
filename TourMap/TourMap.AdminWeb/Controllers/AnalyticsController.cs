using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.ViewModels;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class AnalyticsController : Controller
{
    private readonly AdminDbContext _context;

    public AnalyticsController(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var from7Days = now.AddDays(-7);
        var from30Days = now.AddDays(-30);

        var baseQuery = _context.PlaybackHistories.AsNoTracking();
        var query7Days = baseQuery.Where(x => x.Timestamp >= from7Days);
        var query30Days = baseQuery.Where(x => x.Timestamp >= from30Days);

        var total7Days = await query7Days.CountAsync();
        var total30Days = await query30Days.CountAsync();
        var avgDuration = await query7Days.AnyAsync()
            ? await query7Days.AverageAsync(x => x.DurationSeconds)
            : 0;
        var completionRate = await query7Days.AnyAsync()
            ? await query7Days.AverageAsync(x => x.IsCompleted ? 1.0 : 0.0) * 100.0
            : 0;

        var topPois = await _context.PlaybackHistories
            .AsNoTracking()
            .Where(x => x.Timestamp >= from7Days)
            .GroupBy(x => x.PoiId)
            .Select(g => new
            {
                PoiId = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var poiDict = await _context.Pois
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Id, x => x.Title);

        var topPoiItems = topPois
            .Select(x => new TopPoiItem
            {
                PoiId = x.PoiId,
                PoiTitle = poiDict.TryGetValue(x.PoiId, out var title) ? title : x.PoiId,
                PlayCount = x.Count
            })
            .ToList();

        var triggerBreakdown = await query7Days
            .GroupBy(x => x.TriggerType.ToUpper())
            .Select(g => new TriggerTypeItem
            {
                TriggerType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var dailyPlays = await query7Days
            .GroupBy(x => x.Timestamp.Date)
            .Select(g => new DailyPlayItem
            {
                Day = DateOnly.FromDateTime(g.Key),
                Count = g.Count()
            })
            .OrderBy(x => x.Day)
            .ToListAsync();

        var heatmapLogs = await _context.UserLocationLogs
            .AsNoTracking()
            .Where(x => x.RecordedAt >= from7Days)
            .ToListAsync();

        var heatmapPoints = heatmapLogs
            .GroupBy(x => new { Lat = Math.Round(x.Latitude, 4), Lng = Math.Round(x.Longitude, 4) })
            .Select(g => new HeatmapPoint
            {
                Lat = g.Key.Lat,
                Lng = g.Key.Lng,
                Intensity = Math.Min(g.Count() * 0.2, 1.0)
            })
            .ToList();

        var vm = new AnalyticsDashboardViewModel
        {
            TotalPlays7Days = total7Days,
            TotalPlays30Days = total30Days,
            AverageDurationSeconds = Math.Round(avgDuration, 2),
            CompletionRatePercent = Math.Round(completionRate, 2),
            TopPois7Days = topPoiItems,
            TriggerTypeBreakdown7Days = triggerBreakdown,
            DailyPlays7Days = dailyPlays,
            HeatmapPoints = heatmapPoints
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> ExportCsv(int days = 30)
    {
        days = days is > 0 and <= 365 ? days : 30;
        var from = DateTime.UtcNow.AddDays(-days);

        var rows = await _context.PlaybackHistories
            .AsNoTracking()
            .Where(x => x.Timestamp >= from)
            .OrderByDescending(x => x.Timestamp)
            .Take(5000)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Id,PoiId,DeviceId,TimestampUtc,TriggerType,DurationSeconds,IsCompleted");
        foreach (var item in rows)
        {
            sb.AppendLine($"{item.Id},{Escape(item.PoiId)},{Escape(item.DeviceId)},{item.Timestamp:O},{Escape(item.TriggerType)},{item.DurationSeconds},{item.IsCompleted}");
        }

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var fileName = $"analytics_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";
        return File(bytes, "text/csv", fileName);
    }

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var normalized = value.Replace("\"", "\"\"")
                              .Replace("\r", " ")
                              .Replace("\n", " ");
        return $"\"{normalized}\"";
    }
}
