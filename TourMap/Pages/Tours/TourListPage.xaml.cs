using System.Collections.ObjectModel;
using System.Linq;
using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages.Tours;

public partial class TourListPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly SyncService _syncService;

    public ObservableCollection<TourViewModel> Tours { get; } = new();

    public TourListPage(
        DatabaseService dbService,
        SyncService syncService)
    {
        InitializeComponent();
        _dbService = dbService;
        _syncService = syncService;
        
        BindingContext = this;
        ToursCollection.ItemsSource = Tours;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadToursAsync();
    }

    private async Task LoadToursAsync()
    {
        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        Tours.Clear();

        try
        {
            var tours = await _dbService.GetActiveToursAsync();
            
            foreach (var tour in tours)
            {
                // Lấy danh sách POI của tour
                var pois = await _dbService.GetTourPoisAsync(tour.Id);

                var vm = new TourViewModel
                {
                    Id = tour.Id,
                    Name = tour.Name,
                    Description = tour.Description ?? "Khám phá ẩm thực Vĩnh Khánh",
                    ThumbnailUrl = tour.ThumbnailUrl,
                    PoiCount = pois.Count,
                    Pois = pois
                };

                Tours.Add(vm);
            }

            EmptyState.IsVisible = Tours.Count == 0;
            Console.WriteLine($"[TourList] Loaded {Tours.Count} tours");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TourList] Error loading tours: {ex.Message}");
            await DisplayAlertAsync("Lỗi", $"Không thể tải danh sách tour: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
            LoadingIndicator.IsVisible = false;
        }
    }

    private async void OnTourSelected(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is TourViewModel tour)
        {
            ToursCollection.SelectedItem = null;
            
            // Navigate to TourDetailPage
            await Shell.Current.GoToAsync($"//tourdetail?tourId={tour.Id}");
        }
    }

    private async void OnSyncClicked(object? sender, EventArgs e)
    {
        LoadingIndicator.IsRunning = true;
        
        try
        {
            var candidates = BackendEndpoints.GetCandidateServerBaseUrls();
            var synced = false;

            foreach (var serverUrl in candidates)
            {
                var poiSuccess = await _syncService.SyncPoisFromServerAsync(serverUrl);
                if (!poiSuccess)
                    continue;

                var tourSuccess = await _syncService.SyncToursFromServerAsync(serverUrl);
                if (!tourSuccess)
                    continue;

                BackendEndpoints.RememberWorkingServerFromUrl(serverUrl);
                synced = true;
                break;
            }

            if (synced)
            {
                await DisplayAlertAsync("Thành công", "Đã đồng bộ dữ liệu", "OK");
                await LoadToursAsync();
            }
            else
            {
                await DisplayAlertAsync("Lỗi", "Không kết nối được backend. Kiểm tra server hoặc URL rồi thử lại.", "OK");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TourList] Sync error: {ex.Message}");
            await DisplayAlertAsync("Lỗi", $"Đồng bộ thất bại: {ex.Message}", "OK");
        }
        finally
        {
            LoadingIndicator.IsRunning = false;
        }
    }
}

public class TourViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int PoiCount { get; set; }
    public List<Poi> Pois { get; set; } = new();
}
