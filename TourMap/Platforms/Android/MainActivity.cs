using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using Microsoft.Maui.Storage;
using System;
using System.Globalization;
using TourMap.Services;

namespace TourMap
{
    [Activity(
        Theme = "@style/Maui.SplashTheme", 
        MainLauncher = true, 
        LaunchMode = LaunchMode.SingleTop, 
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density | ConfigChanges.Locale | ConfigChanges.LayoutDirection)]
    [IntentFilter(new[] { Android.Content.Intent.ActionView },
        Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
        DataSchemes = new[] { "audiotour" },
        DataHosts = new[] { "poi" })]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HandleIntent(Intent);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            HandleIntent(intent);
        }

        void HandleIntent(Intent? intent)
        {
            try
            {
                var data = intent?.DataString;
                if (!string.IsNullOrEmpty(data))
                {
                    // Store pending deeplink so app startup logic can navigate to it
                    Preferences.Default.Set("pending_deeplink", data);
                    Console.WriteLine($"[MainActivity] Saved pending deeplink: {data}");
                }
                // Some Play Store installs (or older flows) provide a "referrer" extra containing
                // key=value pairs like "deep_link=audiotour%3A%2F%2Fpoi%2F{id}&utm_source=qr".
                var referrer = intent?.GetStringExtra("referrer");
                if (!string.IsNullOrEmpty(referrer))
                {
                    try
                    {
                        string? ParseDeepLinkFromReferrer(string raw)
                        {
                            var parts = raw.Split('&');
                            foreach (var p in parts)
                            {
                                var kv = p.Split('=', 2);
                                if (kv.Length == 2 && kv[0] == "deep_link")
                                {
                                    return System.Net.WebUtility.UrlDecode(kv[1]);
                                }
                            }
                            return null;
                        }

                        var deep = ParseDeepLinkFromReferrer(referrer);
                        if (!string.IsNullOrEmpty(deep))
                        {
                            Preferences.Default.Set("pending_deeplink", deep);
                            Console.WriteLine($"[MainActivity] Saved pending deeplink from referrer: {deep}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[MainActivity] Error parsing referrer: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainActivity] Error handling intent: {ex}");
            }
        }

        /// <summary>
        /// Detect system language change while app is in foreground.
        /// </summary>
        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            SyncSystemLanguage();
        }

        /// <summary>
        /// Detect system language change when returning from Settings.
        /// </summary>
        protected override void OnResume()
        {
            base.OnResume();
            SyncSystemLanguage();
        }

        /// <summary>
        /// Đồng bộ ngôn ngữ app theo ngôn ngữ hệ thống Android.
        /// </summary>
        private void SyncSystemLanguage()
        {
            try
            {
                var sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                var loc = LocalizationService.Current;
                
                if (loc.CurrentLanguage != sysLang)
                {
                    var supportedCodes = LocalizationService.SupportedLanguages
                        .Select(l => l.Code).ToHashSet();
                    
                    if (supportedCodes.Contains(sysLang))
                    {
                        Console.WriteLine($"[MainActivity] System language changed → {sysLang}, syncing app");
                        loc.CurrentLanguage = sysLang;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainActivity] Error syncing language: {ex.Message}");
            }
        }

        // MAUI 8/10 automatically handles permission callbacks via ActivityResultCallbackRegistry
        // No manual forwarding needed - keeping override for Android lifecycle compatibility only
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            // Permission results are automatically handled by MAUI's Permissions class
        }
    }
}
