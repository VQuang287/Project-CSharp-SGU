namespace TourMap.AdminWeb.ViewModels;

public class AnalyticsDashboardViewModel
{
    public int TotalPlays7Days { get; set; }
    public int TotalPlays30Days { get; set; }
    public double AverageDurationSeconds { get; set; }
    public double CompletionRatePercent { get; set; }
    public List<TopPoiItem> TopPois7Days { get; set; } = new();
    public List<TriggerTypeItem> TriggerTypeBreakdown7Days { get; set; } = new();
    public List<DailyPlayItem> DailyPlays7Days { get; set; } = new();
}

public class TopPoiItem
{
    public string PoiId { get; set; } = string.Empty;
    public string PoiTitle { get; set; } = string.Empty;
    public int PlayCount { get; set; }
}

public class TriggerTypeItem
{
    public string TriggerType { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class DailyPlayItem
{
    public DateOnly Day { get; set; }
    public int Count { get; set; }
}
