using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/pois")]
[ApiController]
public class PoisController : ControllerBase
{
    private readonly AdminDbContext _context;

    public PoisController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var data = await _context.Pois.AsNoTracking().ToListAsync();
        return Ok(data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var poi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return poi == null ? NotFound() : Ok(poi);
    }

    [HttpGet("nearby")]
    public async Task<IActionResult> Nearby([FromQuery] double lat, [FromQuery] double lng, [FromQuery] double radius = 200)
    {
        if (radius <= 0) radius = 200;
        var pois = await _context.Pois.AsNoTracking().ToListAsync();
        var inRange = pois
            .Select(p => new
            {
                poi = p,
                distance = Haversine(lat, lng, p.Latitude, p.Longitude)
            })
            .Where(x => x.distance <= radius)
            .OrderBy(x => x.distance)
            .Select(x => new
            {
                x.poi.Id,
                x.poi.Title,
                x.poi.Description,
                x.poi.Latitude,
                x.poi.Longitude,
                x.poi.RadiusMeters,
                x.poi.Priority,
                DistanceMeters = Math.Round(x.distance, 2)
            })
            .ToList();

        return Ok(inRange);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Models.Poi poi)
    {
        poi.Id = Guid.NewGuid().ToString();
        poi.UpdatedAt = DateTime.UtcNow;
        _context.Pois.Add(poi);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = poi.Id }, poi);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] Models.Poi poi)
    {
        var existing = await _context.Pois.FirstOrDefaultAsync(x => x.Id == id);
        if (existing == null) return NotFound();

        existing.Title = poi.Title;
        existing.Description = poi.Description;
        existing.Latitude = poi.Latitude;
        existing.Longitude = poi.Longitude;
        existing.Priority = poi.Priority;
        existing.RadiusMeters = poi.RadiusMeters;
        existing.ImageUrl = poi.ImageUrl;
        existing.AudioUrl = poi.AudioUrl;
        existing.MapLink = poi.MapLink;
        existing.DescriptionEn = poi.DescriptionEn;
        existing.DescriptionZh = poi.DescriptionZh;
        existing.DescriptionKo = poi.DescriptionKo;
        existing.AudioUrlEn = poi.AudioUrlEn;
        existing.AudioUrlZh = poi.AudioUrlZh;
        existing.AudioUrlKo = poi.AudioUrlKo;
        existing.DescriptionJa = poi.DescriptionJa;
        existing.AudioUrlJa = poi.AudioUrlJa;
        existing.DescriptionFr = poi.DescriptionFr;
        existing.AudioUrlFr = poi.AudioUrlFr;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(existing);
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Administrator")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var poi = await _context.Pois.FirstOrDefaultAsync(x => x.Id == id);
        if (poi == null) return NotFound();
        _context.Pois.Remove(poi);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static double Haversine(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadius = 6371000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadius * c;
    }
}
