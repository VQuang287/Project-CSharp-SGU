namespace TourMap.AdminWeb.Models.ViewModels;

public sealed class AdminDashboardViewModel
{
    public int TotalPois { get; set; }
    public int ActiveTours { get; set; }
    public int TotalPlays { get; set; }
    public int ActiveMobileUsers { get; set; }
    public int TotalQrCodes { get; set; }

    public List<PoiPlaybackItem> TopPois { get; set; } = new();
}

public sealed class PoiPlaybackItem
{
    public string PoiId { get; set; } = string.Empty;
    public string PoiTitle { get; set; } = string.Empty;
    public int PlayCount { get; set; }
}
