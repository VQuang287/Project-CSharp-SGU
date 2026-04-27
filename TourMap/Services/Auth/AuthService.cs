using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net;
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

            Exception? lastConnectException = null;

            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    _logger.LogInformation("[Auth] Trying login at {0}", baseUrl);
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/login-user", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            BackendEndpoints.RememberWorkingServerFromUrl(baseUrl);
                            return new AuthResult(true);
                        }

                        return new AuthResult(false, "Server trả về dữ liệu không hợp lệ.");
                    }

                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        return new AuthResult(false, "Email hoặc mật khẩu không đúng.");

                    var error = await response.Content.ReadAsStringAsync();
                    var serverMsg = TryExtractServerMessage(error);
                    _logger.LogWarning("[Auth] Login failed at {0}: {1} - {2}", baseUrl, (int)response.StatusCode, serverMsg ?? error);

                    // Nếu đúng server nhưng đang lỗi 5xx, trả về thông báo rõ ràng.
                    if ((int)response.StatusCode >= 500)
                        return new AuthResult(false, "Server đang lỗi. Vui lòng thử lại sau.");

                    // Với các lỗi khác (404/405...), thử base URL kế tiếp.
                    continue;
                }
                catch (HttpRequestException ex)
                {
                    lastConnectException = ex;
                    _logger.LogWarning("[Auth] Login request failed at {0}: {1}", baseUrl, ex.Message);
                    continue;
                }
                catch (TaskCanceledException ex)
                {
                    lastConnectException = ex;
                    _logger.LogWarning("[Auth] Login timeout at {0}", baseUrl);
                    continue;
                }
            }

            if (lastConnectException != null)
            {
                _logger.LogWarning("[Auth] All login endpoints failed. Last error: {0}", lastConnectException.Message);
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

            Exception? lastConnectException = null;

            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    _logger.LogInformation("[Auth] Trying register at {0}", baseUrl);
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/register", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            BackendEndpoints.RememberWorkingServerFromUrl(baseUrl);
                            return new AuthResult(true);
                        }

                        return new AuthResult(false, "Server trả về dữ liệu không hợp lệ.");
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

                        if ((int)response.StatusCode >= 500)
                            return new AuthResult(false, "Server đang lỗi. Vui lòng thử lại sau.");

                        return new AuthResult(false, "Đăng ký thất bại. Vui lòng thử lại.");
                    }
                }
                catch (HttpRequestException ex)
                {
                    lastConnectException = ex;
                    _logger.LogWarning("[Auth] Register request failed at {0}: {1}", baseUrl, ex.Message);
                    continue;
                }
                catch (TaskCanceledException ex)
                {
                    lastConnectException = ex;
                    _logger.LogWarning("[Auth] Register timeout at {0}", baseUrl);
                    continue;
                }
            }

            if (lastConnectException != null)
            {
                _logger.LogWarning("[Auth] All register endpoints failed. Last error: {0}", lastConnectException.Message);
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
            // QUAN TRỌNG: Xóa token cũ trước khi login dạng khách
            // để không bị dính token của tài khoản đã đăng nhập trước đó
            await ClearTokensAsync();
            CurrentToken = null;
            CurrentUser = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;

            _deviceId = GetOrCreateDeviceId();
            var request = new { DeviceId = _deviceId };

            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    _logger.LogInformation("[Auth] Trying anonymous login at {0}", baseUrl);
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/login", request);
                    if (response.IsSuccessStatusCode)
                    {
                        var result = await response.Content.ReadFromJsonAsync<AuthResponse>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (result != null)
                        {
                            await SaveTokensAsync(result);
                            BackendEndpoints.RememberWorkingServerFromUrl(baseUrl);
                            _logger.LogInformation("[Auth] Anonymous login success. Role: {0}", result.User?.Role);
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

            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
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
                            BackendEndpoints.RememberWorkingServerFromUrl(baseUrl);
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
    // Logout — revoke token trên server rồi xóa local
    // ═══════════════════════════════════════════════════════════
    public async Task LogoutAsync()
    {
        // Best-effort: gọi server để revoke refresh token
        if (!string.IsNullOrEmpty(CurrentToken))
        {
            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/logout");
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken);
                    var response = await _httpClient.SendAsync(message);
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("[Auth] Server-side logout success");
                        break;
                    }
                }
                catch (HttpRequestException) { continue; }
                catch (TaskCanceledException) { continue; }
            }
        }

        // Xóa local tokens
        await ClearTokensAsync();
        CurrentToken = null;
        CurrentUser = null;
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _logger.LogInformation("User logged out");
    }

    // ═══════════════════════════════════════════════════════════
    // Password Management
    // ═══════════════════════════════════════════════════════════
    public async Task<AuthResult> ChangePasswordAsync(string oldPassword, string newPassword)
    {
        try
        {
            if (string.IsNullOrEmpty(CurrentToken)) return new AuthResult(false, "Không có quyền truy cập.");
            var request = new { CurrentPassword = oldPassword, NewPassword = newPassword };

            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    using var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/change-password")
                    {
                        Content = JsonContent.Create(request)
                    };
                    message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CurrentToken);

                    var response = await _httpClient.SendAsync(message);
                    if (response.IsSuccessStatusCode)
                    {
                        return new AuthResult(true);
                    }
                }
                catch (HttpRequestException) { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Change password error: {ex.Message}");
        }
        return new AuthResult(false, "Đổi mật khẩu thất bại. Vui lòng thử lại.");
    }

    public async Task<AuthResult> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new { Email = email };
            foreach (var baseUrl in BackendEndpoints.GetAuthBaseUrls())
            {
                try
                {
                    var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/forgot-password", request);
                    if (response.IsSuccessStatusCode)
                    {
                        return new AuthResult(true);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound ||
                        response.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed)
                    {
                        return new AuthResult(false, "Tính năng khôi phục mật khẩu chưa được hỗ trợ trên server này.");
                    }
                }
                catch (HttpRequestException) { continue; }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Forgot password error: {ex.Message}");
        }
        return new AuthResult(false, "Yêu cầu khôi phục mật khẩu thất bại.");
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


    private static string? TryExtractServerMessage(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            var errorObj = JsonSerializer.Deserialize<JsonElement>(raw);
            if (errorObj.ValueKind == JsonValueKind.Object)
            {
                if (errorObj.TryGetProperty("message", out var msg) || errorObj.TryGetProperty("Message", out msg))
                    return msg.GetString();
            }
        }
        catch { }

        return null;
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
