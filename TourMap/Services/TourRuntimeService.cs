using TourMap.Models;

namespace TourMap.Services;

/// <summary>
/// App-level runtime orchestration:
/// - load latest POIs from SQLite into geofence
/// - OWN GPS tracking lifecycle (SYS-C02 fix — single subscription point)
/// - route geofence triggers to narration engine
/// - expose LocationUpdated for UI consumers (MapPage)
/// </summary>
public class TourRuntimeService : IDisposable
{
    private readonly DatabaseService _databaseService;
    private readonly GeofenceEngine _geofenceEngine;
    private readonly IGpsTrackingService _gpsTrackingService;
    private readonly NarrationEngine _narrationEngine;

    private bool _isInitialized;
    private bool _disposed;

    /// <summary>Relays GPS updates to UI (MapPage) without double-subscribing GPS.</summary>
    public event Action<Location>? LocationUpdated;

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

        // Single GPS subscription point (SYS-C02 fix)
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
        // Relay to UI consumers (MapPage) — SYS-C02 fix
        LocationUpdated?.Invoke(location);
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
            Console.WriteLine($"[Runtime] Stack trace: {ex.StackTrace}");
            Console.WriteLine($"[Runtime] POI ID: {poi.Id}, Title: {poi.Title}");

            // Handle specific error types
            if (ex is ArgumentNullException)
            {
                Console.WriteLine($"[Runtime] POI data is null or invalid");
            }
            else if (ex is InvalidOperationException invalidEx)
            {
                Console.WriteLine($"[Runtime] Narration engine in invalid state: {invalidEx.Message}");
            }
            else if (ex is TaskCanceledException)
            {
                Console.WriteLine($"[Runtime] Narration task was cancelled");
            }
            else if (ex is System.IO.IOException ioEx)
            {
                Console.WriteLine($"[Runtime] Audio IO error: {ioEx.Message}");
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _gpsTrackingService.LocationChanged -= OnLocationChanged;
        _geofenceEngine.POITriggered -= OnPoiTriggered;
        _gpsTrackingService.StopTracking();
        _disposed = true;
    }
}
