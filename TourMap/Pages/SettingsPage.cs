using TourMap.Services;

namespace TourMap.Pages;

public class SettingsPage : ContentPage
{
    private readonly Label _cacheSizeLabel;

    public SettingsPage()
    {
        var loc = LocalizationService.Current;
        Title = loc["SettingsTitle"];
        BackgroundColor = Color.FromArgb("#FAFAFA");

        // === Language ===
        var langHeader = new Label
        {
            Text = loc["LangSection"],
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 10)
        };

        var langButtons = new VerticalStackLayout { Spacing = 6 };
        foreach (var lang in LocalizationService.SupportedLanguages)
        {
            var capturedCode = lang.Code;
            var btn = new Button
            {
                Text = $"{lang.Flag}  {lang.DisplayName}",
                HorizontalOptions = LayoutOptions.Fill,
                HeightRequest = 46,
                CornerRadius = 10,
                FontSize = 15,
                // Highlight current language
                BackgroundColor = loc.CurrentLanguage == capturedCode
                    ? Color.FromArgb("#1565C0")
                    : Color.FromArgb("#E3EAF5"),
                TextColor = loc.CurrentLanguage == capturedCode ? Colors.White : Colors.Black,
            };
            btn.Clicked += (s, e) =>
            {
                Preferences.Default.Set("selected_language", capturedCode);
                loc.CurrentLanguage = capturedCode;
                
                // Hard reset AppShell để force redraw toàn bộ giao diện app theo ngôn ngữ mới
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current!.Windows[0].Page = new AppShell();
                });
            };
            langButtons.Add(btn);
        }

        // === Cache ===
        var cacheHeader = new Label
        {
            Text = loc["CacheSection"],
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 20, 0, 5)
        };

        _cacheSizeLabel = new Label { FontSize = 14, TextColor = Colors.Gray };

        var clearCacheBtn = new Button
        {
            Text = loc["ClearCache"],
            BackgroundColor = Color.FromArgb("#B71C1C"),
            TextColor = Colors.White,
            CornerRadius = 10,
            Margin = new Thickness(0, 5, 0, 20)
        };
        clearCacheBtn.Clicked += async (s, e) =>
        {
            bool confirm = await DisplayAlertAsync(loc["ClearCacheConfirmTitle"], loc["ClearCacheConfirmMsg"], loc["ClearCacheOk"], loc["ClearCacheCancel"]);
            if (confirm)
            {
                var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
                if (Directory.Exists(audioFolder)) Directory.Delete(audioFolder, true);
                _cacheSizeLabel.Text = "0 KB";
                await DisplayAlertAsync("✅", loc["ClearCacheSuccess"], "OK");
            }
        };

        // === Info ===
        var infoHeader = new Label
        {
            Text = loc["InfoSection"],
            FontSize = 18,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 10, 0, 5)
        };

        var versionLabel = new Label
        {
            Text = $"Version {AppInfo.Current.VersionString} (Build {AppInfo.Current.BuildString})",
            FontSize = 14,
            TextColor = Colors.Gray
        };

        var projectLabel = new Label
        {
            Text = "Audio Tour Guide — Phố Ẩm thực Vĩnh Khánh, Quận 4",
            FontSize = 13,
            TextColor = Colors.Gray,
            Margin = new Thickness(0, 0, 0, 20)
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = new Thickness(20),
                Children =
                {
                    langHeader, langButtons,
                    cacheHeader, _cacheSizeLabel, clearCacheBtn,
                    infoHeader, versionLabel, projectLabel
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        UpdateCacheSize();
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
}
