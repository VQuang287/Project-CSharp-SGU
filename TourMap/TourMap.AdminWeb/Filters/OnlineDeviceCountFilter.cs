using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Data;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Filters;

/// <summary>
/// Action filter tự động set ViewData["OnlineDevices"] cho MỌI MVC page.
/// Đăng ký global trong Program.cs để navbar badge luôn cập nhật.
/// </summary>
public class OnlineDeviceCountFilter : IAsyncActionFilter
{
    private readonly AdminDbContext _dbContext;

    public OnlineDeviceCountFilter(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var resultContext = await next();

        // Chỉ set ViewData cho MVC controller (không phải API)
        if (resultContext.Controller is Controller controller)
        {
            var fiveMinutesAgo = DateTime.UtcNow.AddMinutes(-5);
            var count = await _dbContext.DeviceConnections
                .CountAsync(d => d.State != ConnectionState.Offline && d.LastHeartbeatAt > fiveMinutesAgo);

            controller.ViewData["OnlineDevices"] = count;
        }
    }
}
