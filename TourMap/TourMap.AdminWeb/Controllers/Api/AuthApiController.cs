using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TourMap.AdminWeb.Models;
using TourMap.AdminWeb.Data;
using System.Text;

namespace TourMap.AdminWeb.Controllers.Api;

[ApiController]
[Route("api/v1/auth")]
public class AuthApiController : ControllerBase
{
    private readonly AdminDbContext _db;
    private readonly IConfiguration _config;

    public AuthApiController(AdminDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    /// <summary>
    /// Đăng nhập ẩn danh: Mobile App truyền DeviceId lên để đổi lấy JWT Bearer Token.
    /// Token này dùng để gọi các API đồng bộ dữ liệu.
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { Message = "DeviceId is required." });

        var user = await _db.MobileUsers.FirstOrDefaultAsync(u => u.DeviceId == request.DeviceId);
        if (user == null)
        {
            user = new MobileUser
            {
                DeviceId = request.DeviceId,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow
            };
            _db.MobileUsers.Add(user);
        }
        else
        {
            user.LastLoginAt = DateTime.UtcNow;
            _db.MobileUsers.Update(user);
        }

        await _db.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        
        return Ok(new
        {
            Token = token,
            ValidTo = DateTime.UtcNow.AddDays(30),
            DeviceId = user.DeviceId
        });
    }

    private string GenerateJwtToken(MobileUser user)
    {
        var secretKey = _config["JwtSettings:SecretKey"] ?? "TourMap_Super_Secret_Key_For_Demo_Development_32Bytes!!";
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim("DeviceId", user.DeviceId),
            new Claim(ClaimTypes.Role, "MobileUser"),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _config["JwtSettings:Issuer"] ?? "TourMapServer",
            audience: _config["JwtSettings:Audience"] ?? "TourMapMobileApp",
            claims: claims,
            expires: DateTime.UtcNow.AddDays(30), // Sống lâu cho tiện demo
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

public class LoginRequest
{
    public string DeviceId { get; set; } = string.Empty;
}
