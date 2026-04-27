using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;
using Microsoft.AspNetCore.Authorization;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/pois/sync")]
[Authorize]
[ApiController]
public class SyncController : ControllerBase
{
    private readonly AdminDbContext _context;

    public SyncController(AdminDbContext context)
    {
        _context = context;
    }

    // Luồng 1: App Mobile ngầm gọi API này để tải danh sách các điểm ăn chơi mới nhất
    [HttpGet("pois")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "MobileAccess")]
    public async Task<ActionResult<SyncPoisResponse>> GetPois([FromQuery] DateTime? since)
    {
        var query = _context.Pois.AsNoTracking().AsQueryable();

        if (since.HasValue)
        {
            query = query.Where(p => p.UpdatedAt >= since.Value);
        }

        var pois = await query
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync();

        var payload = pois.Select(p => new SyncPoiDto
        {
            Id = p.Id,
            Title = p.Title,
            Description = p.Description,
            Latitude = p.Latitude,
            Longitude = p.Longitude,
            RadiusMeters = p.RadiusMeters,
            Priority = p.Priority,
            ImageUrl = ToAbsoluteAssetUrl(p.ImageUrl),
            AudioUrl = ToAbsoluteAssetUrl(p.AudioUrl),
            MapLink = p.MapLink,
            DescriptionEn = p.DescriptionEn,
            AudioUrlEn = ToAbsoluteAssetUrl(p.AudioUrlEn),
            DescriptionZh = p.DescriptionZh,
            AudioUrlZh = ToAbsoluteAssetUrl(p.AudioUrlZh),
            DescriptionKo = p.DescriptionKo,
            AudioUrlKo = ToAbsoluteAssetUrl(p.AudioUrlKo),
            DescriptionJa = p.DescriptionJa,
            AudioUrlJa = ToAbsoluteAssetUrl(p.AudioUrlJa),
            DescriptionFr = p.DescriptionFr,
            AudioUrlFr = ToAbsoluteAssetUrl(p.AudioUrlFr),
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return Ok(new SyncPoisResponse
        {
            ServerTimeUtc = DateTime.UtcNow,
            Pois = payload
        });
    }

    // Luồng 2: App Mobile gọi API này để báo cáo đã phát âm thanh (Analytics)
    [HttpPost("history")]
    public async Task<IActionResult> LogHistory([FromBody] PlaybackHistory history)
    {
        if (history == null || string.IsNullOrEmpty(history.PoiId))
            return BadRequest("Data không hợp lệ");

        // Gắn nhãn thời gian thực lúc gửi
        history.Timestamp = DateTime.UtcNow;
        _context.PlaybackHistories.Add(history);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Lịch sử đã được lưu lên máy chủ Admin" });
    }

    private string? ToAbsoluteAssetUrl(string? relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return relativeOrAbsolute;

        if (Uri.TryCreate(relativeOrAbsolute, UriKind.Absolute, out _))
            return relativeOrAbsolute;

        if (Request.Host.HasValue && !string.IsNullOrWhiteSpace(Request.Scheme))
            return $"{Request.Scheme}://{Request.Host}{relativeOrAbsolute}";

        return relativeOrAbsolute;
    }
}

public sealed class SyncPoisResponse
{
    public DateTime ServerTimeUtc { get; set; }
    public List<SyncPoiDto> Pois { get; set; } = new();
}

public sealed class SyncPoiDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
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
    public DateTime UpdatedAt { get; set; }
}
