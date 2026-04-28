using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using static TourMap.AdminWeb.Models.ConnectionState;

namespace TourMap.AdminWeb.Controllers;

/// <summary>
/// Base controller cho tất cả admin controllers.
/// Tự động set ViewData["OnlineDevices"] cho navbar badge.
/// </summary>
[Authorize]
public abstract class BaseAdminController : Controller
{
    protected readonly AdminDbContext _context;

    protected BaseAdminController(AdminDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tự động set ViewData["OnlineDevices"] trước khi action thực thi
    /// </summary>
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Set online device count cho navbar badge
        ViewData["OnlineDevices"] = await GetOnlineDeviceCountAsync();
        
        await next();
    }

    /// <summary>
    /// Get số lượng thiết bị đang online (heartbeat trong 30 giây gần đây và không ở trạng thái offline)
    /// </summary>
    protected async Task<int> GetOnlineDeviceCountAsync()
    {
        var thirtySecondsAgo = DateTime.UtcNow.AddSeconds(-30);
        return await _context.DeviceConnections
            .CountAsync(d => d.LastHeartbeatAt > thirtySecondsAgo && d.State != Offline);
    }
}
