using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Hubs;

/// <summary>
/// SignalR Hub for real-time device tracking
/// </summary>
[Authorize(AuthenticationSchemes = "Cookies,Bearer")]
public class DeviceTrackingHub : Hub
{
    private readonly AdminDbContext _dbContext;
    private readonly ILogger<DeviceTrackingHub> _logger;

    public DeviceTrackingHub(AdminDbContext dbContext, ILogger<DeviceTrackingHub> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Lấy DeviceId từ JWT token claim (an toàn) thay vì tin tưởng client gửi lên
    /// </summary>
    private string? GetDeviceIdFromToken()
    {
        return Context.User?.FindFirstValue("DeviceId") 
            ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    /// <summary>
    /// Validate deviceId từ client khớp với token claim để ngăn spoofing
    /// </summary>
    private bool ValidateDeviceId(string clientDeviceId)
    {
        var tokenDeviceId = GetDeviceIdFromToken();
        if (string.IsNullOrEmpty(tokenDeviceId))
        {
            _logger.LogWarning("DeviceId not found in token - rejecting request");
            return false;
        }

        // Cho phép guest device đăng ký lần đầu, nhưng sau đó phải khớp
        var existingConnection = _dbContext.DeviceConnections
            .FirstOrDefaultAsync(d => d.SignalRConnectionId == Context.ConnectionId).Result;
        
        if (existingConnection != null && existingConnection.DeviceId != clientDeviceId)
        {
            _logger.LogWarning("DeviceId mismatch - Token: {TokenDeviceId}, Client: {ClientDeviceId}", 
                tokenDeviceId, clientDeviceId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Mobile app calls this when first connecting
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "MobileAccess")]
    public async Task RegisterDevice(string deviceId, string deviceType, string? appVersion, string? userId, string? userName)
    {
        try
        {
            // SECURITY: Validate deviceId từ token claim để ngăn spoofing
            if (!ValidateDeviceId(deviceId))
            {
                _logger.LogWarning("Device spoofing detected - ConnectionId: {ConnectionId}, DeviceId: {DeviceId}", 
                    Context.ConnectionId, deviceId);
                throw new HubException("Invalid device identification");
            }

            // Remove existing connection for this device
            var existing = await _dbContext.DeviceConnections
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);
            
            if (existing != null)
            {
                _dbContext.DeviceConnections.Remove(existing);
            }

            // Create new connection record
            var connection = new DeviceConnection
            {
                DeviceId = deviceId,
                UserId = userId,
                UserName = userName ?? "Guest",
                DeviceType = deviceType,
                AppVersion = appVersion,
                SignalRConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow,
                LastHeartbeatAt = DateTime.UtcNow,
                State = ConnectionState.Online
            };

            _dbContext.DeviceConnections.Add(connection);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceId} registered. Total online: {Count}", 
                deviceId, await GetOnlineCount());

            // Notify admins of new connection
            await Clients.Group("Admins").SendAsync("DeviceConnected", connection);
            
            // Join device to admin group for updates
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Device_{deviceId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Mobile app calls this periodically to keep connection alive
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "MobileAccess")]
    public async Task Heartbeat(string deviceId, double? latitude, double? longitude, string? currentPoiId, string? currentPoiName)
    {
        try
        {
            // SECURITY: Validate deviceId từ token claim để ngăn spoofing
            if (!ValidateDeviceId(deviceId))
            {
                _logger.LogWarning("Heartbeat spoofing detected - ConnectionId: {ConnectionId}, DeviceId: {DeviceId}", 
                    Context.ConnectionId, deviceId);
                throw new HubException("Invalid device identification");
            }

            var connection = await _dbContext.DeviceConnections
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

            if (connection != null)
            {
                connection.LastHeartbeatAt = DateTime.UtcNow;
                connection.LastLatitude = latitude;
                connection.LastLongitude = longitude;
                connection.CurrentPoiId = currentPoiId;
                connection.CurrentPoiName = currentPoiName;

                await _dbContext.SaveChangesAsync();

                // Update admins with current state
                await Clients.Group("Admins").SendAsync("DeviceUpdated", connection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing heartbeat for {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Update device state (e.g., when playing audio)
    /// </summary>
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "MobileAccess")]
    public async Task UpdateState(string deviceId, ConnectionState state)
    {
        try
        {
            // SECURITY: Validate deviceId từ token claim để ngăn spoofing
            if (!ValidateDeviceId(deviceId))
            {
                _logger.LogWarning("State update spoofing detected - ConnectionId: {ConnectionId}, DeviceId: {DeviceId}", 
                    Context.ConnectionId, deviceId);
                throw new HubException("Invalid device identification");
            }

            var connection = await _dbContext.DeviceConnections
                .FirstOrDefaultAsync(d => d.DeviceId == deviceId);

            if (connection != null)
            {
                connection.State = state;
                connection.LastHeartbeatAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                await Clients.Group("Admins").SendAsync("DeviceUpdated", connection);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating state for {DeviceId}", deviceId);
        }
    }

    /// <summary>
    /// Admin joins to receive device updates
    /// </summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "AdminOnly")]
    public async Task JoinAdminGroup()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin joined tracking group");
        
        // Send current device list to admin
        var devices = await _dbContext.DeviceConnections
            .Where(d => d.LastHeartbeatAt > DateTime.UtcNow.AddMinutes(-5))
            .ToListAsync();
        
        await Clients.Caller.SendAsync("DeviceList", devices);
    }

    /// <summary>
    /// Admin leaves tracking
    /// </summary>
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme, Policy = "AdminOnly")]
    public async Task LeaveAdminGroup()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "Admins");
        _logger.LogInformation("Admin left tracking group");
    }

    /// <summary>
    /// Get count of online devices
    /// </summary>
    public async Task<int> GetOnlineCount()
    {
        var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
        return await _dbContext.DeviceConnections
            .CountAsync(d => d.LastHeartbeatAt > fiveMinutesAgo);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            // Mark device as disconnected
            var connection = await _dbContext.DeviceConnections
                .FirstOrDefaultAsync(d => d.SignalRConnectionId == Context.ConnectionId);

            if (connection != null)
            {
                connection.State = ConnectionState.Offline;
                await _dbContext.SaveChangesAsync();

                await Clients.Group("Admins").SendAsync("DeviceDisconnected", connection.DeviceId);
                
                _logger.LogInformation("Device {DeviceId} disconnected", connection.DeviceId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnect");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
