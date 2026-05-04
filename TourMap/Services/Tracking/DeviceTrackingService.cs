using Microsoft.AspNetCore.SignalR.Client;
using System.ComponentModel;

namespace TourMap.Services;

/// <summary>
/// Service for tracking device connection status with the Admin server via SignalR.
/// Sends heartbeats and location updates in real-time.
/// </summary>
public class DeviceTrackingService : INotifyPropertyChanged, IDisposable
{
    private HubConnection? _hubConnection;
    private readonly ILoggerService _logger;
    private readonly ILocationService? _locationService;
    private readonly AuthService _authService;
    
    private Timer? _heartbeatTimer;
    private readonly SemaphoreSlim _heartbeatLock = new(1, 1);
    private string? _currentPoiId;
    private string? _currentPoiName;
    private bool _isConnected;
    
    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<bool>? ConnectionStateChanged;
    public event EventHandler<string>? OnError;

    public bool IsConnected 
    { 
        get => _isConnected;
        private set 
        { 
            if (_isConnected != value)
            {
                _isConnected = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsConnected)));
                ConnectionStateChanged?.Invoke(this, value);
            }
        }
    }

    public string? DeviceId { get; private set; }
    public string? DeviceType { get; private set; }

    public DeviceTrackingService(
        ILoggerService logger,
        AuthService authService,
        ILocationService? locationService = null)
    {
        _logger = logger;
        _authService = authService;
        _locationService = locationService;
        
        // Generate unique device ID if not exists
        DeviceId = GetOrCreateDeviceId();
        DeviceType = DeviceInfo.Platform.ToString();
        
        // Sửa lỗi FormatException bằng cách không truyền DeviceId vào chuỗi nếu không có tham số format arg
        _logger.LogInformation($"DeviceTrackingService initialized. DeviceId: {DeviceId}");
    }

    /// <summary>
    /// Connects to the SignalR hub on the admin server
    /// </summary>
    public async Task ConnectAsync(string hubUrl)
    {
        if (IsConnected)
        {
            _logger.LogWarning("Already connected to device tracking hub");
            return;
        }

        try
        {
            // Build SignalR URL with JWT token for authentication
            var token = _authService.CurrentToken;
            var hubUrlWithToken = hubUrl;
            if (!string.IsNullOrEmpty(token))
            {
                var separator = hubUrl.Contains("?") ? "&" : "?";
                hubUrlWithToken = $"{hubUrl}{separator}access_token={Uri.EscapeDataString(token)}";
                _logger.LogDebug("Added JWT token to SignalR URL");
            }
            else
            {
                _logger.LogWarning("No JWT token available for SignalR connection");
            }

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(hubUrlWithToken)
                .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
                .Build();

            _hubConnection.On<string>("DeviceDisconnected", (deviceId) =>
            {
                _logger.LogDebug("Device {DeviceId} disconnected from hub", deviceId);
            });

            _hubConnection.Closed += (error) =>
            {
                IsConnected = false;
                _logger.LogWarning("Device tracking hub connection closed: {Error}", error?.Message ?? "Unknown");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnecting += (error) =>
            {
                IsConnected = false;
                _logger.LogWarning("Reconnecting to device tracking hub...");
                return Task.CompletedTask;
            };

            _hubConnection.Reconnected += async (connectionId) =>
            {
                IsConnected = true;
                _logger.LogInformation("Reconnected to device tracking hub. ConnectionId: {ConnectionId}", connectionId);
                // Re-register và gửi heartbeat ngay để CMS cập nhật nhanh
                await RegisterDeviceAsync();
                await SendHeartbeatAsync();
            };

            await _hubConnection.StartAsync();
            IsConnected = true;
            
            _logger.LogInformation("Connected to device tracking hub at {HubUrl}", hubUrl);

            // Register this device
            await RegisterDeviceAsync();

            // Start heartbeat timer
            StartHeartbeatTimer();
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to connect to device tracking hub: {Error}", ex);
            OnError?.Invoke(this, ex.Message);
            IsConnected = false;
        }
    }

    /// <summary>
    /// Registers the device with the server
    /// </summary>
    private async Task RegisterDeviceAsync()
    {
        if (_hubConnection == null || !IsConnected) return;

        try
        {
            var appVersion = AppInfo.VersionString;

            await _hubConnection.InvokeAsync("RegisterDevice",
                DeviceId,
                DeviceType,
                appVersion,
                null,
                "Anonymous");

            _logger.LogInformation("Device registered successfully. DeviceId: {DeviceId}", DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to register device: {Error}", ex);
        }
    }

    /// <summary>
    /// Starts the heartbeat timer to send periodic updates
    /// </summary>
    private void StartHeartbeatTimer()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = new Timer(async _ => 
        {
            // Ngăn callback chồng chập với SemaphoreSlim
            if (!await _heartbeatLock.WaitAsync(0))
            {
                _logger.LogDebug("Heartbeat callback skipped - previous still running");
                return;
            }
            
            try
            {
                await SendHeartbeatAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError("Heartbeat timer error", ex);
            }
            finally
            {
                _heartbeatLock.Release();
            }
        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
        _logger.LogInformation("Heartbeat timer started (5s interval)");
    }

    /// <summary>
    /// Sends heartbeat with current location and POI
    /// </summary>
    private async Task SendHeartbeatAsync()
    {
        if (_hubConnection == null || !IsConnected) return;

        try
        {
            double? lat = null;
            double? lon = null;

            // Get current location if available
            if (_locationService != null)
            {
                try
                {
                    var location = await _locationService.GetCurrentLocationAsync();
                    if (location != null)
                    {
                        lat = location.Latitude;
                        lon = location.Longitude;
                    }
                }
                catch { /* Ignore location errors */ }
            }

            await _hubConnection.InvokeAsync("Heartbeat",
                DeviceId,
                lat,
                lon,
                _currentPoiId,
                _currentPoiName);

            _logger.LogDebug("Heartbeat sent. Location: {Lat}, {Lon}, POI: {Poi}", lat, lon, _currentPoiName ?? "None");
        }
        catch (Exception ex)
        {
            _logger.LogError("Error processing heartbeat for {DeviceId}: {Error}", ex, DeviceId, ex.Message);
        }
    }

    /// <summary>
    /// Updates the current POI being visited
    /// </summary>
    public void UpdateCurrentPoi(string? poiId, string? poiName)
    {
        _currentPoiId = poiId;
        _currentPoiName = poiName;
        
        // Send immediate heartbeat on POI change
        _ = SendHeartbeatAsync();
        
        _logger.LogInformation("Current POI updated: {PoiName} ({PoiId})", poiName ?? "None", poiId ?? "None");
    }

    /// <summary>
    /// Updates the device state (Online, Idle, Playing, Offline)
    /// </summary>
    public async Task UpdateStateAsync(int state)
    {
        if (_hubConnection == null || !IsConnected) 
            throw new InvalidOperationException("Not connected to device tracking hub");

        await _hubConnection.InvokeAsync("UpdateState", DeviceId, state);
        _logger.LogInformation("Device state updated to: {State}", state);
    }

    /// <summary>
    /// Disconnects from the SignalR hub
    /// </summary>
    public async Task DisconnectAsync()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;

        if (_hubConnection != null)
        {
            try
            {
                await _hubConnection.StopAsync();
                await _hubConnection.DisposeAsync();
                _logger.LogInformation("Disconnected from device tracking hub");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error handling disconnect: {Error}", ex, ex.Message);
            }
            finally
            {
                _hubConnection = null;
                IsConnected = false;
            }
        }
    }

    /// <summary>
    /// Gets or creates a unique device ID stored in preferences
    /// </summary>
    private static string GetOrCreateDeviceId()
    {
        var existingId = Preferences.Get("DeviceTrackingId", string.Empty);
        if (!string.IsNullOrEmpty(existingId))
        {
            return existingId;
        }

        var newId = Guid.NewGuid().ToString("N");
        Preferences.Set("DeviceTrackingId", newId);
        return newId;
    }

    public void Dispose()
    {
        _ = DisconnectAsync();
        _heartbeatTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Device connection states matching server enum
/// </summary>
public static class DeviceState
{
    public const int Online = 0;
    public const int Idle = 1;
    public const int Playing = 2;
    public const int Offline = 3;
}
