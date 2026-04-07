using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using TourMap.Services;

namespace TourMap.Platforms.Android;

[Service(ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class LocationForegroundService : Service
{
    private const int NotificationId = 1001;
    private const string ChannelId = "TourMap_GPS_Channel";

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "STOP_SERVICE")
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
                StopForeground(StopForegroundFlags.Remove);
            else
                StopForeground(true); // Fallback cho máy cũ
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        // Tạo Notification Channel (Android 8.0+)
        CreateNotificationChannel();

        // Intent khi user nhấn vào Notification (mở lại app)
        try
        {
            // Android 12+ requires immutable pending intents for security
            PendingIntentFlags pendingFlags = OperatingSystem.IsAndroidVersionAtLeast(31) 
                ? PendingIntentFlags.Immutable 
                : PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
                
            var pendingIntent = PendingIntent.GetActivity(this, 0, 
                new Intent(this, typeof(MainActivity)), 
                pendingFlags);

            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("TourMap đang hoạt động")
                .SetContentText("Đang dò tìm tọa độ GPS để phát Audio Guide...")
                .SetSmallIcon(Resource.Mipmap.appicon) // Icon mặc định của MAUI app
                .SetContentIntent(pendingIntent)
                .SetOngoing(true)
                .Build();

            if (notification != null)
            {
                StartForeground(NotificationId, notification);
            }
        }
        catch (Java.Lang.Exception jex)
        {
            Console.WriteLine($"[LocationService] Java exception creating notification: {jex.Message}");
            Console.WriteLine($"[LocationService] Java exception type: {jex.GetType().Name}");
            // Still try to start service with minimal notification
            var notification = new NotificationCompat.Builder(this, ChannelId)
                .SetContentTitle("TourMap")
                .SetContentText("GPS Tracking Active")
                .SetSmallIcon(Resource.Mipmap.appicon)
                .SetOngoing(true)
                .Build();
            StartForeground(NotificationId, notification);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocationService] Exception creating notification: {ex.Message}");
            Console.WriteLine($"[LocationService] Exception type: {ex.GetType().Name}");
            throw;
        }

        // Báo cho OS biết service này muốn sống dai
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(26)) // Android 8.0+
            {
                var channel = new NotificationChannel(ChannelId, "TourMap GPS Tracking", NotificationImportance.Low)
                {
                    Description = "Thông báo khi ứng dụng đang dò GPS ngầm"
                };
                var manager = (NotificationManager?)GetSystemService(NotificationService);
                manager?.CreateNotificationChannel(channel);
            }
        }
        catch (Java.Lang.Exception jex)
        {
            Console.WriteLine($"[LocationService] Java exception creating notification channel: {jex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LocationService] Exception creating notification channel: {ex.Message}");
        }
    }
}
