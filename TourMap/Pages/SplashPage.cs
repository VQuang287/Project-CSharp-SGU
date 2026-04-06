using TourMap.Services;

namespace TourMap.Pages;

public class SplashPage : ContentPage
{
    public SplashPage()
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
        // Nếu đã chọn ngôn ngữ rồi → tự chuyển sang AppShell
        var savedLang = Preferences.Default.Get("selected_language", string.Empty);
        if (!string.IsNullOrEmpty(savedLang))
        {
            LocalizationService.Current.CurrentLanguage = savedLang;
            await Task.Delay(600);
            Application.Current!.Windows[0].Page = new AppShell();
        }
    }

    private async Task SelectLanguageAndProceed(string lang)
    {
        Preferences.Default.Set("selected_language", lang);
        LocalizationService.Current.CurrentLanguage = lang;
        await Task.Delay(200);
        Application.Current!.Windows[0].Page = new AppShell();
    }
}
