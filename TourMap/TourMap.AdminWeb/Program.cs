using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
    });

// Ép Key xác thực lưu trên RAM. Khi Server tắt, toàn bộ Cookie cũ của người dùng tự động phế bỏ
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();

builder.Services.AddHttpClient<IAITranslationService, AITranslationService>();

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite("Data Source=AdminTourMap.db"));

var app = builder.Build();

// Auto-migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();

    // Mồi sẵn 4 dữ liệu Vĩnh Khánh vào Database của Web Admin để người dùng không phải gõ tay
    if (!db.Pois.Any())
    {
        db.Pois.AddRange(
            new TourMap.AdminWeb.Models.Poi { Id = Guid.NewGuid().ToString(), Title = "Ngã 4 Hoàng Diệu", Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh", Latitude = 10.7618898, Longitude = 106.7020039, Priority = 1, RadiusMeters = 30, MapLink="https://maps.app.goo.gl/xyz123" },
            new TourMap.AdminWeb.Models.Poi { Id = Guid.NewGuid().ToString(), Title = "Ốc Oanh", Description = "Trọng điểm phố ẩm thực", Latitude = 10.7608247, Longitude = 106.7034143, Priority = 10, RadiusMeters = 40, MapLink="https://maps.app.goo.gl/x2x" },
            new TourMap.AdminWeb.Models.Poi { Id = Guid.NewGuid().ToString(), Title = "Ớt Xiêm Quán", Description = "Quán ăn nổi tiếng", Latitude = 10.7611784, Longitude = 106.705375, Priority = 5, RadiusMeters = 30 },
            new TourMap.AdminWeb.Models.Poi { Id = Guid.NewGuid().ToString(), Title = "Ngã 3 Tôn Đản", Description = "Giao lộ Tôn Đản - Vĩnh Khánh", Latitude = 10.760456, Longitude = 106.707236, Priority = 2, RadiusMeters = 50 }
        );
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pois}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
