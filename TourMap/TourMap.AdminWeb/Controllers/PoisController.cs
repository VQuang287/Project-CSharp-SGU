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

    [AllowAnonymous]
    public async Task<IActionResult> Details(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return NotFound();
        }

        var poi = await _context.Pois
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        return View(poi);
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

            poi.TtsScriptVi = poi.Description;

            if (autoAI && !string.IsNullOrWhiteSpace(poi.Description))
            {
                poi.DescriptionEn = await _aiService.TranslateTextAsync(poi.Description, "en");
                poi.AudioUrlEn = await _aiService.GenerateTtsAudioAsync(poi.DescriptionEn, "en", _env.WebRootPath);
                poi.TtsScriptEn = poi.DescriptionEn;

                poi.DescriptionZh = await _aiService.TranslateTextAsync(poi.Description, "zh");
                poi.AudioUrlZh = await _aiService.GenerateTtsAudioAsync(poi.DescriptionZh, "zh", _env.WebRootPath);
                poi.TtsScriptZh = poi.DescriptionZh;

                poi.DescriptionKo = await _aiService.TranslateTextAsync(poi.Description, "ko");
                poi.AudioUrlKo = await _aiService.GenerateTtsAudioAsync(poi.DescriptionKo, "ko", _env.WebRootPath);
                poi.TtsScriptKo = poi.DescriptionKo;

                poi.DescriptionJa = await _aiService.TranslateTextAsync(poi.Description, "ja");
                poi.AudioUrlJa = await _aiService.GenerateTtsAudioAsync(poi.DescriptionJa, "ja", _env.WebRootPath);
                poi.TtsScriptJa = poi.DescriptionJa;

                poi.DescriptionFr = await _aiService.TranslateTextAsync(poi.Description, "fr");
                poi.AudioUrlFr = await _aiService.GenerateTtsAudioAsync(poi.DescriptionFr, "fr", _env.WebRootPath);
                poi.TtsScriptFr = poi.DescriptionFr;
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

        poi.TtsScriptVi ??= poi.Description;
        poi.TtsScriptEn ??= poi.DescriptionEn;
        poi.TtsScriptZh ??= poi.DescriptionZh;
        poi.TtsScriptKo ??= poi.DescriptionKo;
        poi.TtsScriptJa ??= poi.DescriptionJa;
        poi.TtsScriptFr ??= poi.DescriptionFr;

        return View(poi);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, Poi poi, IFormFile? ImageFile, IFormFile? AudioFile, [FromForm] bool autoAI = false, CancellationToken cancellationToken = default)
    {
        if (id != poi.Id) return NotFound();

        if (ModelState.IsValid)
        {
            var existingPoi = await _context.Pois.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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
            poi.TtsScriptVi = existingPoi.TtsScriptVi;
            poi.TtsScriptEn = existingPoi.TtsScriptEn;
            poi.TtsScriptZh = existingPoi.TtsScriptZh;
            poi.TtsScriptKo = existingPoi.TtsScriptKo;
            poi.TtsScriptJa = existingPoi.TtsScriptJa;
            poi.TtsScriptFr = existingPoi.TtsScriptFr;

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

            poi.TtsScriptVi = poi.Description;

            // Chỉ sinh lại AI nếu description thực sự thay đổi
            if (autoAI && !string.IsNullOrWhiteSpace(poi.Description) && poi.Description != existingPoi.Description)
            {
                poi.DescriptionEn = await _aiService.TranslateTextAsync(poi.Description, "en");
                poi.AudioUrlEn = await _aiService.GenerateTtsAudioAsync(poi.DescriptionEn, "en", _env.WebRootPath);
                poi.TtsScriptEn = poi.DescriptionEn;

                poi.DescriptionZh = await _aiService.TranslateTextAsync(poi.Description, "zh");
                poi.AudioUrlZh = await _aiService.GenerateTtsAudioAsync(poi.DescriptionZh, "zh", _env.WebRootPath);
                poi.TtsScriptZh = poi.DescriptionZh;

                poi.DescriptionKo = await _aiService.TranslateTextAsync(poi.Description, "ko");
                poi.AudioUrlKo = await _aiService.GenerateTtsAudioAsync(poi.DescriptionKo, "ko", _env.WebRootPath);
                poi.TtsScriptKo = poi.DescriptionKo;

                poi.DescriptionJa = await _aiService.TranslateTextAsync(poi.Description, "ja");
                poi.AudioUrlJa = await _aiService.GenerateTtsAudioAsync(poi.DescriptionJa, "ja", _env.WebRootPath);
                poi.TtsScriptJa = poi.DescriptionJa;

                poi.DescriptionFr = await _aiService.TranslateTextAsync(poi.Description, "fr");
                poi.AudioUrlFr = await _aiService.GenerateTtsAudioAsync(poi.DescriptionFr, "fr", _env.WebRootPath);
                poi.TtsScriptFr = poi.DescriptionFr;
            }

            try
            {
                poi.UpdatedAt = DateTime.UtcNow;
                _context.Update(poi);
                await _context.SaveChangesAsync(cancellationToken);
                TempData["Success"] = "Đã lưu chỉnh sửa POI thành công.";
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "Không thể lưu vì dữ liệu POI đã bị thay đổi ở nơi khác. Vui lòng tải lại trang và thử lại.");
                TempData["Error"] = "Lưu POI thất bại do xung đột dữ liệu.";
                return View(poi);
            }
            catch (OperationCanceledException)
            {
                ModelState.AddModelError(string.Empty, "Yêu cầu lưu đã bị hủy do timeout hoặc mất kết nối. Vui lòng thử lại.");
                TempData["Error"] = "Lưu POI bị hủy do quá thời gian chờ.";
                return View(poi);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi khi lưu chỉnh sửa POI. Vui lòng thử lại sau.");
                TempData["Error"] = "Lưu POI thất bại do lỗi hệ thống.";
                return View(poi);
            }
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVisibility(string id)
    {
        if (string.IsNullOrEmpty(id)) return NotFound();
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null) return NotFound();

        poi.IsActive = !poi.IsActive;
        poi.UpdatedAt = DateTime.UtcNow;
        _context.Update(poi);
        await _context.SaveChangesAsync();
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> TranslateScripts([FromBody] TranslateScriptsRequest request)
    {
        var sourceText = request.SourceText?.Trim();
        if (string.IsNullOrWhiteSpace(sourceText))
        {
            return BadRequest(new { success = false, message = "Scripts TTS đang trống." });
        }

        var translations = new Dictionary<string, string>();
        foreach (var lang in SupportedLanguages)
        {
            translations[lang] = await _aiService.TranslateTextAsync(sourceText, lang);
        }

        return Json(new { success = true, translations });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewLanguageAudio([FromBody] PreviewLanguageAudioRequest request)
    {
        if (!TryNormalizeLanguage(request.Language, out var language))
        {
            return BadRequest(new { success = false, message = "Ngôn ngữ không hợp lệ." });
        }

        var text = request.Text?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return BadRequest(new { success = false, message = "Nội dung text đang trống." });
        }

        var audioUrl = await _aiService.GenerateTtsAudioAsync(text, language, _env.WebRootPath);
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return StatusCode(StatusCodes.Status502BadGateway, new { success = false, message = "Không tạo được audio AI." });
        }

        return Json(new { success = true, audioUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveLanguageScript([FromBody] SaveLanguageScriptRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PoiId))
        {
            return BadRequest(new { success = false, message = "Thiếu mã POI." });
        }

        if (!TryNormalizeLanguage(request.Language, out var language))
        {
            return BadRequest(new { success = false, message = "Ngôn ngữ không hợp lệ." });
        }

        var poi = await _context.Pois.FirstOrDefaultAsync(x => x.Id == request.PoiId);
        if (poi == null)
        {
            return NotFound(new { success = false, message = "Không tìm thấy POI." });
        }

        var scriptText = request.Text?.Trim() ?? string.Empty;
        SetScriptByLanguage(poi, language, scriptText);

        string? audioUrl = null;
        if (!string.IsNullOrWhiteSpace(scriptText))
        {
            audioUrl = await _aiService.GenerateTtsAudioAsync(scriptText, language, _env.WebRootPath);
        }
        SetAudioByLanguage(poi, language, audioUrl);

        poi.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Json(new
        {
            success = true,
            language,
            audioUrl = GetAudioByLanguage(poi, language),
            message = "Đã lưu scripts vào database."
        });
    }

    private static readonly string[] SupportedLanguages = ["en", "zh", "ko", "ja", "fr"];

    private static bool TryNormalizeLanguage(string? inputLanguage, out string language)
    {
        language = (inputLanguage ?? string.Empty).Trim().ToLowerInvariant();
        return language is "vi" or "en" or "zh" or "ko" or "ja" or "fr";
    }

    private static void SetScriptByLanguage(Poi poi, string language, string text)
    {
        switch (language)
        {
            case "vi":
                poi.Description = text;
                poi.TtsScriptVi = text;
                break;
            case "en":
                poi.DescriptionEn = text;
                poi.TtsScriptEn = text;
                break;
            case "zh":
                poi.DescriptionZh = text;
                poi.TtsScriptZh = text;
                break;
            case "ko":
                poi.DescriptionKo = text;
                poi.TtsScriptKo = text;
                break;
            case "ja":
                poi.DescriptionJa = text;
                poi.TtsScriptJa = text;
                break;
            case "fr":
                poi.DescriptionFr = text;
                poi.TtsScriptFr = text;
                break;
        }
    }

    private static void SetAudioByLanguage(Poi poi, string language, string? audioUrl)
    {
        switch (language)
        {
            case "vi":
                poi.AudioUrl = audioUrl;
                break;
            case "en":
                poi.AudioUrlEn = audioUrl;
                break;
            case "zh":
                poi.AudioUrlZh = audioUrl;
                break;
            case "ko":
                poi.AudioUrlKo = audioUrl;
                break;
            case "ja":
                poi.AudioUrlJa = audioUrl;
                break;
            case "fr":
                poi.AudioUrlFr = audioUrl;
                break;
        }
    }

    private static string? GetAudioByLanguage(Poi poi, string language)
    {
        return language switch
        {
            "vi" => poi.AudioUrl,
            "en" => poi.AudioUrlEn,
            "zh" => poi.AudioUrlZh,
            "ko" => poi.AudioUrlKo,
            "ja" => poi.AudioUrlJa,
            "fr" => poi.AudioUrlFr,
            _ => null
        };
    }

    public sealed class TranslateScriptsRequest
    {
        public string? SourceText { get; set; }
    }

    public sealed class PreviewLanguageAudioRequest
    {
        public string? Language { get; set; }
        public string? Text { get; set; }
    }

    public sealed class SaveLanguageScriptRequest
    {
        public string? PoiId { get; set; }
        public string? Language { get; set; }
        public string? Text { get; set; }
    }
}
