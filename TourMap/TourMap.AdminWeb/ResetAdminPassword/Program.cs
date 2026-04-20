// =====================================================================
// One-time tool: Reset AdminUser password to a known value
// Run: dotnet run --project ResetAdminPassword.csproj
// =====================================================================
using Microsoft.AspNetCore.Identity;

const string DbPath = "../AdminTourMap.db";
const string TargetUsername = "admin";
const string NewPassword = "admin@2026";

if (!File.Exists(DbPath))
{
    Console.WriteLine($"❌ DB not found at: {Path.GetFullPath(DbPath)}");
    return;
}

// Generate hash using ASP.NET Identity v3 format
var hasher = new PasswordHasher<object>();
var hash = hasher.HashPassword(new object(), NewPassword);

// Update via SQLite ADO.NET (no EF needed)
using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={DbPath}");
conn.Open();

using var cmd = conn.CreateCommand();
cmd.CommandText = @"
    UPDATE AdminUsers 
    SET PasswordHash = @hash,
        FailedLoginCount = 0,
        LockedUntilUtc = NULL,
        IsActive = 1
    WHERE Username = @username;
    SELECT changes();
";
cmd.Parameters.AddWithValue("@hash", hash);
cmd.Parameters.AddWithValue("@username", TargetUsername);

var affected = cmd.ExecuteScalar();
Console.WriteLine(affected?.ToString() == "1"
    ? $"✅ Password reset OK → login: {TargetUsername} / {NewPassword}"
    : $"⚠️  No rows updated. Check username: {TargetUsername}");
