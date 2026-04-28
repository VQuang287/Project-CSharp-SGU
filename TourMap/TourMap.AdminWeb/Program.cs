using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;
using TourMap.AdminWeb.Services;
using TourMap.AdminWeb.Hubs;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
var keyDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

var jwtSecret =
    builder.Configuration["JwtSettings:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? "ChangeThisJwtKey_ToAtLeast32Characters_2026!";
var jwtIssuer =
    builder.Configuration["JwtSettings:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "TourMap.AdminWeb";
var jwtAudience =
    builder.Configuration["JwtSettings:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "TourMap.MobileApp";

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/Login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.FromSeconds(30)
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // SignalR websocket requests commonly send bearer token via query string.
                if (!string.IsNullOrEmpty(accessToken)
                    && path.StartsWithSegments("/hubs/devices", StringComparison.OrdinalIgnoreCase))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// === RBAC Authorization Policies ===
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("AuthenticatedUser", policy => policy.RequireRole("User", "Premium", "Administrator"));
    options.AddPolicy("PremiumContent", policy => policy.RequireRole("Premium", "Administrator"));
    options.AddPolicy("MobileAccess", policy => policy.RequireRole("Guest", "User", "Premium", "MobileUser"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "TourMap REST API", 
        Version = "v1",
        Description = "Tài liệu API chính thức cho ứng dụng di động TourMap. Gồm các chức năng Auth, Định vị, Tracking và Quản lý POI."
    });

    // Bật XML Comments trên Swagger UI
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if(File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

Directory.CreateDirectory(keyDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDir))
    .SetApplicationName("TourMap.AdminWeb");

builder.Services.AddHttpClient<IAITranslationService, AITranslationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
});

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlServer(connectionString)
        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// Background service to cleanup stale device connections
builder.Services.AddHostedService<DeviceCleanupService>();

var app = builder.Build();
app.Logger.LogInformation("AdminWeb content root: {ContentRoot}", builder.Environment.ContentRootPath);
app.Logger.LogInformation("Using SQL Server database");
app.Logger.LogInformation("AdminWeb key ring path: {KeyRingPath}", keyDir);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // Apply pending migrations (production-ready approach)
    try
    {
        await db.Database.MigrateAsync();
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error applying database migrations. Ensure database exists and connection string is valid.");
        // Fallback to EnsureCreated for first-time setup (dev only)
        if (builder.Environment.IsDevelopment())
        {
            logger.LogWarning("Falling back to EnsureCreated for development...");
            await db.Database.EnsureCreatedAsync();
        }
        else
        {
            throw; // In production, fail fast if migrations can't be applied
        }
    }

    // Seed initial POIs
    try
    {
        if (!db.Pois.Any())
        {
            db.Pois.AddRange(
                new Poi { Id = Guid.NewGuid().ToString(), Title = "Ngã 4 Hoàng Diệu", Description = "Giao lộ Hoàng Diệu - Vĩnh Khánh", Latitude = 10.7618898, Longitude = 106.7020039, Priority = 1, RadiusMeters = 30, MapLink = "https://maps.app.goo.gl/xyz123", UpdatedAt = DateTime.UtcNow },
                new Poi { Id = Guid.NewGuid().ToString(), Title = "Ốc Oanh", Description = "Trọng điểm phố ẩm thực", Latitude = 10.7608247, Longitude = 106.7034143, Priority = 10, RadiusMeters = 40, MapLink = "https://maps.app.goo.gl/x2x", UpdatedAt = DateTime.UtcNow },
                new Poi { Id = Guid.NewGuid().ToString(), Title = "Út Xiêm Quán", Description = "Quán ăn nổi tiếng", Latitude = 10.7611784, Longitude = 106.705375, Priority = 5, RadiusMeters = 30, UpdatedAt = DateTime.UtcNow },
                new Poi { Id = Guid.NewGuid().ToString(), Title = "Ngã 3 Tôn Đản", Description = "Giao lộ Tôn Đản - Vĩnh Khánh", Latitude = 10.760456, Longitude = 106.707236, Priority = 2, RadiusMeters = 50, UpdatedAt = DateTime.UtcNow }
            );
            db.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error seeding initial data");
    }

    // Cleanup stale device connections on startup
    try
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddSeconds(-30);
        var staleDevices = db.DeviceConnections
            .Where(d => d.LastHeartbeatAt <= fiveMinutesAgo || d.State == ConnectionState.Offline)
            .ToList();
        
        if (staleDevices.Any())
        {
            db.DeviceConnections.RemoveRange(staleDevices);
            db.SaveChanges();
            logger.LogInformation("Startup cleanup: removed {Count} stale device connections", staleDevices.Count);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error during startup device cleanup");
    }

    // === BOOTSTRAP ADMIN USER ===
    // SECURITY: Chỉ tạo admin khi có cấu hình rõ ràng, không dùng fallback password
    {
        var username = app.Configuration["AdminBootstrap:Username"];
        var password = app.Configuration["AdminBootstrap:Password"];
        
        // Bắt buộc phải cấu hình qua environment variable hoặc config, không có fallback
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("[Bootstrap] AdminBootstrap:Username và AdminBootstrap:Password chưa được cấu hình. Bỏ qua tạo admin tự động.");
        }
        else
        {
            var hasher = new PasswordHasher<AdminUser>();
            var existingAdmin = db.AdminUsers.FirstOrDefault(u => u.Username == username);
            
            if (existingAdmin == null)
            {
                // First run — create admin from configured credentials
                var newAdmin = new AdminUser
                {
                    Username = username,
                    Role = "Administrator",
                    IsActive = true
                };
                newAdmin.PasswordHash = hasher.HashPassword(newAdmin, password);
                db.AdminUsers.Add(newAdmin);
                db.SaveChanges();
                logger.LogWarning($"[Bootstrap] Admin user '{username}' đã được tạo. HÃY ĐỔI MẬT KHẨU NGAY sau lần đăng nhập đầu tiên!");
            }
            else
            {
                // Validate existing hash — if invalid/legacy format, rehash với password từ config
                var verifyResult = hasher.VerifyHashedPassword(existingAdmin, existingAdmin.PasswordHash ?? "", password);
                if (verifyResult == PasswordVerificationResult.Failed && 
                    (string.IsNullOrEmpty(existingAdmin.PasswordHash) || !existingAdmin.PasswordHash.StartsWith("AQ")))
                {
                    // Legacy plain-text or invalid hash — rehash với password từ config
                    existingAdmin.PasswordHash = hasher.HashPassword(existingAdmin, password);
                    existingAdmin.FailedLoginCount = 0;
                    existingAdmin.LockedUntilUtc = null;
                    db.SaveChanges();
                    logger.LogWarning($"[Bootstrap] Admin '{username}' đã được rehash mật khẩu. HÃY ĐỔI MẬT KHẨU NGAY sau lần đăng nhập đầu tiên!");
                }
            }
        }
    }

    // MOCK DATA for analytics
    if (!db.PlaybackHistories.Any())
    {
        var rnd = new Random();
        var poiIds = db.Pois.Select(p => p.Id).ToList();
        var triggers = new[] { "GPS", "GPS", "GPS", "QR", "QR", "MANUAL" };
        var plays = new List<PlaybackHistory>();
        for (int i = 0; i < 150; i++)
        {
            plays.Add(new PlaybackHistory
            {
                PoiId = poiIds[rnd.Next(poiIds.Count)],
                Timestamp = DateTime.UtcNow.AddDays(-rnd.Next(0, 7)),
                TriggerType = triggers[rnd.Next(triggers.Length)],
                DurationSeconds = rnd.Next(30, 180),
                IsCompleted = rnd.Next(0, 2) == 1
            });
        }
        db.PlaybackHistories.AddRange(plays);
        db.SaveChanges();
    }

    if (!db.UserLocationLogs.Any())
    {
        var rnd = new Random();
        var logs = new List<UserLocationLog>();
        // Mock points around Q4 Vinh Khanh (Latitude: ~10.761, Longitude: ~106.704)
        for (int i = 0; i < 500; i++)
        {
            logs.Add(new UserLocationLog
            {
                UserAnonId = "device_" + rnd.Next(1, 10),
                Latitude = 10.761 + (rnd.NextDouble() - 0.5) * 0.005,
                Longitude = 106.704 + (rnd.NextDouble() - 0.5) * 0.005,
                RecordedAt = DateTime.UtcNow.AddHours(-rnd.Next(0, 72))
            });
        }
        db.UserLocationLogs.AddRange(logs);
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseRouting();

// Chỉ bật Swagger trong Development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TourMap API V1");
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pois}/{action=Index}/{id?}")
    .WithStaticAssets();

// SignalR Hub endpoint
app.MapHub<DeviceTrackingHub>("/hubs/devices");

app.Run();
