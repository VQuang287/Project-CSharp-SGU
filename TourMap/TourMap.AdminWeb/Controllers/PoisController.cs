using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;
using TourMap.AdminWeb.Services;

namespace TourMap.AdminWeb.Controllers;

[Authorize]
public class PoisController : Controller
{
    private readonly AdminDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IAITranslationService _aiService;

    public PoisController(AdminDbContext context, IWebHostEnvironment env, IAITranslationService aiService)
    {
        _context = context;
        _env = env;
        _aiService = aiService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.Pois.OrderByDescending(p => p.Priority).ToListAsync());
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Poi poi, IFormFile? ImageFile, IFormFile? AudioFile, bool autoAI = false)
    {
        if (ModelState.IsValid)
        {
            if (ImageFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create)) { await ImageFile.CopyToAsync(stream); }
                poi.ImageUrl = "/uploads/images/" + fileName;
            }

            if (AudioFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "audio");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AudioFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create)) { await AudioFile.CopyToAsync(stream); }
                poi.AudioUrl = "/uploads/audio/" + fileName;
            }

            if (autoAI && !string.IsNullOrWhiteSpace(poi.Description))
            {
                poi.DescriptionEn = await _aiService.TranslateTextAsync(poi.Description, "en");
                poi.AudioUrlEn = await _aiService.GenerateTtsAudioAsync(poi.DescriptionEn, "en", _env.WebRootPath);

                poi.DescriptionZh = await _aiService.TranslateTextAsync(poi.Description, "zh");
                poi.AudioUrlZh = await _aiService.GenerateTtsAudioAsync(poi.DescriptionZh, "zh", _env.WebRootPath);

                poi.DescriptionKo = await _aiService.TranslateTextAsync(poi.Description, "ko");
                poi.AudioUrlKo = await _aiService.GenerateTtsAudioAsync(poi.DescriptionKo, "ko", _env.WebRootPath);

                poi.DescriptionJa = await _aiService.TranslateTextAsync(poi.Description, "ja");
                poi.AudioUrlJa = await _aiService.GenerateTtsAudioAsync(poi.DescriptionJa, "ja", _env.WebRootPath);

                poi.DescriptionFr = await _aiService.TranslateTextAsync(poi.Description, "fr");
                poi.AudioUrlFr = await _aiService.GenerateTtsAudioAsync(poi.DescriptionFr, "fr", _env.WebRootPath);
            }

            poi.Id = Guid.NewGuid().ToString();
            poi.UpdatedAt = DateTime.UtcNow;
            _context.Add(poi);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(poi);
    }

    public async Task<IActionResult> Edit(string id)
    {
        if (id == null) return NotFound();
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null) return NotFound();
        return View(poi);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Poi poi, IFormFile? ImageFile, IFormFile? AudioFile, bool autoAI = false)
    {
        if (id != poi.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existingPoi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
            if (existingPoi == null) return NotFound();

            poi.ImageUrl = existingPoi.ImageUrl;
            poi.AudioUrl = existingPoi.AudioUrl;
            poi.AudioUrlEn = existingPoi.AudioUrlEn;
            poi.AudioUrlZh = existingPoi.AudioUrlZh;
            poi.AudioUrlKo = existingPoi.AudioUrlKo;
            poi.AudioUrlJa = existingPoi.AudioUrlJa;
            poi.AudioUrlFr = existingPoi.AudioUrlFr;
            poi.DescriptionEn = existingPoi.DescriptionEn;
            poi.DescriptionZh = existingPoi.DescriptionZh;
            poi.DescriptionKo = existingPoi.DescriptionKo;
            poi.DescriptionJa = existingPoi.DescriptionJa;
            poi.DescriptionFr = existingPoi.DescriptionFr;

            if (ImageFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "images");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(ImageFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create)) { await ImageFile.CopyToAsync(stream); }
                poi.ImageUrl = "/uploads/images/" + fileName;
            }

            if (AudioFile != null)
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "audio");
                Directory.CreateDirectory(folder);
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(AudioFile.FileName);
                using (var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create)) { await AudioFile.CopyToAsync(stream); }
                poi.AudioUrl = "/uploads/audio/" + fileName;
            }

            // Chỉ sinh lại AI nếu description thực sự thay đổi
            if (autoAI && !string.IsNullOrWhiteSpace(poi.Description) && poi.Description != existingPoi.Description)
            {
                poi.DescriptionEn = await _aiService.TranslateTextAsync(poi.Description, "en");
                poi.AudioUrlEn = await _aiService.GenerateTtsAudioAsync(poi.DescriptionEn, "en", _env.WebRootPath);

                poi.DescriptionZh = await _aiService.TranslateTextAsync(poi.Description, "zh");
                poi.AudioUrlZh = await _aiService.GenerateTtsAudioAsync(poi.DescriptionZh, "zh", _env.WebRootPath);

                poi.DescriptionKo = await _aiService.TranslateTextAsync(poi.Description, "ko");
                poi.AudioUrlKo = await _aiService.GenerateTtsAudioAsync(poi.DescriptionKo, "ko", _env.WebRootPath);

                poi.DescriptionJa = await _aiService.TranslateTextAsync(poi.Description, "ja");
                poi.AudioUrlJa = await _aiService.GenerateTtsAudioAsync(poi.DescriptionJa, "ja", _env.WebRootPath);

                poi.DescriptionFr = await _aiService.TranslateTextAsync(poi.Description, "fr");
                poi.AudioUrlFr = await _aiService.GenerateTtsAudioAsync(poi.DescriptionFr, "fr", _env.WebRootPath);
            }

            try
            {
                poi.UpdatedAt = DateTime.UtcNow;
                _context.Update(poi);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) { }
            return RedirectToAction(nameof(Index));
        }
        return View(poi);
    }

    public async Task<IActionResult> Delete(string id)
    {
        if (id == null) return NotFound();
        var poi = await _context.Pois.FirstOrDefaultAsync(m => m.Id == id);
        if (poi == null) return NotFound();
        return View(poi);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi != null) _context.Pois.Remove(poi);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}
