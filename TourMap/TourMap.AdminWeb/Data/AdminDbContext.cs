using Microsoft.EntityFrameworkCore;
using TourMap.AdminWeb.Models;

namespace TourMap.AdminWeb.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options) : base(options) { }

    public DbSet<Poi> Pois { get; set; }
    public DbSet<PlaybackHistory> PlaybackHistories { get; set; }
}
