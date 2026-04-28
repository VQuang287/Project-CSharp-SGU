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
    private readonly DeviceTrackingService _deviceTrackingService;
    private readonly AutoDownloadService _autoDownloadService;

    private bool _isInitialized;
    private bool _disposed;
    private string? _currentPoiId;

    /// <summary>Relays GPS updates to UI (MapPage) without double-subscribing GPS.</summary>
    public event Action<Location>? LocationUpdated;

    public TourRuntimeService(
        DatabaseService databaseService,
        GeofenceEngine geofenceEngine,
        IGpsTrackingService gpsTrackingService,
        NarrationEngine narrationEngine,
        DeviceTrackingService deviceTrackingService,
        AutoDownloadService autoDownloadService)
    {
        _databaseService = databaseService;
        _geofenceEngine = geofenceEngine;
        _gpsTrackingService = gpsTrackingService;
        _narrationEngine = narrationEngine;
        _deviceTrackingService = deviceTrackingService;
        _autoDownloadService = autoDownloadService;
        
        // Subscribe to narration state changes for device tracking
        _narrationEngine.StateChanged += OnNarrationStateChanged;
    }
    
    private async void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        // Update device tracking state based on narration (async void là OK cho event handler)
        try
        {
            switch (state)
            {
                case NarrationState.Playing:
                    await _deviceTrackingService.UpdateStateAsync(DeviceState.Playing);
                    break;
                case NarrationState.Cooldown:
                    await _deviceTrackingService.UpdateStateAsync(DeviceState.Idle);
                    break;
                default:
                    await _deviceTrackingService.UpdateStateAsync(DeviceState.Online);
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TourRuntime] Error updating device state: {ex.Message}");
        }
    }

    public async Task InitializeAsync()
    {
        var pois = await _databaseService.GetPoisAsync();
        _geofenceEngine.UpdatePois(pois);

        if (!_isInitialized)
        {
            // Single GPS subscription point (SYS-C02 fix)
            _gpsTrackingService.LocationChanged += OnLocationChanged;
            _geofenceEngine.POITriggered += OnPoiTriggered;
            _isInitialized = true;
        }

        // If permission was previously denied, callers may try again later.
        // Always attempt to start tracking if not currently tracking.
        if (!_gpsTrackingService.IsTracking)
        {
            await _gpsTrackingService.StartTrackingAsync();
        }

        Console.WriteLine($"[Runtime] Initialized. POIs: {pois.Count}, GPS tracking: {_gpsTrackingService.IsTracking}");
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
            // Update device tracking with current POI
            _currentPoiId = poi.Id;
            _deviceTrackingService.UpdateCurrentPoi(poi.Id, poi.Title);

            // Auto-download audio cho POI này (fire-and-forget, không block narration)
            _ = Task.Run(async () =>
            {
                try
                {
                    await _autoDownloadService.EnsurePoiAudioDownloadedAsync(poi.Id, poi.AudioUrl);
                }
                catch (Exception ex)
                {
                    // Log nhưng không ảnh hưởng narration
                    Console.WriteLine($"[Runtime] Auto-download failed for POI {poi.Id}: {ex.Message}");
                }
            });
            
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
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
        _gpsTrackingService.StopTracking();
        _deviceTrackingService.Dispose();
        _disposed = true;
    }
}
