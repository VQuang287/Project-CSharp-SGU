using System.Data.Common;
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

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

var jwtSecret =
    builder.Configuration["JwtSettings:SecretKey"]
    ?? builder.Configuration["Jwt:Key"]
    ?? "ChangeThisJwtKey_ToAtLeast32Characters_2026!";
var jwtIssuer =
    builder.Configuration["JwtSettings:Issuer"]
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "TourMap.AdminWeb";
var jwtAudience =
    builder.Configuration["JwtSettings:Audience"]
    ?? builder.Configuration["Jwt:Audience"]
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
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "TourMap API", Version = "v1" });
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

var keyDir = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "keys");
Directory.CreateDirectory(keyDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keyDir))
    .SetApplicationName("TourMap.AdminWeb");

builder.Services.AddHttpClient<IAITranslationService, AITranslationService>();

builder.Services.AddDbContext<AdminDbContext>(options =>
    options.UseSqlite("Data Source=AdminTourMap.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    db.Database.EnsureCreated();
    EnsureCompatibilityTables(db);
    EnsureCompatibilityColumns(db);

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

    if (!db.AdminUsers.Any())
    {
        var username = app.Configuration["AdminBootstrap:Username"] ?? "admin";
        var password = app.Configuration["AdminBootstrap:Password"] ?? "ChangeMe-Now-123!";
        var hasher = new PasswordHasher<AdminUser>();
        var adminUser = new AdminUser
        {
            Username = username,
            Role = "Administrator",
            IsActive = true
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, password);
        db.AdminUsers.Add(adminUser);
        db.SaveChanges();
        logger.LogWarning("Bootstrap admin user created. Please change AdminBootstrap credentials after first login.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "TourMap API V1");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Pois}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

static void EnsureCompatibilityTables(AdminDbContext db)
{
    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "MobileUsers" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_MobileUsers" PRIMARY KEY,
            "DeviceId" TEXT NOT NULL,
            "Token" TEXT NULL,
            "CreatedAt" TEXT NOT NULL,
            "LastLoginAt" TEXT NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "AdminUsers" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_AdminUsers" PRIMARY KEY AUTOINCREMENT,
            "Username" TEXT NOT NULL,
            "PasswordHash" TEXT NOT NULL,
            "Role" TEXT NOT NULL,
            "IsActive" INTEGER NOT NULL,
            "FailedLoginCount" INTEGER NOT NULL DEFAULT 0,
            "LockedUntilUtc" TEXT NULL,
            "LastLoginUtc" TEXT NULL
        );
        """);

    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "Tours" (
            "Id" TEXT NOT NULL CONSTRAINT "PK_Tours" PRIMARY KEY,
            "Name" TEXT NOT NULL,
            "Description" TEXT NULL,
            "IsActive" INTEGER NOT NULL,
            "CreatedAt" TEXT NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "TourPoiMappings" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_TourPoiMappings" PRIMARY KEY AUTOINCREMENT,
            "TourId" TEXT NOT NULL,
            "PoiId" TEXT NOT NULL,
            "OrderIndex" INTEGER NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "QrCodeEntries" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_QrCodeEntries" PRIMARY KEY AUTOINCREMENT,
            "PoiId" TEXT NOT NULL,
            "DeepLink" TEXT NOT NULL,
            "QrImageUrl" TEXT NOT NULL,
            "CreatedAt" TEXT NOT NULL
        );
        """);

    db.Database.ExecuteSqlRaw(
        """
        CREATE TABLE IF NOT EXISTS "UserLocationLogs" (
            "Id" INTEGER NOT NULL CONSTRAINT "PK_UserLocationLogs" PRIMARY KEY AUTOINCREMENT,
            "UserAnonId" TEXT NULL,
            "Latitude" REAL NOT NULL,
            "Longitude" REAL NOT NULL,
            "RecordedAt" TEXT NOT NULL
        );
        """);
}

static void EnsureCompatibilityColumns(AdminDbContext db)
{
    EnsureColumn(db, "PlaybackHistories", "TriggerType", "TEXT NOT NULL DEFAULT 'GPS'");
    EnsureColumn(db, "PlaybackHistories", "DurationSeconds", "INTEGER NOT NULL DEFAULT 0");
    EnsureColumn(db, "PlaybackHistories", "IsCompleted", "INTEGER NOT NULL DEFAULT 0");
    EnsureColumn(db, "Pois", "UpdatedAt", "TEXT NOT NULL DEFAULT '2026-01-01T00:00:00Z'");
    EnsureColumn(db, "Pois", "AudioLocalPath", "TEXT NULL");

    // Multilingual AI
    EnsureColumn(db, "Pois", "DescriptionEn", "TEXT NULL");
    EnsureColumn(db, "Pois", "AudioUrlEn", "TEXT NULL");
    EnsureColumn(db, "Pois", "DescriptionZh", "TEXT NULL");
    EnsureColumn(db, "Pois", "AudioUrlZh", "TEXT NULL");
    EnsureColumn(db, "Pois", "DescriptionKo", "TEXT NULL");
    EnsureColumn(db, "Pois", "AudioUrlKo", "TEXT NULL");
    EnsureColumn(db, "Pois", "DescriptionJa", "TEXT NULL");
    EnsureColumn(db, "Pois", "AudioUrlJa", "TEXT NULL");
    EnsureColumn(db, "Pois", "DescriptionFr", "TEXT NULL");
    EnsureColumn(db, "Pois", "AudioUrlFr", "TEXT NULL");
}

static void EnsureColumn(AdminDbContext db, string tableName, string columnName, string columnSqlDefinition)
{
    using var connection = db.Database.GetDbConnection();
    if (connection.State != System.Data.ConnectionState.Open)
    {
        connection.Open();
    }

    if (HasColumn(connection, tableName, columnName))
    {
        return;
    }

    using var command = connection.CreateCommand();
    command.CommandText = $"ALTER TABLE \"{tableName}\" ADD COLUMN \"{columnName}\" {columnSqlDefinition};";
    command.ExecuteNonQuery();
}

static bool HasColumn(DbConnection connection, string tableName, string columnName)
{
    using var command = connection.CreateCommand();
    command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
    using var reader = command.ExecuteReader();
    while (reader.Read())
    {
        var existingColumn = reader["name"]?.ToString();
        if (string.Equals(existingColumn, columnName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
    }

    return false;
}
