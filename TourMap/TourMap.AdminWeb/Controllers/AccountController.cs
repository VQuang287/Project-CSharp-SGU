using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers;

// BUG-C04 fix: Use PasswordHasher + AdminUser from database instead of hardcoded credentials
public class AccountController : Controller
{
    private readonly AdminDbContext _db;

    public AccountController(AdminDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
        // Nếu đã đăng nhập rồi thì đá về trang chủ
        if (User.Identity != null && User.Identity.IsAuthenticated) return LocalRedirect(returnUrl);
        
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password, string returnUrl = "/")
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu!";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        var user = await _db.AdminUsers
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
        {
            ViewBag.Error = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa!";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Check account lockout
        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc > DateTime.UtcNow)
        {
            var remaining = (user.LockedUntilUtc.Value - DateTime.UtcNow).TotalMinutes;
            ViewBag.Error = $"Tài khoản đã bị khóa tạm thời. Vui lòng thử lại sau {Math.Ceiling(remaining)} phút.";
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Verify password using ASP.NET Identity PasswordHasher
        var hasher = new PasswordHasher<AdminUser>();
        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginCount++;
            if (user.FailedLoginCount >= 5)
            {
                user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(15);
                await _db.SaveChangesAsync();
                ViewBag.Error = "Đăng nhập sai quá 5 lần. Tài khoản đã bị khóa 15 phút.";
            }
            else
            {
                await _db.SaveChangesAsync();
                ViewBag.Error = $"Mật khẩu không chính xác! Còn {5 - user.FailedLoginCount} lần thử.";
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // Successful login — reset failed attempts
        user.FailedLoginCount = 0;
        user.LockedUntilUtc = null;
        user.LastLoginUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        return LocalRedirect(returnUrl);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }
}
