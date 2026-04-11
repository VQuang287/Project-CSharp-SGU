using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/tours")]
[ApiController]
public class ToursController : ControllerBase
{
    private readonly AdminDbContext _context;

    public ToursController(AdminDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetTours()
    {
        var tours = await _context.Tours
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.Name, x.Description })
            .ToListAsync();
        return Ok(tours);
    }

    [HttpGet("{id}/pois")]
    public async Task<IActionResult> GetTourPois(string id)
    {
        var pois = await _context.TourPoiMappings
            .AsNoTracking()
            .Where(x => x.TourId == id)
            .OrderBy(x => x.OrderIndex)
            .Join(
                _context.Pois.AsNoTracking(),
                m => m.PoiId,
                p => p.Id,
                (m, p) => new
                {
                    m.OrderIndex,
                    p.Id,
                    p.Title,
                    p.Description,
                    p.Latitude,
                    p.Longitude,
                    p.RadiusMeters,
                    p.Priority,
                    p.AudioUrl
                })
            .ToListAsync();

        return Ok(pois);
    }
}
