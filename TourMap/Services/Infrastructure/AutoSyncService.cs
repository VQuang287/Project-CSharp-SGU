using System.Threading;

namespace TourMap.Services;

/// <summary>
/// Coordinates app-wide background POI sync so screens don't each invent their own sync flow.
/// </summary>
public sealed class AutoSyncService
{
    private static readonly TimeSpan MinimumSyncInterval = TimeSpan.FromSeconds(30);

    private readonly SyncService _syncService;
    private readonly TourRuntimeService _tourRuntimeService;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    private DateTime _lastAttemptUtc = DateTime.MinValue;

    public AutoSyncService(SyncService syncService, TourRuntimeService tourRuntimeService)
    {
        _syncService = syncService;
        _tourRuntimeService = tourRuntimeService;
    }

    public event EventHandler<AutoSyncCompletedEventArgs>? SyncCompleted;

    public async Task<bool> EnsureSyncedAsync(string reason, bool force = false, CancellationToken cancellationToken = default)
    {
        if (!force && DateTime.UtcNow - _lastAttemptUtc < MinimumSyncInterval)
            return false;

        if (Connectivity.NetworkAccess != NetworkAccess.Internet)
        {
            Console.WriteLine($"[AutoSync] Skip sync ({reason}) - no internet");
            return false;
        }

        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            if (!force && DateTime.UtcNow - _lastAttemptUtc < MinimumSyncInterval)
                return false;

            _lastAttemptUtc = DateTime.UtcNow;

            foreach (var serverUrl in BackendEndpoints.GetCandidateServerBaseUrls())
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    if (!await _syncService.SyncPoisFromServerAsync(serverUrl))
                        continue;

                    BackendEndpoints.RememberWorkingServerFromUrl(serverUrl);
                    await _tourRuntimeService.RefreshPoisAsync();

                    var args = new AutoSyncCompletedEventArgs(reason, serverUrl, DateTime.UtcNow);
                    SyncCompleted?.Invoke(this, args);

                    Console.WriteLine($"[AutoSync] Sync completed from {serverUrl} ({reason})");
                    return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AutoSync] Sync failed at {serverUrl} ({reason}): {ex.Message}");
                }
            }

            return false;
        }
        finally
        {
            _syncLock.Release();
        }
    }
}

public sealed class AutoSyncCompletedEventArgs : EventArgs
{
    public AutoSyncCompletedEventArgs(string reason, string serverUrl, DateTime syncedAtUtc)
    {
        Reason = reason;
        ServerUrl = serverUrl;
        SyncedAtUtc = syncedAtUtc;
    }

    public string Reason { get; }
    public string ServerUrl { get; }
    public DateTime SyncedAtUtc { get; }
}
