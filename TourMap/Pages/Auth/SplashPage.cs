using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// Splash Page - No authentication, users are anonymous by default.
/// Only handles language selection and connects to device tracking.
/// </summary>
public class SplashPage : ContentPage
{
    private readonly DeviceTrackingService? _deviceTrackingService;

    public SplashPage() : this(TryResolveDeviceTrackingService())
    {
    }

    public SplashPage(DeviceTrackingService? deviceTrackingService)
    {
        _deviceTrackingService = deviceTrackingService;
        InitializeUI();
    }

    private void InitializeUI()
    {
        BackgroundColor = Color.FromArgb("#1A237E");

        var logo = new Label
        {
            Text = "🎧",
            FontSize = 80,
            HorizontalOptions = LayoutOptions.Center,
        };

        var title = new Label
        {
            Text = "Audio Guide Tour",
            FontSize = 32,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };

        var subtitle = new Label
        {
            Text = "Phố Ẩm thực Vĩnh Khánh, Quận 4",
            FontSize = 14,
            TextColor = Color.FromArgb("#B0BEC5"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 5, 0, 40)
        };

        var langPrompt = new Label
        {
            Text = "Chọn ngôn ngữ / Choose language",
            FontSize = 16,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 0, 0, 15)
        };

        var grid = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star),
            },
            RowDefinitions =
            {
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto),
            },
            ColumnSpacing = 8,
            RowSpacing = 8,
            Margin = new Thickness(20, 0)
        };

        foreach (var (index, lang) in LocalizationService.SupportedLanguages.Select((l, i) => (i, l)))
        {
            var btn = new Button
            {
                Text = $"{lang.Flag} {lang.DisplayName}",
                BackgroundColor = Color.FromArgb("#1565C0"),
                TextColor = Colors.White,
                CornerRadius = 10,
                FontSize = 12,
                HeightRequest = 48
            };
            var capturedCode = lang.Code;
            btn.Clicked += async (s, e) => await SelectLanguageAndProceed(capturedCode);
            grid.Add(btn, index % 3, index / 3);
        }

        Content = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Spacing = 5,
            Children = { logo, title, subtitle, langPrompt, grid }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        try
        {
            var savedLang = Preferences.Default.Get("selected_language", string.Empty);
            
            if (string.IsNullOrEmpty(savedLang))
            {
                // First time - show language selection, wait for user
                Console.WriteLine("[SplashPage] No language selected, showing UI");
                return;
            }

            // Has saved language - auto proceed after delay
            Console.WriteLine($"[SplashPage] Saved language: {savedLang}");
            LocalizationService.Current.CurrentLanguage = savedLang;
            
            // Wait for UI to fully render
            await Task.Delay(500);
            
            // Navigate to main app
            NavigateToShell();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SplashPage] Error in OnAppearing: {ex}");
        }
    }

    private async Task SelectLanguageAndProceed(string lang)
    {
        Preferences.Default.Set("selected_language", lang);
        LocalizationService.Current.CurrentLanguage = lang;
        await Task.Delay(200);
        NavigateToShell();
    }

    private async void NavigateToShell()
    {
        try
        {
            // Mark onboarding completed
            Preferences.Default.Set("onboarding_completed", true);

            // Get AppShell service
            AppShell? shell = null;
            try
            {
                shell = ServiceHelper.GetService<AppShell>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SplashPage] Failed to get AppShell: {ex.Message}");
            }

            if (shell == null)
            {
                Console.WriteLine("[SplashPage] ERROR: AppShell is null, creating new instance");
                shell = new AppShell();
            }

            // Navigate on main thread with delay
            await Task.Delay(100);
            
            var window = Application.Current?.Windows.FirstOrDefault();
            if (window == null)
            {
                Console.WriteLine("[SplashPage] ERROR: No window available");
                return;
            }

            // Set the page
            window.Page = shell;
            Console.WriteLine("[SplashPage] Navigated to AppShell");

            // Fire and forget device tracking connection
            _ = Task.Run(async () =>
            {
                try
                {
                    if (_deviceTrackingService != null)
                    {
                        var hubUrls = BackendEndpoints.GetDeviceHubUrls().ToList();
                        foreach (var hubUrl in hubUrls)
                        {
                            try
                            {
                                await _deviceTrackingService.ConnectAsync(hubUrl);
                                BackendEndpoints.RememberWorkingServerFromUrl(hubUrl);
                                Console.WriteLine($"[SplashPage] Connected to {hubUrl}");
                                break;
                            }
                            catch { /* ignore */ }
                        }
                    }
                }
                catch { /* ignore */ }
            });

            // Handle deep link (fire and forget)
            _ = Task.Run(async () =>
            {
                await Task.Delay(800);
                try
                {
                    var poiId = DeepLinkHelper.ConsumePendingPoiId();
                    if (!string.IsNullOrEmpty(poiId))
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try
                            {
                                await Shell.Current.GoToAsync($"{nameof(Pages.PoiDetailPage)}?poiId={poiId}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[SplashPage] Deep link failed: {ex.Message}");
                            }
                        });
                    }
                }
                catch { /* ignore */ }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SplashPage] CRITICAL ERROR: {ex}");
        }
    }

    private static DeviceTrackingService? TryResolveDeviceTrackingService()
    {
        try
        {
            return ServiceHelper.GetService<DeviceTrackingService>();
        }
        catch
        {
            return null;
        }
    }
}
