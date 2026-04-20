using System;
using Android.App;
using Android.Content;
using Microsoft.Maui.Storage;

namespace TourMap
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { "com.android.vending.INSTALL_REFERRER" })]
    public class InstallReferrerReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            try
            {
                var raw = intent?.GetStringExtra("referrer");
                if (string.IsNullOrEmpty(raw))
                    return;

                // Example raw: "deep_link=audiotour%3A%2F%2Fpoi%2F{poiId}&utm_source=qr"
                string? deepLink = null;
                var parts = raw.Split('&');
                foreach (var p in parts)
                {
                    var kv = p.Split('=', 2);
                    if (kv.Length == 2 && kv[0] == "deep_link")
                    {
                        deepLink = System.Net.WebUtility.UrlDecode(kv[1]);
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(deepLink))
                {
                    Preferences.Default.Set("pending_deeplink", deepLink);
                    Android.Util.Log.Info("InstallReferrerReceiver", $"Saved pending_deeplink: {deepLink}");
                }
            }
            catch (Exception ex)
            {
                Android.Util.Log.Error("InstallReferrerReceiver", ex.ToString());
            }
        }
    }
}
