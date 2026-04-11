using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/qr")]
[ApiController]
public class QrController : ControllerBase
{
    private readonly AdminDbContext _context;

    public QrController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet("{poiId}")]
    public async Task<IActionResult> Resolve(string poiId)
    {
        var poi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poiId);
        if (poi == null) return NotFound(new { success = false });

        return Ok(new
        {
            success = true,
            deepLink = $"audiotour://poi/{poi.Id}",
            poi = new
            {
                poi.Id,
                poi.Title,
                poi.Description,
                poi.Latitude,
                poi.Longitude,
                poi.RadiusMeters,
                poi.Priority,
                poi.AudioUrl
            }
        });
    }

    [HttpPost("generate/{poiId}")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    public async Task<IActionResult> Generate(string poiId)
    {
        var poi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == poiId);
        if (poi == null) return NotFound(new { success = false });

        return Ok(new
        {
            success = true,
            deepLink = $"audiotour://poi/{poi.Id}",
            qrImageUrl = $"https://api.qrserver.com/v1/create-qr-code/?size=300x300&data={Uri.EscapeDataString($"audiotour://poi/{poi.Id}")}"
        });
    }
}
