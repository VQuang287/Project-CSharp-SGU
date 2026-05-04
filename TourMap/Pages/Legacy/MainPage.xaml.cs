using TourMap.Services;

namespace TourMap
{
    public partial class MainPage : ContentPage
    {
        private const string OnboardingCompletedKey = "onboarding_completed";
        private readonly SyncService _syncService;
        private readonly TourRuntimeService _tourRuntimeService;
        private readonly LocalizationService _loc;
        private readonly Label _syncStatusLabel;
        private readonly Button _langSwitchBtn;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Button _openMapBtn;
        private readonly Button _openPoiListBtn;
        private readonly Button _openQrBtn;
        private readonly Button _settingsBtn;

        public MainPage(SyncService syncService, TourRuntimeService tourRuntimeService)
        {
            _syncService = syncService;
            _tourRuntimeService = tourRuntimeService;
            _loc = LocalizationService.Current;

            // Nút chuyển ngôn ngữ ở góc trên
            _langSwitchBtn = new Button
            {
                Text = _loc["LangSwitchBtn"],
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 10, 10, 0),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#1565C0")
            };

            _titleLabel = new Label
            {
                Text = _loc["AppTitle"],
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            _subtitleLabel = new Label
            {
                Text = _loc["AppSubtitle"],
                FontSize = 14,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            _syncStatusLabel = new Label
            {
                Text = "",
                FontSize = 12,
                TextColor = Colors.Gray,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _openMapBtn = new Button
            {
                Text = _loc["MapBtn"],
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#1565C0"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            _openMapBtn.Clicked += OpenMap_Clicked;

            _openPoiListBtn = new Button
            {
                Text = _loc["PoiListBtn"],
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#2E7D32"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            _openPoiListBtn.Clicked += OpenPoiList_Clicked;

            _openQrBtn = new Button
            {
                Text = _loc["QrBtn"],
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#E65100"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            _openQrBtn.Clicked += OpenQrScanner_Clicked;

            _settingsBtn = new Button
            {
                Text = _loc["SettingsTitle"] ?? "⚙️ Cài đặt",
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#455A64"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            _settingsBtn.Clicked += async (s, e) => await Shell.Current.GoToAsync(nameof(Pages.SettingsPage));

            // Xử lý đổi ngôn ngữ
            _langSwitchBtn.Clicked += (s, e) =>
            {
                _loc.CurrentLanguage = _loc.CurrentLanguage == "vi" ? "en" : "vi";
                RefreshLocalizedText();
            };

            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Padding = new Thickness(20),
                Children = { _langSwitchBtn, _titleLabel, _subtitleLabel, _syncStatusLabel, _openMapBtn, _openPoiListBtn, _openQrBtn, _settingsBtn }
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            try
            {
                // Subscribe to language changes
                _loc.LanguageChanged += OnLanguageChanged;
                RefreshLocalizedText();
                
                await RunFirstLaunchFlowAsync();
                await RequestLocationPermissionAsync();
                await TrySyncAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Error in OnAppearing: {ex.Message}");
                // Show user-friendly error if needed
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            // Unsubscribe from language changes to prevent memory leaks
            _loc.LanguageChanged -= OnLanguageChanged;
        }

        private async Task RunFirstLaunchFlowAsync()
        {
            if (Preferences.Default.Get<bool>(OnboardingCompletedKey, false))
                return;

            var loc = LocalizationService.Current;

            await DisplayAlertAsync(
                "TourMap",
                "Audio Tour Guide sẽ tự động phát thuyết minh khi bạn đi vào khu vực POI. Bạn có thể xem bản đồ, danh sách POI và quét QR để mở nhanh nội dung.",
                "Tiếp tục");

            var languageChoice = await DisplayActionSheetAsync(
                "Chọn ngôn ngữ giao diện lần đầu",
                null,
                null,
                "Tiếng Việt",
                "English");

            loc.CurrentLanguage = languageChoice == "English" ? "en" : "vi";
            RefreshLocalizedText();

            await DisplayAlertAsync(
                "OK",
                loc.CurrentLanguage == "vi"
                    ? "Đã lưu ngôn ngữ. App sẽ tiếp tục xin quyền vị trí và đồng bộ dữ liệu demo."
                    : "Language saved. The app will now request location permission and sync demo data.",
                loc.CurrentLanguage == "vi" ? "Bắt đầu" : "Start");

            Preferences.Default.Set(OnboardingCompletedKey, true);
        }

        private void OnLanguageChanged()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    RefreshLocalizedText();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MainPage] Error in OnLanguageChanged: {ex.Message}");
                }
            });
        }

        private void RefreshLocalizedText()
        {
            var loc = LocalizationService.Current;
            _langSwitchBtn.Text = loc["LangSwitchBtn"] ?? "🌐 Ngôn ngữ";
            _titleLabel.Text = loc["AppTitle"] ?? "🎧 Audio Guide Tour";
            _subtitleLabel.Text = loc["AppSubtitle"] ?? "Chi tiết ứng dụng";
            _openMapBtn.Text = loc["MapBtn"] ?? "🗺️ Mở Bản Đồ";
            _openPoiListBtn.Text = loc["PoiListBtn"] ?? "📍 Địa điểm";
            _openQrBtn.Text = loc["QrBtn"] ?? "📷 Quét QR Code";
            _settingsBtn.Text = loc["SettingsTitle"] ?? "⚙️ Cài đặt";
        }

        private async Task RequestLocationPermissionAsync()
        {
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }
        }

        private async Task TrySyncAsync()
        {
            try
            {
                var loc = LocalizationService.Current;
                _syncStatusLabel.Text = loc["Syncing"];

                // Auth removed - proceed directly to sync

                var success = false;
                foreach (var serverUrl in GetSyncBaseUrls())
                {
                    success = await _syncService.SyncPoisFromServerAsync(serverUrl);
                    if (success)
                        break;
                }
                _syncStatusLabel.Text = success
                    ? loc["SyncSuccess"]
                    : loc["OfflineMode"];

                await _tourRuntimeService.InitializeAsync();
                await _tourRuntimeService.RefreshPoisAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Error Syncing: {ex}");
                _syncStatusLabel.Text = LocalizationService.Current["OfflineMode"];

                // Dù sync lỗi vẫn chạy geofence bằng local SQLite/mock-data.
                await _tourRuntimeService.InitializeAsync();
            }
        }

        private static IReadOnlyList<string> GetSyncBaseUrls()
        {
            // Port must match AdminWeb launchSettings.json (5042)
            // 10.0.2.2 = host machine IP from Android emulator
            return new[]
            {
            "http://192.168.1.7:5042",
            "http://10.0.2.2:5042",
            "http://localhost:5042"
        };
    }

    private async void OpenMap_Clicked(object? sender, EventArgs e)
    {
        try
        {
            await Shell.Current.GoToAsync(nameof(Pages.MapPage));
        }
        catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Error navigating to MapPage: {ex.Message}");
            }
        }

        private async void OpenPoiList_Clicked(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Pages.PoiListPage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Error navigating to PoiListPage: {ex.Message}");
            }
        }

        private async void OpenQrScanner_Clicked(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync(nameof(Pages.QrScannerPage));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainPage] Error navigating to QrScannerPage: {ex.Message}");
            }
        }
    }
}

