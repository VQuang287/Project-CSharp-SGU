// =====================================================================
// One-time tool: Reset AdminUser password to a known value (SQL Server)
// Run from TourMap.AdminWeb folder:
//   dotnet run --project .\ResetAdminPassword\ResetAdminPassword.csproj
// =====================================================================
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

const string TargetUsername = "admin";
const string NewPassword = "admin123";
const string EnvConnectionStringName = "TOURMAP_ADMINWEB_CONNECTION_STRING";

string? ReadConnectionStringFromJson(string path)
{
    if (!File.Exists(path))
        return null;

    try
    {
        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        if (doc.RootElement.TryGetProperty("ConnectionStrings", out var cs)
            && cs.TryGetProperty("DefaultConnection", out var defaultConn)
            && defaultConn.ValueKind == JsonValueKind.String)
        {
            return defaultConn.GetString();
        }
    }
    catch
    {
        // ignore malformed file and continue probing
    }

    return null;
}

var connectionString = Environment.GetEnvironmentVariable(EnvConnectionStringName);

if (string.IsNullOrWhiteSpace(connectionString))
{
    var probePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.Development.json"),
        Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.Development.json")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "appsettings.json"))
    };

    connectionString = probePaths
        .Select(ReadConnectionStringFromJson)
        .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine("❌ Connection string not found.");
    Console.WriteLine($"Set env var {EnvConnectionStringName} or run this tool from TourMap.AdminWeb folder.");
    return;
}

// Generate ASP.NET Identity-compatible hash
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), NewPassword);

using var conn = new SqlConnection(connectionString);
await conn.OpenAsync();

// 1) Try update existing admin row
using var updateCmd = conn.CreateCommand();
updateCmd.CommandText = @"
    UPDATE [AdminUsers]
    SET [PasswordHash] = @hash,
        [FailedLoginCount] = 0,
        [LockedUntilUtc] = NULL,
        [IsActive] = 1
    WHERE [Username] = @username;
";
updateCmd.Parameters.AddWithValue("@hash", hash);
updateCmd.Parameters.AddWithValue("@username", TargetUsername);

var affected = await updateCmd.ExecuteNonQueryAsync();

// 2) If user does not exist, create it
if (affected == 0)
{
    using var insertCmd = conn.CreateCommand();
    insertCmd.CommandText = @"
        INSERT INTO [AdminUsers] ([Username], [PasswordHash], [Role], [IsActive], [FailedLoginCount], [LockedUntilUtc], [LastLoginUtc])
        VALUES (@username, @hash, 'Administrator', 1, 0, NULL, NULL);
    ";
    insertCmd.Parameters.AddWithValue("@username", TargetUsername);
    insertCmd.Parameters.AddWithValue("@hash", hash);
    await insertCmd.ExecuteNonQueryAsync();

    Console.WriteLine($"✅ Admin user created and password set: {TargetUsername} / {NewPassword}");
}
else
{
    Console.WriteLine($"✅ Password reset OK: {TargetUsername} / {NewPassword}");
}
