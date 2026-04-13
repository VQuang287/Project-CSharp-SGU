using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;
using System.IdentityModel.Tokens.Jwt;

namespace TourMap.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;
    private string? _deviceId;

    public string? CurrentToken { get; private set; }
    public UserProfile? CurrentUser { get; private set; }
    public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken) && !IsTokenExpired();
    public bool IsGuest => CurrentUser == null || CurrentUser.Role == "Guest";

    public AuthService(IHttpClientFactory httpClientFactory, ILoggerService logger)
    {
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _logger.LogInformation("AuthService initialized");
    }

    // ═══════════════════════════════════════════════════════════
    // Initialize — Check saved tokens from SecureStorage
    // ═══════════════════════════════════════════════════════════
    public async Task InitializeAsync()
    {
        _logger.LogInformation("AuthService.InitializeAsync called");

        try
        {
            // Try to load cached token
            CurrentToken = await SecureStorage.Default.GetAsync("access_token");
            var profileJson = await SecureStorage.Default.GetAsync("user_profile");

            if (!string.IsNullOrEmpty(profileJson))
            {
                CurrentUser = JsonSerializer.Deserialize<UserProfile>(profileJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }

            _logger.LogInformation($"Token found: {!string.IsNullOrEmpty(CurrentToken)}, IsExpired: {IsTokenExpired()}");

            // If token expired, try refresh
            if (!string.IsNullOrEmpty(CurrentToken) && IsTokenExpired())
            {
                _logger.LogInformation("Token expired, attempting refresh...");
                var refreshed = await RefreshTokenAsync();
                if (!refreshed)
                {
                    _logger.LogInformation("Refresh failed, clearing tokens");
                    await ClearTokensAsync();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"AuthService init error: {ex.Message}");
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Login with Email + Password
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> LoginAsync(string email, string password)
    {
        try
        {
            _deviceId = GetOrCreateDeviceId();
            var request = new { Email = email, Password = password, DeviceId = _deviceId };

            foreach (var baseUrl in GetAuthBaseUrls())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/login-user", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            return new AuthResult(true);
                        }
                    }
                    else
                    {
                        var error = await response.Content.ReadAsStringAsync();
                        _logger.LogInformation($"Login failed: {response.StatusCode} - {error}");
                        return new AuthResult(false, "Email hoặc mật khẩu không đúng.");
                    }
                }
                catch (HttpRequestException) { continue; }
                catch (TaskCanceledException) { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Login error: {ex.Message}");
        }

        return new AuthResult(false, "Không thể kết nối đến server.");
    }

    // ═══════════════════════════════════════════════════════════
    // Register new account
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> RegisterAsync(string email, string password, string displayName)
    {
        try
        {
            _deviceId = GetOrCreateDeviceId();
            var request = new { Email = email, Password = password, DisplayName = displayName, DeviceId = _deviceId };

            foreach (var baseUrl in GetAuthBaseUrls())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/register", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            return new AuthResult(true);
                        }
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        // Try parse error message
                        try
                        {
                            var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                            if (errorObj.TryGetProperty("message", out var msg) || errorObj.TryGetProperty("Message", out msg))
                                return new AuthResult(false, msg.GetString());
                        }
                        catch { }
                        return new AuthResult(false, "Đăng ký thất bại. Vui lòng thử lại.");
                    }
                }
                catch (HttpRequestException) { continue; }
                catch (TaskCanceledException) { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Register error: {ex.Message}");
        }

        return new AuthResult(false, "Không thể kết nối đến server.");
    }

    // ═══════════════════════════════════════════════════════════
    // Anonymous Device Login (Guest mode — kept for backward compatibility)
    // ═══════════════════════════════════════════════════════════
    public async Task<bool> LoginAnonymousAsync()
    {
        try
        {
            _deviceId = GetOrCreateDeviceId();
            var request = new { DeviceId = _deviceId };

            foreach (var baseUrl in GetAuthBaseUrls())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/login", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            return true;
                        }
                    }
                }
                catch (HttpRequestException) { continue; }
                catch (TaskCanceledException) { continue; }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth Error: {ex.Message}");
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════
    // Refresh Token
    // ═══════════════════════════════════════════════════════════
    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync("refresh_token");
            if (string.IsNullOrEmpty(refreshToken)) return false;

            var request = new { RefreshToken = refreshToken };

            foreach (var baseUrl in GetAuthBaseUrls())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/refresh", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            return true;
                        }
                    }
                }
                catch (HttpRequestException) { continue; }
                catch (TaskCanceledException) { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Refresh error: {ex.Message}");
        }

        return false;
    }

    // ═══════════════════════════════════════════════════════════
    // Logout
    // ═══════════════════════════════════════════════════════════
    public async Task LogoutAsync()
    {
        await ClearTokensAsync();
        CurrentToken = null;
        CurrentUser = null;
        _logger.LogInformation("User logged out");
    }

    // ═══════════════════════════════════════════════════════════
    // Helpers
    // ═══════════════════════════════════════════════════════════

    private async Task SaveTokensAsync(AuthResponse result)
    {
        CurrentToken = result.AccessToken;
        CurrentUser = result.User;

        await SecureStorage.Default.SetAsync("access_token", result.AccessToken);
        await SecureStorage.Default.SetAsync("refresh_token", result.RefreshToken);
        await SecureStorage.Default.SetAsync("user_profile",
            JsonSerializer.Serialize(result.User));

        _logger.LogInformation($"Tokens saved. Role: {result.User?.Role}");
    }

    private async Task ClearTokensAsync()
    {
        SecureStorage.Default.Remove("access_token");
        SecureStorage.Default.Remove("refresh_token");
        SecureStorage.Default.Remove("user_profile");
        await Task.CompletedTask;
    }

    private bool IsTokenExpired()
    {
        if (string.IsNullOrEmpty(CurrentToken)) return true;
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(CurrentToken);
            return token.ValidTo <= DateTime.UtcNow;
        }
        catch
        {
            return true;
        }
    }

    private string GetOrCreateDeviceId()
    {
        var id = Preferences.Default.Get<string>("device_uuid", string.Empty);
        if (string.IsNullOrEmpty(id))
        {
            id = Guid.NewGuid().ToString();
            Preferences.Default.Set("device_uuid", id);
        }
        return id;
    }

    private static IReadOnlyList<string> GetAuthBaseUrls()
    {
        return new[]
        {
            // Port must match AdminWeb launchSettings.json (5042)
            // 10.0.2.2 = host machine IP from Android emulator
            "http://10.0.2.2:5042/api/v1/auth",
            "http://localhost:5042/api/v1/auth"
        };
    }
}

// ═══════════════════════════════════════════════════════════
// DTOs (shared between AuthService methods)
// ═══════════════════════════════════════════════════════════

public record AuthResult(bool Success, string? ErrorMessage = null);

public class UserProfile
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string DisplayName { get; set; } = "Khách";
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = "Guest";
    public string AuthProvider { get; set; } = "local";
    public DateTime CreatedAt { get; set; }
}

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public UserProfile User { get; set; } = new();
}
