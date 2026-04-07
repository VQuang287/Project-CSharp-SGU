using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/analytics")]
[ApiController]
public class AnalyticsApiController : ControllerBase
{
    private readonly AdminDbContext _context;

    public AnalyticsApiController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpPost("play")]
    public async Task<IActionResult> LogPlay([FromBody] PlaybackHistory request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PoiId))
        {
            return BadRequest(new { success = false, message = "Invalid payload." });
        }

        var poiExists = await _context.Pois.AnyAsync(x => x.Id == request.PoiId);
        if (!poiExists)
        {
            return BadRequest(new { success = false, message = "Poi does not exist." });
        }

        request.Id = 0;
        request.Timestamp = DateTime.UtcNow;
        request.TriggerType = string.IsNullOrWhiteSpace(request.TriggerType)
            ? "GPS"
            : request.TriggerType.Trim().ToUpperInvariant();

        _context.PlaybackHistories.Add(request);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, id = request.Id });
    }

    [HttpPost("location")]
    public async Task<IActionResult> LogLocation([FromBody] List<UserLocationLog> logs)
    {
        if (logs == null || !logs.Any())
        {
            return BadRequest(new { success = false, message = "Empty payload." });
        }

        foreach (var log in logs)
        {
            log.Id = 0;
            if (log.RecordedAt == default)
            {
                log.RecordedAt = DateTime.UtcNow;
            }
        }

        _context.UserLocationLogs.AddRange(logs);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, count = logs.Count });
    }

    [HttpGet("dashboard")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    public async Task<IActionResult> Dashboard([FromQuery] int days = 7)
    {
        days = days is > 0 and <= 90 ? days : 7;
        var from = DateTime.UtcNow.AddDays(-days);

        var query = _context.PlaybackHistories.AsNoTracking().Where(x => x.Timestamp >= from);

        var totalPlays = await query.CountAsync();
        var completedPlays = await query.CountAsync(x => x.IsCompleted);
        var averageDurationSeconds = await query.AnyAsync()
            ? await query.AverageAsync(x => x.DurationSeconds)
            : 0;

        var triggerBreakdown = await query
            .GroupBy(x => x.TriggerType.ToUpper())
            .Select(g => new { triggerType = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .ToListAsync();

        var topPois = await query
            .GroupBy(x => x.PoiId)
            .Select(g => new { poiId = g.Key, count = g.Count() })
            .OrderByDescending(x => x.count)
            .Take(10)
            .Join(
                _context.Pois.AsNoTracking(),
                h => h.poiId,
                p => p.Id,
                (h, p) => new { h.poiId, poiTitle = p.Title, h.count })
            .ToListAsync();

        return Ok(new
        {
            days,
            totalPlays,
            completedPlays,
            completionRatePercent = totalPlays == 0 ? 0 : Math.Round((double)completedPlays / totalPlays * 100, 2),
            averageDurationSeconds = Math.Round(averageDurationSeconds, 2),
            triggerBreakdown,
            topPois
        });
    }
}
