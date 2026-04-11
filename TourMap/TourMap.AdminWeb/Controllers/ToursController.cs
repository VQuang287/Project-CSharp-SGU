using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;
using TourMap.AdminWeb.ViewModels;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class ToursController : Controller
{
    private readonly AdminDbContext _context;

    public ToursController(AdminDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var tours = await _context.Tours
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();
        return View(tours);
    }

    public async Task<IActionResult> Create()
    {
        var vm = new TourEditViewModel();
        await BindPois(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TourEditViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            await BindPois(vm);
            return View(vm);
        }

        var tour = new Tour
        {
            Id = Guid.NewGuid().ToString(),
            Name = vm.Name.Trim(),
            Description = vm.Description?.Trim(),
            IsActive = vm.IsActive
        };

        _context.Tours.Add(tour);

        for (var i = 0; i < vm.SelectedPoiIds.Count; i++)
        {
            _context.TourPoiMappings.Add(new TourPoiMapping
            {
                TourId = tour.Id,
                PoiId = vm.SelectedPoiIds[i],
                OrderIndex = i + 1
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(string id)
    {
        var tour = await _context.Tours.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (tour == null) return NotFound();

        var selected = await _context.TourPoiMappings
            .AsNoTracking()
            .Where(x => x.TourId == id)
            .OrderBy(x => x.OrderIndex)
            .Select(x => x.PoiId)
            .ToListAsync();

        var vm = new TourEditViewModel
        {
            Id = tour.Id,
            Name = tour.Name,
            Description = tour.Description,
            IsActive = tour.IsActive,
            SelectedPoiIds = selected
        };

        await BindPois(vm);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, TourEditViewModel vm)
    {
        if (id != vm.Id) return NotFound();
        var tour = await _context.Tours.FirstOrDefaultAsync(x => x.Id == id);
        if (tour == null) return NotFound();

        if (!ModelState.IsValid)
        {
            await BindPois(vm);
            return View(vm);
        }

        tour.Name = vm.Name.Trim();
        tour.Description = vm.Description?.Trim();
        tour.IsActive = vm.IsActive;

        var oldMappings = _context.TourPoiMappings.Where(x => x.TourId == id);
        _context.TourPoiMappings.RemoveRange(oldMappings);

        for (var i = 0; i < vm.SelectedPoiIds.Count; i++)
        {
            _context.TourPoiMappings.Add(new TourPoiMapping
            {
                TourId = id,
                PoiId = vm.SelectedPoiIds[i],
                OrderIndex = i + 1
            });
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(string id)
    {
        var tour = await _context.Tours.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (tour == null) return NotFound();
        return View(tour);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var tour = await _context.Tours.FirstOrDefaultAsync(x => x.Id == id);
        if (tour == null) return NotFound();

        var mappings = _context.TourPoiMappings.Where(x => x.TourId == id);
        _context.TourPoiMappings.RemoveRange(mappings);
        _context.Tours.Remove(tour);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private async Task BindPois(TourEditViewModel vm)
    {
        vm.AvailablePois = await _context.Pois
            .AsNoTracking()
            .OrderByDescending(x => x.Priority)
            .Select(x => new SelectListItem
            {
                Value = x.Id,
                Text = $"{x.Title} (P{x.Priority})"
            })
            .ToListAsync();
    }
}
