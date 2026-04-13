using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace TourMap.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ILoggerService _logger;
    private string? _deviceId;

    public string? CurrentToken { get; private set; }

    public AuthService(ILoggerService logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
        
        _logger.LogInformation("AuthService initialized");
    }

    public async Task InitializeAsync()
    {
        _logger.LogInformation("AuthService.InitializeAsync called");
        
        // Thử lấy token đã lưu từ lần trước
        CurrentToken = await SecureStorage.Default.GetAsync("auth_token");
        _logger.LogInformation($"Retrieved existing token: {!string.IsNullOrEmpty(CurrentToken)}");

        // Nếu chưa có, tiến hành đăng nhập ẩn danh để lấy token mới
        if (string.IsNullOrEmpty(CurrentToken))
        {
            _logger.LogInformation("No existing token found, proceeding with device login");
            // Device ID logic...
        }
    }

    public async Task<bool> LoginAnonymousAsync()
    {
        try
        {
            _deviceId = Preferences.Default.Get<string>("device_uuid", string.Empty);
            if (string.IsNullOrEmpty(_deviceId))
            {
                _deviceId = Guid.NewGuid().ToString();
                Preferences.Default.Set("device_uuid", _deviceId);
            }

            var request = new { DeviceId = _deviceId };
            foreach (var baseUrl in GetAuthBaseUrls())
            {
                var response = await _httpClient.PostAsJsonAsync($"{baseUrl}/login", request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<LoginResponse>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        CurrentToken = result.Token;
                        await SecureStorage.Default.SetAsync("auth_token", CurrentToken);
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth Error: {ex.Message}");
            Console.WriteLine($"Auth Stack trace: {ex.StackTrace}");
            
            // Handle specific error types
            if (ex is HttpRequestException httpEx)
            {
                Console.WriteLine($"Auth Network error: {httpEx.StatusCode}");
                Console.WriteLine($"Auth - Check if server is running and accessible");
            }
            else if (ex is TaskCanceledException)
            {
                Console.WriteLine($"Auth Request timeout - server may be slow or unreachable");
            }
            else if (ex is JsonException jsonEx)
            {
                Console.WriteLine($"Auth JSON parsing error: {jsonEx.Message}");
                Console.WriteLine($"Auth - Server response format may be invalid");
            }
            else if (ex is System.Security.SecurityException secEx)
            {
                Console.WriteLine($"Auth Security error: {secEx.Message}");
                Console.WriteLine($"Auth - Check secure storage permissions");
            }
        }

        return false;
    }

    private static IReadOnlyList<string> GetAuthBaseUrls()
    {
        return new[]
        {
            "http://10.0.2.2:5000/api/v1/auth",
            "http://localhost:5000/api/v1/auth"
        };
    }

    private class LoginResponse
    {
        public string? Token { get; set; }
        public DateTime ValidTo { get; set; }
        public string? DeviceId { get; set; }
    }
}
