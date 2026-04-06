using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Poi> Pois { get; set; }
    public DbSet<PlaybackHistory> PlaybackHistories { get; set; }
    public DbSet<MobileUser> MobileUsers { get; set; }
    public DbSet<Tour> Tours { get; set; }
    public DbSet<TourPoiMapping> TourPoiMappings { get; set; }
    public DbSet<QrCodeEntry> QrCodeEntries { get; set; }
    public DbSet<AdminUser> AdminUsers { get; set; }
}
