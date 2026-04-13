using System.Collections.ObjectModel;
using TourMap.Models;
using TourMap.Services;

namespace TourMap.ViewModels;

public class MainViewModel : BindableObject
{
    private readonly DatabaseService _dataService;

    public ObservableCollection<Poi> Pois { get; } = new();

    private Poi? _selectedPoi;
    public Poi? SelectedPoi
    {
        get => _selectedPoi;
        set
        {
            _selectedPoi = value;
            OnPropertyChanged();
        }
    }

    public MainViewModel(DatabaseService dataService)
    {
        _dataService = dataService;
    }

    public async Task LoadAsync()
    {
        var list = await _dataService.GetPoisAsync();
        Pois.Clear();
        foreach (var p in list)
            Pois.Add(p);
    }
}
