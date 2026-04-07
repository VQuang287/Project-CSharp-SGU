using TourMap.Services;

namespace TourMap
{
    public partial class MainPage : ContentPage
    {
        private const string OnboardingCompletedKey = "onboarding_completed";
        private readonly SyncService _syncService;
        private readonly AuthService _authService;
        private readonly TourRuntimeService _tourRuntimeService;
        private readonly Label _syncStatusLabel;
        private readonly Button _langSwitchBtn;
        private readonly Label _titleLabel;
        private readonly Label _subtitleLabel;
        private readonly Button _openMapBtn;
        private readonly Button _openPoiListBtn;
        private readonly Button _openQrBtn;

        public MainPage(SyncService syncService, AuthService authService, TourRuntimeService tourRuntimeService)
        {
            _syncService = syncService;
            _authService = authService;
            _tourRuntimeService = tourRuntimeService;
            var loc = LocalizationService.Current;

            // Nút chuyển ngôn ngữ ở góc trên
            _langSwitchBtn = new Button
            {
                Text = loc["LangSwitchBtn"],
                HorizontalOptions = LayoutOptions.End,
                Margin = new Thickness(0, 10, 10, 0),
                BackgroundColor = Colors.Transparent,
                TextColor = Color.FromArgb("#1565C0")
            };

            _titleLabel = new Label
            {
                Text = loc["AppTitle"],
                FontSize = 26,
                FontAttributes = FontAttributes.Bold,
                HorizontalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 10, 0, 10)
            };

            _subtitleLabel = new Label
            {
                Text = loc["AppSubtitle"],
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
                Text = loc["MapBtn"],
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
                Text = loc["PoiListBtn"],
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
                Text = loc["QrBtn"],
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#E65100"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            _openQrBtn.Clicked += OpenQrScanner_Clicked;

            var settingsBtn = new Button
            {
                Text = "⚙️ Settings",
                HorizontalOptions = LayoutOptions.Center,
                WidthRequest = 250,
                Margin = new Thickness(0, 8),
                BackgroundColor = Color.FromArgb("#455A64"),
                TextColor = Colors.White,
                CornerRadius = 10
            };
            settingsBtn.Clicked += async (s, e) => await Shell.Current.GoToAsync(nameof(Pages.SettingsPage));

            // Xử lý đổi ngôn ngữ
            _langSwitchBtn.Clicked += (s, e) =>
            {
                loc.CurrentLanguage = loc.CurrentLanguage == "vi" ? "en" : "vi";
                RefreshLocalizedText();
            };

            Content = new VerticalStackLayout
            {
                Spacing = 5,
                Padding = new Thickness(20),
                Children = { _langSwitchBtn, _titleLabel, _subtitleLabel, _syncStatusLabel, _openMapBtn, _openPoiListBtn, _openQrBtn, settingsBtn }
            };
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await RunFirstLaunchFlowAsync();
            await RequestLocationPermissionAsync();
            await TrySyncAsync();
        }

        private async Task RunFirstLaunchFlowAsync()
        {
            if (Preferences.Default.Get<bool>(OnboardingCompletedKey, false))
                return;

            var loc = LocalizationService.Current;

            await DisplayAlert(
                "TourMap",
                "Audio Tour Guide sẽ tự động phát thuyết minh khi bạn đi vào khu vực POI. Bạn có thể xem bản đồ, danh sách POI và quét QR để mở nhanh nội dung.",
                "Tiếp tục");

            var languageChoice = await DisplayActionSheet(
                "Chọn ngôn ngữ giao diện lần đầu",
                null,
                null,
                "Tiếng Việt",
                "English");

            loc.CurrentLanguage = languageChoice == "English" ? "en" : "vi";
            RefreshLocalizedText();

            await DisplayAlert(
                "OK",
                loc.CurrentLanguage == "vi"
                    ? "Đã lưu ngôn ngữ. App sẽ tiếp tục xin quyền vị trí và đồng bộ dữ liệu demo."
                    : "Language saved. The app will now request location permission and sync demo data.",
                loc.CurrentLanguage == "vi" ? "Bắt đầu" : "Start");

            Preferences.Default.Set(OnboardingCompletedKey, true);
        }

        private void RefreshLocalizedText()
        {
            var loc = LocalizationService.Current;
            _langSwitchBtn.Text = loc["LangSwitchBtn"];
            _titleLabel.Text = loc["AppTitle"];
            _subtitleLabel.Text = loc["AppSubtitle"];
            _openMapBtn.Text = loc["MapBtn"];
            _openPoiListBtn.Text = loc["PoiListBtn"];
            _openQrBtn.Text = loc["QrBtn"];
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

                // Authenticate (Anonymous Device Login) first
                await _authService.InitializeAsync();

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
            // Ưu tiên emulator loopback, sau đó fallback localhost
            // để hỗ trợ nhiều cách chạy backend trong môi trường dev.
            return new[]
            {
                "http://10.0.2.2:5042",
                "http://localhost:5042"
            };
        }

        private async void OpenMap_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(Pages.MapPage));
        }

        private async void OpenPoiList_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(Pages.PoiListPage));
        }

        private async void OpenQrScanner_Clicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync(nameof(Pages.QrScannerPage));
        }
    }
}

