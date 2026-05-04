using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages.Tours;

[QueryProperty(nameof(TourId), "tourId")]
public partial class TourDetailPage : ContentPage, INotifyPropertyChanged
{
    private readonly DatabaseService _dbService;
    private string _tourId = string.Empty;
    private Tour? _tour;
    private double _progressPercent;
    private int _visitedCount;

    public string TourId
    {
        get => _tourId;
        set
        {
            _tourId = value;
            LoadTourAsync();
        }
    }

    public ObservableCollection<TourPoiViewModel> TourPois { get; } = new();

    public double ProgressPercent
    {
        get => _progressPercent;
        set
        {
            _progressPercent = value;
            OnPropertyChanged();
        }
    }

    public int VisitedCount
    {
        get => _visitedCount;
        set
        {
            _visitedCount = value;
            OnPropertyChanged();
        }
    }

    public TourDetailPage(DatabaseService dbService)
    {
        InitializeComponent();
        _dbService = dbService;
        BindingContext = this;
        PoiCollection.ItemsSource = TourPois;
    }

    private async void LoadTourAsync()
    {
        if (string.IsNullOrEmpty(TourId)) return;

        try
        {
            _tour = await _dbService.GetTourByIdAsync(TourId);
            if (_tour == null)
            {
                await DisplayAlert("Lỗi", "Không tìm thấy tour", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Update UI
            TourName.Text = _tour.Name;
            TourDescription.Text = _tour.Description ?? "Khám phá ẩm thực Vĩnh Khánh";

            // Load POIs
            await LoadTourPoisAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TourDetail] Error loading tour: {ex.Message}");
        }
    }

    private async Task LoadTourPoisAsync()
    {
        TourPois.Clear();

        try
        {
            var pois = await _dbService.GetTourPoisAsync(TourId);
            int order = 1;
            int visited = 0;

            foreach (var poi in pois)
            {
                var isVisited = await IsPoiVisitedAsync(poi.Id);
                if (isVisited) visited++;

                var vm = new TourPoiViewModel
                {
                    Id = poi.Id,
                    Title = poi.Title,
                    Category = "Ẩm thực",
                    OrderNumber = order++,
                    IsVisited = isVisited
                };

                TourPois.Add(vm);
            }

            VisitedCount = visited;
            ProgressPercent = pois.Count > 0 ? (double)visited / pois.Count : 0;
            ProgressBar.Progress = ProgressPercent;
            ProgressText.Text = $"{visited}/{pois.Count} quán đã thử";

            EmptyPois.IsVisible = TourPois.Count == 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TourDetail] Error loading POIs: {ex.Message}");
        }
    }

    private async Task<bool> IsPoiVisitedAsync(string poiId)
    {
        return await Task.FromResult(Preferences.Default.Get<bool>($"visited_{poiId}", false));
    }

    private async void OnStartTourClicked(object sender, EventArgs e)
    {
        if (TourPois.Count == 0)
        {
            await DisplayAlert("Thông báo", "Tour này chưa có quán nào", "OK");
            return;
        }

        // Navigate to MapPage with this tour active
        await Shell.Current.GoToAsync($"//map?tourId={TourId}");
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class TourPoiViewModel : INotifyPropertyChanged
{
    private bool _isVisited;

    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int OrderNumber { get; set; }
    public string DistanceText { get; set; } = "Cách bạn ~100m";

    public bool IsVisited
    {
        get => _isVisited;
        set
        {
            _isVisited = value;
            OnPropertyChanged();
            // Save preference
            Preferences.Default.Set($"visited_{Id}", value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
