using TourMap.Models;

namespace TourMap.Services;

/// <summary>
/// App-level runtime orchestration:
/// - load latest POIs from SQLite into geofence
/// - start GPS tracking once
/// - route geofence triggers to narration engine
/// </summary>
public class TourRuntimeService
{
    private readonly DatabaseService _databaseService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly IGpsTrackingService _gpsTrackingService;
    private readonly NarrationEngine _narrationEngine;

    private bool _isInitialized;

    public TourRuntimeService(
        DatabaseService databaseService,
        GeofenceEngine geofenceEngine,
        IGpsTrackingService gpsTrackingService,
        NarrationEngine narrationEngine)
    {
        _databaseService = databaseService;
        _geofenceEngine = geofenceEngine;
        _gpsTrackingService = gpsTrackingService;
        _narrationEngine = narrationEngine;
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            await RefreshPoisAsync();
            return;
        }

        var pois = await _databaseService.GetPoisAsync();
        _geofenceEngine.UpdatePois(pois);

        _gpsTrackingService.LocationChanged += OnLocationChanged;
        _geofenceEngine.POITriggered += OnPoiTriggered;
        await _gpsTrackingService.StartTrackingAsync();

        _isInitialized = true;
        Console.WriteLine($"[Runtime] Started with {pois.Count} POIs");
    }

    public async Task RefreshPoisAsync()
    {
        var pois = await _databaseService.GetPoisAsync();
        _geofenceEngine.UpdatePois(pois);
        Console.WriteLine($"[Runtime] Refreshed geofence POIs: {pois.Count}");
    }

    private void OnLocationChanged(Location location)
    {
        _geofenceEngine.OnLocationChanged(location);
    }

    private async void OnPoiTriggered(Poi poi)
    {
        try
        {
            await _narrationEngine.OnPOITriggeredAsync(poi, "GPS");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Runtime] Narration trigger failed: {ex.Message}");
        }
    }
}
