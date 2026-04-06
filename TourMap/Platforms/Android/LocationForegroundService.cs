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
        var pendingIntent = PendingIntent.GetActivity(this, 0, 
            new Intent(this, typeof(MainActivity)), 
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("TourMap đang hoạt động")
            .SetContentText("Đang dò tìm tọa độ GPS để phát Audio Guide...")
            .SetSmallIcon(Resource.Mipmap.appicon) // Icon mặc định của MAUI app
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();

        StartForeground(NotificationId, notification);

        // Báo cho OS biết service này muốn sống dai
        return StartCommandResult.Sticky;
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var channel = new NotificationChannel(ChannelId, "TourMap GPS Tracking", NotificationImportance.Low)
            {
                Description = "Thông báo khi ứng dụng đang dò GPS ngầm"
            };
            var manager = (NotificationManager?)GetSystemService(NotificationService);
            manager?.CreateNotificationChannel(channel);
        }
    }
}
