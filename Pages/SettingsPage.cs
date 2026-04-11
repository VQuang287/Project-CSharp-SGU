using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// Settings Screen — Figma-faithful dark/light theme (currently light).
/// Grouped lists for Language, Cache, and App Info.
/// </summary>
public class SettingsPage : ContentPage
{
    private readonly Label _cacheSizeLabel;
    private readonly VerticalStackLayout _langGroup;
    private readonly Label _headerTitle;
    private readonly Label _langHeader;
    private readonly Label _prefHeader;
    private readonly Label _autoPlayTitle;
    private readonly Label _bgPlayTitle;
    private readonly Label _dataHeader;
    private readonly Label _cacheDataTitle;
    private readonly Label _cleanupDownloadsTitle;
    private readonly Label _clearCacheActionLabel;
    private readonly Label _infoHeader;
    private readonly Label _versionTitle;

    public SettingsPage()
    {
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F6F5F1"); // Figma background color

        var loc = LocalizationService.Current;

        _headerTitle = new Label
        {
            Text = loc["SettingsTitle"] ?? "Cài đặt",
            FontFamily = "InterBold", FontSize = 17,
            TextColor = Color.FromArgb("#18181B"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 16, 0, 16)
        };

        // ═══════════════════════════════════════════
        // SECTION: LANGUAGE
        // ═══════════════════════════════════════════
        _langHeader = CreateSectionHeader(loc["LangSection"] ?? "Ngôn ngữ / Language");
        _langGroup = new VerticalStackLayout { Spacing = 0 };
        RebuildLanguageGroup();

        var langSection = new VerticalStackLayout { Children = { _langHeader, CreateGroupCard(_langGroup) } };

        // ═══════════════════════════════════════════
        // SECTION: PREFERENCES (MOCK FOR FIGMA)
        // ═══════════════════════════════════════════
        _prefHeader = CreateSectionHeader(loc["AudioPrefsSection"] ?? "Tùy chỉnh âm thanh");
        var autoPlaySwitch = new Switch { IsToggled = true, OnColor = Color.FromArgb("#0D7A5F") };
        var autoPlayRow = CreateSettingRow("🔊", loc["AutoPlayNear"] ?? "Tự động phát khi đến gần", autoPlaySwitch, false, out _autoPlayTitle);
        
        var bgPlaySwitch = new Switch { IsToggled = true, OnColor = Color.FromArgb("#0D7A5F") };
        var bgPlayRow = CreateSettingRow("📱", loc["BackgroundPlay"] ?? "Phát khi khóa màn hình", bgPlaySwitch, true, out _bgPlayTitle);

        var prefSection = new VerticalStackLayout { Children = { _prefHeader, CreateGroupCard(new VerticalStackLayout { Children = { autoPlayRow, CreateDivider(), bgPlayRow } }) } };

        // ═══════════════════════════════════════════
        // SECTION: DATA & CACHE
        // ═══════════════════════════════════════════
        _dataHeader = CreateSectionHeader(loc["DataSection"] ?? "Quản lý dữ liệu");
        
        _cacheSizeLabel = new Label { FontFamily = "InterMedium", FontSize = 13, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        var cacheRow = CreateSettingRow("💾", loc["CacheDataLabel"] ?? "Dữ liệu đệm (Cache)", _cacheSizeLabel, false, out _cacheDataTitle);
        
        _clearCacheActionLabel = new Label { Text = loc["ClearCache"] ?? "Xóa dữ liệu bộ đệm", FontFamily = "InterMedium", FontSize = 15, TextColor = Color.FromArgb("#EF4444"), VerticalOptions = LayoutOptions.Center };
        var clearCacheRow = CreateSettingRow("🗑️", loc["CleanupDownloads"] ?? "Dọn dẹp tải xuống", _clearCacheActionLabel, true, out _cleanupDownloadsTitle);
        
        // Wrap clearCacheRow in gesturerecognizer
        var clearCacheTap = new TapGestureRecognizer();
        clearCacheTap.Command = new Command(async () => await OnClearCacheClicked(loc));
        clearCacheRow.GestureRecognizers.Add(clearCacheTap);

        var dataSection = new VerticalStackLayout { Children = { _dataHeader, CreateGroupCard(new VerticalStackLayout { Children = { cacheRow, CreateDivider(), clearCacheRow } }) } };

        // ═══════════════════════════════════════════
        // SECTION: APP INFO
        // ═══════════════════════════════════════════
        _infoHeader = CreateSectionHeader(loc["InfoSection"] ?? "Thông tin");
        var versionText = new Label { Text = AppInfo.Current.VersionString, FontFamily = "InterRegular", FontSize = 14, TextColor = Color.FromArgb("#9CA3AF"), VerticalOptions = LayoutOptions.Center };
        var versionRow = CreateSettingRow("ℹ️", loc["VersionLabel"] ?? "Phiên bản", versionText, true, out _versionTitle);

        var infoSection = new VerticalStackLayout { Children = { _infoHeader, CreateGroupCard(new VerticalStackLayout { Children = { versionRow } }) } };
        
        var bottomPadding = new BoxView { HeightRequest = 40, Color = Colors.Transparent };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 24,
                Padding = new Thickness(16, 0, 16, 16),
                Children = { _headerTitle, langSection, prefSection, dataSection, infoSection, bottomPadding }
            }
        };

        ApplyLocalization();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        
        // Subscribe to events when page appears
        var loc = LocalizationService.Current;
        loc.LanguageChanged += OnLanguageChanged;
        
        UpdateCacheSize();
        RebuildLanguageGroup();
        ApplyLocalization();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        
        // Unsubscribe from events to prevent memory leaks and crashes
        var loc = LocalizationService.Current;
        loc.LanguageChanged -= OnLanguageChanged;
    }

    private void RebuildLanguageGroup()
    {
        _langGroup.Children.Clear();
        var loc = LocalizationService.Current;
        var langs = LocalizationService.SupportedLanguages;
        
        for (int i = 0; i < langs.Count; i++)
        {
            var lang = langs[i];
            var isSelected = loc.CurrentLanguage == lang.Code;
            
            var checkMark = new Label { Text = isSelected ? "✓" : "", FontFamily = "InterBold", FontSize = 16, TextColor = Color.FromArgb("#0D7A5F"), VerticalOptions = LayoutOptions.Center };
            var row = CreateSettingRow(lang.Flag, lang.DisplayName, checkMark, i == langs.Count - 1, out _);
            
            var tap = new TapGestureRecognizer();
            tap.Command = new Command(() => 
            {
                loc.CurrentLanguage = lang.Code;
                // LanguageChanged event will trigger UI refresh automatically
            });
            row.GestureRecognizers.Add(tap);
            
            _langGroup.Children.Add(row);
            if (i < langs.Count - 1)
            {
                _langGroup.Children.Add(CreateDivider());
            }
        }
    }

    private async Task OnClearCacheClicked(LocalizationService loc)
    {
        bool confirm = await DisplayAlertAsync(loc["ClearCacheConfirmTitle"], loc["ClearCacheConfirmMsg"], loc["ClearCacheOk"], loc["ClearCacheCancel"]);
        if (confirm)
        {
            var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
            if (Directory.Exists(audioFolder)) Directory.Delete(audioFolder, true);
            _cacheSizeLabel.Text = "0 KB";
            await DisplayAlertAsync(loc["InfoSection"], loc["ClearCacheSuccess"], loc["OK"]);
        }
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Content == null) return;

                ApplyLocalization();
                RebuildLanguageGroup();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SettingsPage] ❌ Error updating UI: {ex.Message}");
            }
        });
    }

    private void ApplyLocalization()
    {
        var loc = LocalizationService.Current;
        _headerTitle.Text = loc["SettingsTitle"] ?? "Cài đặt";
        _langHeader.Text = (loc["LangSection"] ?? "Ngôn ngữ / Language").ToUpper();
        _prefHeader.Text = (loc["AudioPrefsSection"] ?? "Tùy chỉnh âm thanh").ToUpper();
        _autoPlayTitle.Text = loc["AutoPlayNear"] ?? "Tự động phát khi đến gần";
        _bgPlayTitle.Text = loc["BackgroundPlay"] ?? "Phát khi khóa màn hình";
        _dataHeader.Text = (loc["DataSection"] ?? "Quản lý dữ liệu").ToUpper();
        _cacheDataTitle.Text = loc["CacheDataLabel"] ?? "Dữ liệu đệm (Cache)";
        _cleanupDownloadsTitle.Text = loc["CleanupDownloads"] ?? "Dọn dẹp tải xuống";
        _clearCacheActionLabel.Text = loc["ClearCache"] ?? "Xóa dữ liệu bộ đệm";
        _infoHeader.Text = (loc["InfoSection"] ?? "Thông tin").ToUpper();
        _versionTitle.Text = loc["VersionLabel"] ?? "Phiên bản";
    }

    private void UpdateCacheSize()
    {
        var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
        if (Directory.Exists(audioFolder))
        {
            var files = Directory.GetFiles(audioFolder);
            long totalBytes = files.Sum(f => new FileInfo(f).Length);
            _cacheSizeLabel.Text = $"{files.Length} files — {totalBytes / 1024} KB";
        }
        else
        {
            _cacheSizeLabel.Text = "0 KB";
        }
    }

    // ═══════════════════════════════════════════════════════════
    // UI Helpers
    // ═══════════════════════════════════════════════════════════

    private Label CreateSectionHeader(string text)
    {
        return new Label
        {
            Text = text.ToUpper(),
            FontFamily = "InterBold", FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            Margin = new Thickness(16, 0, 16, 8)
        };
    }

    private Border CreateGroupCard(View content)
    {
        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Content = content
        };
    }

    private Grid CreateSettingRow(string icon, string title, View trailingView, bool isLast, out Label titleLabel)
    {
        var iconBg = new Border
        {
            WidthRequest = 32, HeightRequest = 32,
            BackgroundColor = Color.FromArgb("#F6F5F1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = icon, FontSize = 16, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center }
        };

        titleLabel = new Label
        {
            Text = title,
            FontFamily = "InterMedium", FontSize = 14,
            TextColor = Color.FromArgb("#18181B"),
            VerticalOptions = LayoutOptions.Center
        };

        var grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            Padding = new Thickness(16, 12),
        };
        grid.Add(iconBg, 0, 0);
        grid.Add(titleLabel, 1, 0);
        grid.Add(trailingView, 2, 0);

        return grid;
    }

    private BoxView CreateDivider()
    {
        return new BoxView { HeightRequest = 1, Color = Color.FromArgb("#F3F4F6"), Margin = new Thickness(60, 0, 0, 0) };
    }
}
