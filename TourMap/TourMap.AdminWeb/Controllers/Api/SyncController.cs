using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/[controller]")]
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
    public async Task<ActionResult<IEnumerable<Poi>>> GetPois()
    {
        return await _context.Pois.ToListAsync();
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
}
