using Android.App;
using Android.Content.PM;
using Android.OS;

namespace TourMap
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // MAUI 8/10 automatically handles permission callbacks via ActivityResultCallbackRegistry
        // No manual forwarding needed - keeping override for Android lifecycle compatibility only
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            // Permission results are automatically handled by MAUI's Permissions class
        }
    }
}
