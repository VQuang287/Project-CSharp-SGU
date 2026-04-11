using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Controllers.Api;

[Route("api/v1/auth")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly PasswordHasher<AdminUser> _passwordHasher = new();

    public AuthController(AdminDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("token")]
    public async Task<IActionResult> Token([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { success = false, message = "Username/password are required." });
        }

        var user = await _context.AdminUsers.FirstOrDefaultAsync(x => x.Username == request.Username && x.IsActive);
        if (user == null)
        {
            return Unauthorized(new { success = false });
        }

        if (user.LockedUntilUtc.HasValue && user.LockedUntilUtc.Value > DateTime.UtcNow)
        {
            return Unauthorized(new { success = false, message = "Account is temporarily locked." });
        }

        var verify = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verify is not (PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded))
        {
            user.FailedLoginCount += 1;
            if (user.FailedLoginCount >= 5)
            {
                user.LockedUntilUtc = DateTime.UtcNow.AddMinutes(10);
                user.FailedLoginCount = 0;
            }
            await _context.SaveChangesAsync();
            return Unauthorized(new { success = false });
        }

        user.FailedLoginCount = 0;
        user.LockedUntilUtc = null;
        user.LastLoginUtc = DateTime.UtcNow;
        if (verify == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        }
        await _context.SaveChangesAsync();

        var key = _configuration["Jwt:Key"] ?? "replace-this-with-long-random-key-please";
        var issuer = _configuration["Jwt:Issuer"] ?? "TourMap.AdminWeb";
        var audience = _configuration["Jwt:Audience"] ?? "TourMap.MobileApp";
        var expiresMinutes = int.TryParse(_configuration["Jwt:ExpireMinutes"], out var mins) ? mins : 60;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiresMinutes),
            signingCredentials: credentials);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            tokenType = "Bearer",
            expiresIn = expiresMinutes * 60
        });
    }

    public sealed class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
