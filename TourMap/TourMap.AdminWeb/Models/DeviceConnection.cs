namespace TourMap.AdminWeb.Models;

/// <summary>
/// Tracks active device connections for real-time monitoring
/// </summary>
public class DeviceConnection
{
    public int Id { get; set; }
    
    /// <summary>
    /// Unique device identifier
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated user ID (if authenticated)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// User display name
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// Device type (Android, iOS, etc)
    /// </summary>
    public string DeviceType { get; set; } = "Unknown";
    
    /// <summary>
    /// App version
    /// </summary>
    public string? AppVersion { get; set; }
    
    /// <summary>
    /// Current user location (if available)
    /// </summary>
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
    
    /// <summary>
    /// Current active POI (if any)
    /// </summary>
    public string? CurrentPoiId { get; set; }
    public string? CurrentPoiName { get; set; }
    
    /// <summary>
    /// Connection state
    /// </summary>
    public ConnectionState State { get; set; } = ConnectionState.Online;
    
    /// <summary>
    /// When the device first connected
    /// </summary>
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last heartbeat timestamp
    /// </summary>
    public DateTime LastHeartbeatAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// SignalR Connection ID
    /// </summary>
    public string? SignalRConnectionId { get; set; }
}

public enum ConnectionState
{
    Online,
    Idle,
    Playing,
    Offline
}
