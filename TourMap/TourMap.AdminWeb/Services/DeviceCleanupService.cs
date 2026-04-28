using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Services;

/// <summary>
/// Background service to cleanup stale device connections
/// </summary>
public class DeviceCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DeviceCleanupService> _logger;

    public DeviceCleanupService(IServiceProvider serviceProvider, ILogger<DeviceCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Device cleanup service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Chạy mỗi 10s
                
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
                
                var thirtySecondsAgo = DateTime.UtcNow.AddSeconds(-30);
                
                // Xóa các device quá hạn (> 30s không heartbeat) hoặc đã offline
                var staleDevices = await dbContext.DeviceConnections
                    .Where(d => d.LastHeartbeatAt <= thirtySecondsAgo || d.State == ConnectionState.Offline)
                    .ToListAsync(stoppingToken);
                
                if (staleDevices.Any())
                {
                    dbContext.DeviceConnections.RemoveRange(staleDevices);
                    await dbContext.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Cleaned up {Count} stale device connections", staleDevices.Count);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during device cleanup");
            }
        }

        _logger.LogInformation("Device cleanup service stopped");
    }
}
