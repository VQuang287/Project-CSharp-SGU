using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TourMap.Models;
using TourMap.Services;

namespace TourMap.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly DatabaseService _dataService;

    public ObservableCollection<Poi> Pois { get; } = new();

    [ObservableProperty]
    private Poi? _selectedPoi;

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
