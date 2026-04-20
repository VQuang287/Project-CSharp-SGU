using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers;

[Authorize(Roles = "Administrator")]
public class UsersController : Controller
{
    private readonly AdminDbContext _context;

    public UsersController(AdminDbContext context)
    {
        _context = context;
    }

    // GET: /Users
    public async Task<IActionResult> Index(string? search, string? role)
    {
        var query = _context.MobileUsers.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(u => 
                (u.Email != null && u.Email.Contains(s)) || 
                u.DisplayName.Contains(s) || 
                u.DeviceId.Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(role))
            query = query.Where(u => u.Role == role);

        var users = await query
            .OrderByDescending(u => u.LastLoginAt)
            .Take(200)
            .ToListAsync();

        ViewData["Search"] = search;
        ViewData["Role"] = role;
        ViewData["TotalUsers"] = await _context.MobileUsers.CountAsync();
        ViewData["TotalRegistered"] = await _context.MobileUsers.CountAsync(u => u.Email != null);
        ViewData["TotalGuests"] = await _context.MobileUsers.CountAsync(u => u.Role == "Guest");

        return View(users);
    }

    // GET: /Users/Details/{id}
    public async Task<IActionResult> Details(string id)
    {
        var user = await _context.MobileUsers.FindAsync(id);
        if (user == null) return NotFound();

        var playbackCount = await _context.PlaybackHistories
            .CountAsync(h => h.DeviceId == user.DeviceId);

        ViewData["PlaybackCount"] = playbackCount;
        return View(user);
    }

    // POST: /Users/ChangeRole
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(string id, string newRole)
    {
        var validRoles = new[] { "Guest", "User", "Premium", "Banned" };
        if (!validRoles.Contains(newRole))
            return BadRequest("Role không hợp lệ.");

        var user = await _context.MobileUsers.FindAsync(id);
        if (user == null) return NotFound();

        user.Role = newRole;
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã đổi role của {user.DisplayName} thành {newRole}.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // POST: /Users/Delete
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var user = await _context.MobileUsers.FindAsync(id);
        if (user == null) return NotFound();

        _context.MobileUsers.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã xóa tài khoản {user.DisplayName}.";
        return RedirectToAction(nameof(Index));
    }

    // POST: /Users/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            TempData["Error"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var user = await _context.MobileUsers.FindAsync(id);
        if (user == null) return NotFound();

        var hasher = new PasswordHasher<MobileUser>();
        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Đã reset mật khẩu cho {user.DisplayName}.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
