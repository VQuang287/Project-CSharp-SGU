using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AdminDbContext _db;
    private readonly IConfiguration _config;
    private readonly PasswordHasher<MobileUser> _hasher = new();

    public AuthApiController(AdminDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ═══════════════════════════════════════════════════════════
    // 1. Anonymous Device Login (giữ nguyên cho Guest mode)
    // ═══════════════════════════════════════════════════════════
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] DeviceLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { Message = "DeviceId is required." });

        var user = await _db.MobileUsers.FirstOrDefaultAsync(u => u.DeviceId == request.DeviceId);
        if (user == null)
        {
            user = new MobileUser
            {
                DeviceId = request.DeviceId,
                Role = "Guest",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            _db.MobileUsers.Add(user);
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(60);
        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400, // 24 hours
            User = MapToUserProfile(user)
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 2. Register (Email + Password)
    // ═══════════════════════════════════════════════════════════
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Message = "Email và mật khẩu không được để trống." });

        if (!IsValidEmail(request.Email))
            return BadRequest(new { Message = "Email không hợp lệ." });

        if (request.Password.Length < 6)
            return BadRequest(new { Message = "Mật khẩu phải có ít nhất 6 ký tự." });

        var emailLower = request.Email.Trim().ToLowerInvariant();

        // Check duplicate
        var exists = await _db.MobileUsers.AnyAsync(u => u.Email == emailLower);
        if (exists)
            return Conflict(new { Message = "Email này đã được đăng ký." });

        var user = new MobileUser
        {
            DeviceId = request.DeviceId ?? Guid.NewGuid().ToString(),
            Email = emailLower,
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? emailLower.Split('@')[0] : request.DisplayName.Trim(),
            Role = "User",
            AuthProvider = "local",
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(60);

        _db.MobileUsers.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = GenerateJwtToken(user);

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400,
            User = MapToUserProfile(user)
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 3. Login User (Email + Password)
    // ═══════════════════════════════════════════════════════════
    [HttpPost("login-user")]
    public async Task<IActionResult> LoginUser([FromBody] UserLoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { Message = "Email và mật khẩu không được để trống." });

        var emailLower = request.Email.Trim().ToLowerInvariant();
        var user = await _db.MobileUsers.FirstOrDefaultAsync(u => u.Email == emailLower);

        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return Unauthorized(new { Message = "Email hoặc mật khẩu không đúng." });

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            return Unauthorized(new { Message = "Email hoặc mật khẩu không đúng." });

        // Update last login & link device if provided
        user.LastLoginAt = DateTime.UtcNow;
        if (!string.IsNullOrWhiteSpace(request.DeviceId))
            user.DeviceId = request.DeviceId;

        // Rehash if needed
        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = _hasher.HashPassword(user, request.Password);

        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(60);

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresIn = 86400,
            User = MapToUserProfile(user)
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 4. Refresh Token
    // ═══════════════════════════════════════════════════════════
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            return BadRequest(new { Message = "Refresh token is required." });

        var user = await _db.MobileUsers
            .FirstOrDefaultAsync(u => u.RefreshToken == request.RefreshToken);

        if (user == null || user.RefreshTokenExpiresAt < DateTime.UtcNow)
            return Unauthorized(new { Message = "Refresh token không hợp lệ hoặc đã hết hạn." });

        // Rotate tokens
        var newAccessToken = GenerateJwtToken(user);
        var newRefreshToken = GenerateRefreshToken();
        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(60);
        user.LastLoginAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresIn = 86400,
            User = MapToUserProfile(user)
        });
    }

    // ═══════════════════════════════════════════════════════════
    // 5. Get Profile (Authenticated)
    // ═══════════════════════════════════════════════════════════
    [HttpGet("profile")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _db.MobileUsers.FindAsync(userId);
        if (user == null) return NotFound();

        return Ok(MapToUserProfile(user));
    }

    // ═══════════════════════════════════════════════════════════
    // 6. Update Profile (Authenticated)
    // ═══════════════════════════════════════════════════════════
    [HttpPut("profile")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _db.MobileUsers.FindAsync(userId);
        if (user == null) return NotFound();

        if (!string.IsNullOrWhiteSpace(request.DisplayName))
            user.DisplayName = request.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl))
            user.AvatarUrl = request.AvatarUrl;

        await _db.SaveChangesAsync();

        return Ok(MapToUserProfile(user));
    }

    // ═══════════════════════════════════════════════════════════
    // 7. Change Password (Authenticated)
    // ═══════════════════════════════════════════════════════════
    [HttpPost("change-password")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var user = await _db.MobileUsers.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
            return NotFound();

        var verify = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (verify == PasswordVerificationResult.Failed)
            return BadRequest(new { Message = "Mật khẩu hiện tại không đúng." });

        if (request.NewPassword.Length < 6)
            return BadRequest(new { Message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

        user.PasswordHash = _hasher.HashPassword(user, request.NewPassword);
        await _db.SaveChangesAsync();

        return Ok(new { Message = "Đổi mật khẩu thành công." });
    }

    // ═══════════════════════════════════════════════════════════
    // Helper Methods
    // ═══════════════════════════════════════════════════════════

    private string GenerateJwtToken(MobileUser user)
    {
        var secretKey = _config["JwtSettings:SecretKey"]
            ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? "ChangeThisJwtKey_ToAtLeast32Characters_2026!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new("DeviceId", user.DeviceId),
            new("DisplayName", user.DisplayName),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"]
                ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
                ?? "TourMap.AdminWeb",
            audience: _config["JwtSettings:Audience"]
                ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
                ?? "TourMap.MobileApp",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    private static UserProfileDto MapToUserProfile(MobileUser user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        DisplayName = user.DisplayName,
        AvatarUrl = user.AvatarUrl,
        Role = user.Role,
        AuthProvider = user.AuthProvider,
        CreatedAt = user.CreatedAt
    };

    private static bool IsValidEmail(string email) =>
        Regex.IsMatch(email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
}

// ═══════════════════════════════════════════════════════════
// DTOs
// ═══════════════════════════════════════════════════════════

public class DeviceLoginRequest
{
    public string DeviceId { get; set; } = string.Empty;
}

public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? DeviceId { get; set; }
}

public class UserLoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceId { get; set; }
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class UpdateProfileRequest
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserProfileDto User { get; set; } = new();
}

public class UserProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = "Khách";
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Guest";
    public string AuthProvider { get; set; } = "local";
    public DateTime CreatedAt { get; set; }
}
