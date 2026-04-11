using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// POI Detail Screen — Figma-faithful implementation.
/// Hero image + gradient overlay, audio player with waveform,
/// language selector, speed controls, map link, related POIs.
/// </summary>
[QueryProperty(nameof(PoiId), "poiId")]
public class PoiDetailPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly NarrationEngine _narrationEngine;

    private string? _poiId;
    public string? PoiId
    {
        get => _poiId;
        set
        {
            _poiId = value;
            _ = LoadPoiAsync();
        }
    }

    private Poi? _poi;
    // REMOVED: Local language selection - now always uses system language
    // private string _selectedLang = "vi";
    private string _selectedSpeed = "1x";
    private bool _isPlaying = false;

    // UI refs
    private readonly Image _heroImage;
    private readonly Label _heroTitle;
    private readonly Label _heroSubtitle;
    private readonly Label _categoryBadge;
    private readonly Label _descriptionLabel;
    private readonly Label _audioTypeLabel;
    private readonly Label _statusLabel;
    private readonly Label _timeCurrentLabel;
    private readonly Label _timeRemainingLabel;
    private readonly ProgressBar _progressBar;
    private readonly Button _playPauseBtn;
    // REMOVED: Language selector - now uses system language
    // private readonly HorizontalStackLayout _langRow;
    private readonly HorizontalStackLayout _speedRow;
    private readonly BoxView _waveformPlaceholder;

    // REMOVED: Per-POI language selection - now always uses system language from LocalizationService
    // private static readonly string[] LangCodes = { "vi", "en", "ko", "zh" };
    // private static readonly string[] LangLabels = { "VI", "EN", "KO", "ZH" };
    private static readonly string[] SpeedOptions = { "0.75x", "1x", "1.25x", "1.5x" };

    public PoiDetailPage() : this(
        ServiceHelper.GetService<DatabaseService>(),
        ServiceHelper.GetService<NarrationEngine>())
    {
    }

    public PoiDetailPage(DatabaseService dbService, NarrationEngine narrationEngine)
    {
        _dbService = dbService;
        _narrationEngine = narrationEngine;
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F6F5F1");

        // REMOVED: Local language state - now always uses system language
        // _selectedLang = loc.CurrentLanguage;
        // Event subscription moved to OnAppearing to prevent memory leaks

        // ═══════════════════════════════════════════
        // HERO IMAGE SECTION
        // ═══════════════════════════════════════════
        _heroImage = new Image
        {
            HeightRequest = 208,
            Aspect = Aspect.AspectFill,
            BackgroundColor = Color.FromArgb("#E4E4E0"),
        };

        // Gradient overlay
        var gradientOverlay = new BoxView
        {
            HeightRequest = 208,
            Background = new LinearGradientBrush
            {
                StartPoint = new Point(0.5, 0),
                EndPoint = new Point(0.5, 1),
                GradientStops =
                {
                    new GradientStop(Color.FromRgba(0, 0, 0, 0.2), 0.0f),
                    new GradientStop(Colors.Transparent, 0.4f),
                    new GradientStop(Color.FromRgba(0, 0, 0, 0.5), 1.0f),
                }
            }
        };

        // Back button
        var backBtn = new Border
        {
            WidthRequest = 36, HeightRequest = 36,
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.3),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = "←", FontSize = 18, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(16, 44, 0, 0),
        };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(async () => await Shell.Current.GoToAsync("..")) });

        // Like + Share buttons
        var likeBtn = CreateOverlayButton("♡");
        var shareBtn = CreateOverlayButton("↗");
        var topRightBtns = new HorizontalStackLayout
        {
            Spacing = 8,
            HorizontalOptions = LayoutOptions.End,
            VerticalOptions = LayoutOptions.Start,
            Margin = new Thickness(0, 44, 16, 0),
            Children = { likeBtn, shareBtn }
        };

        // Category badge + title overlay at bottom
        _categoryBadge = new Label
        {
            FontFamily = "InterBold", FontSize = 9,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#F5A623"),
            Padding = new Thickness(8, 3),
            VerticalOptions = LayoutOptions.End,
        };

        _heroTitle = new Label
        {
            FontFamily = "InterBold", FontSize = 18,
            TextColor = Colors.White,
            VerticalOptions = LayoutOptions.End,
        };

        _heroSubtitle = new Label
        {
            FontFamily = "InterRegular", FontSize = 11,
            TextColor = Color.FromRgba(255, 255, 255, 0.7),
            VerticalOptions = LayoutOptions.End,
        };

        var bottomInfo = new VerticalStackLayout
        {
            Spacing = 2,
            VerticalOptions = LayoutOptions.End,
            Margin = new Thickness(16, 0, 16, 12),
            Children = { _categoryBadge, _heroTitle, _heroSubtitle }
        };
        _categoryBadge.HorizontalOptions = LayoutOptions.Start;

        var heroGrid = new Grid
        {
            HeightRequest = 208,
            Children = { _heroImage, gradientOverlay, backBtn, topRightBtns, bottomInfo }
        };

        // ═══════════════════════════════════════════
        // QUICK STATS ROW
        // ═══════════════════════════════════════════
        var statsRow = new HorizontalStackLayout
        {
            Spacing = 16, Padding = new Thickness(16, 12, 16, 8),
            Children =
            {
                CreateStatItem("📍", "—"),
                CreateStatItem("🕐", "— thuyết minh"),
                CreateStatItem("🚶", "— phút đi bộ"),
            }
        };

        // ═══════════════════════════════════════════
        // DESCRIPTION
        // ═══════════════════════════════════════════
        var descHeader = new Label
        {
            Text = "Giới thiệu",
            FontFamily = "InterBold", FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
            Margin = new Thickness(16, 12, 16, 4),
        };

        _descriptionLabel = new Label
        {
            FontFamily = "InterRegular", FontSize = 13,
            TextColor = Color.FromArgb("#6B7280"),
            Margin = new Thickness(16, 0, 16, 12),
            LineHeight = 1.6,
        };

        var descSection = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Children = { descHeader, _descriptionLabel }
        };

        // ═══════════════════════════════════════════
        // AUDIO PLAYER
        // ═══════════════════════════════════════════
        var audioHeader = new Label
        {
            Text = "Thuyết minh audio",
            FontFamily = "InterBold", FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
        };

        // Language indicator (shows current system language - auto, not selectable)
        var langIndicator = new Label
        {
            Text = GetCurrentLanguageDisplay(),
            FontFamily = "InterBold", FontSize = 10,
            TextColor = Colors.White,
            BackgroundColor = Color.FromArgb("#0D7A5F"),
            Padding = new Thickness(8, 4),
            VerticalOptions = LayoutOptions.Center,
        };

        var audioHeaderRow = new Grid
        {
            ColumnDefinitions =
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto),
            },
        };
        audioHeaderRow.Add(audioHeader, 0, 0);
        audioHeaderRow.Add(langIndicator, 1, 0);

        // Audio type badge
        _audioTypeLabel = new Label
        {
            FontFamily = "InterMedium", FontSize = 10,
            Padding = new Thickness(10, 4),
        };

        // Waveform placeholder
        _waveformPlaceholder = new BoxView
        {
            HeightRequest = 32,
            BackgroundColor = Color.FromArgb("#E4E4E0"),
            CornerRadius = 4,
            Margin = new Thickness(0, 8),
        };

        // Progress bar
        _progressBar = new ProgressBar
        {
            Progress = 0,
            ProgressColor = Color.FromArgb("#0D7A5F"),
            HeightRequest = 4,
        };

        // Time labels
        _timeCurrentLabel = new Label { Text = "0:00", FontFamily = "InterRegular", FontSize = 10, TextColor = Color.FromArgb("#9CA3AF") };
        _timeRemainingLabel = new Label { Text = "-0:00", FontFamily = "InterRegular", FontSize = 10, TextColor = Color.FromArgb("#9CA3AF"), HorizontalTextAlignment = TextAlignment.End };
        var timeRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) },
        };
        timeRow.Add(_timeCurrentLabel, 0, 0);
        timeRow.Add(_timeRemainingLabel, 1, 0);

        // Play controls
        var skipBackBtn = new Label { Text = "⏮", FontSize = 20, TextColor = Color.FromArgb("#9CA3AF"), VerticalTextAlignment = TextAlignment.Center };
        _playPauseBtn = new Button
        {
            Text = "▶",
            WidthRequest = 56, HeightRequest = 56,
            BackgroundColor = Color.FromArgb("#0D7A5F"),
            TextColor = Colors.White,
            FontSize = 22,
            CornerRadius = 28,
            Padding = 0,
        };
        _playPauseBtn.Clicked += OnPlayPauseClicked;

        var skipFwdBtn = new Label { Text = "⏭", FontSize = 20, TextColor = Color.FromArgb("#9CA3AF"), VerticalTextAlignment = TextAlignment.Center };

        var controlsRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 24,
            Margin = new Thickness(0, 8),
            Children = { skipBackBtn, _playPauseBtn, skipFwdBtn }
        };

        // Speed selector
        _speedRow = new HorizontalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            Spacing = 8,
            Margin = new Thickness(0, 4, 0, 0),
        };
        foreach (var s in SpeedOptions)
        {
            var speedBtn = CreateSpeedChip(s);
            _speedRow.Children.Add(speedBtn);
        }

        // Status label
        _statusLabel = new Label
        {
            FontFamily = "InterRegular", FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 4, 0, 0),
        };

        var audioSection = new VerticalStackLayout
        {
            BackgroundColor = Colors.White,
            Padding = new Thickness(16, 12),
            Margin = new Thickness(0, 8, 0, 0),
            Spacing = 4,
            Children = { audioHeaderRow, _audioTypeLabel, _waveformPlaceholder, _progressBar, timeRow, controlsRow, _speedRow, _statusLabel }
        };

        // ═══════════════════════════════════════════
        // MAP LINK
        // ═══════════════════════════════════════════
        var mapIcon = new Border
        {
            WidthRequest = 32, HeightRequest = 32,
            BackgroundColor = Color.FromArgb("#E0F5F0"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = "📍", FontSize = 15, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
        };

        var mapInfo = new HorizontalStackLayout
        {
            Spacing = 8,
            Padding = new Thickness(16, 12),
            BackgroundColor = Colors.White,
            Children =
            {
                mapIcon,
                new Label { Text = "Tọa độ GPS", FontFamily = "InterMedium", FontSize = 13, TextColor = Color.FromArgb("#18181B"), VerticalOptions = LayoutOptions.Center },
            }
        };
        mapInfo.Margin = new Thickness(0, 8, 0, 0);
        mapInfo.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(OnShowCoordinates) });

        // ═══════════════════════════════════════════
        // FULL LAYOUT
        // ═══════════════════════════════════════════
        var scrollContent = new VerticalStackLayout
        {
            Spacing = 0,
            Children =
            {
                heroGrid,
                new VerticalStackLayout { BackgroundColor = Colors.White, Padding = new Thickness(0, 4, 0, 8), Children = { statsRow } },
                descSection,
                audioSection,
                mapInfo,
                new BoxView { HeightRequest = 24 }, // Bottom padding
            }
        };

        Content = new ScrollView { Content = scrollContent };
    }

    // ═══════════════════════════════════════════════════════════
    // Lifecycle
    // ═══════════════════════════════════════════════════════════

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _narrationEngine.StateChanged += OnNarrationStateChanged;
        LocalizationService.Current.LanguageChanged += OnLanguageChanged;
        
        // Initialize default speed
        _narrationEngine.Speed = 1.0f;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
        LocalizationService.Current.LanguageChanged -= OnLanguageChanged;
    }

    // ═══════════════════════════════════════════════════════════
    // Data Loading
    // ═══════════════════════════════════════════════════════════

    private async Task LoadPoiAsync()
    {
        if (string.IsNullOrEmpty(_poiId)) return;
        _poi = await _dbService.GetPoiByIdAsync(_poiId);
        if (_poi == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Hero
            _heroTitle.Text = _poi.Title;
            _heroSubtitle.Text = _poi.Title; // Could be English name
            _categoryBadge.Text = $"📍 ĐIỂM THAM QUAN";

            if (!string.IsNullOrEmpty(_poi.ImageUrl))
                _heroImage.Source = _poi.ImageUrl;

            // Description (localized) - ALWAYS use system language
            var lang = LocalizationService.Current.CurrentLanguage;
            _descriptionLabel.Text = GetLocalizedDescription(lang);

            // Audio type
            bool hasAudioFile = lang == "vi" && !string.IsNullOrEmpty(_poi.AudioLocalPath) && File.Exists(_poi.AudioLocalPath);
            _audioTypeLabel.Text = hasAudioFile ? "🎙️ Audio thu sẵn" : "🤖 Giọng TTS";
            _audioTypeLabel.TextColor = hasAudioFile ? Color.FromArgb("#F5A623") : Color.FromArgb("#0D7A5F");
            _audioTypeLabel.BackgroundColor = hasAudioFile ? Color.FromArgb("#FEF6E4") : Color.FromArgb("#E0F5F0");

            // Status
            _statusLabel.Text = hasAudioFile ? "🎵 MP3 sẵn sàng" : "🗣️ TTS sẵn sàng";
        });
    }

    private string GetLocalizedDescription(string lang)
    {
        if (_poi == null) return string.Empty;
        var desc = lang switch
        {
            "en" => _poi.DescriptionEn,
            "zh" => _poi.DescriptionZh,
            "ko" => _poi.DescriptionKo,
            "ja" => _poi.DescriptionJa,
            "fr" => _poi.DescriptionFr,
            _ => _poi.Description
        };
        return desc ?? _poi.Description;
    }

    // ═══════════════════════════════════════════════════════════
    // Audio Controls
    // ═══════════════════════════════════════════════════════════

    private async void OnPlayPauseClicked(object? sender, EventArgs e)
    {
        try
        {
            if (_poi == null) return;

            if (_isPlaying)
            {
                _narrationEngine.Stop();
                _isPlaying = false;
                _playPauseBtn.Text = "▶";
            }
            else
            {
                _isPlaying = true;
                _playPauseBtn.Text = "⏸";
                await _narrationEngine.OnPOITriggeredAsync(_poi, "Manual");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PoiDetailPage] Error playing/pausing audio: {ex.Message}");
            _isPlaying = false;
            _playPauseBtn.Text = "▶";
        }
    }

    private void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case NarrationState.Playing:
                    _isPlaying = true;
                    _playPauseBtn.Text = "⏸";
                    _statusLabel.Text = $"🔊 Đang phát: {poi?.Title}";
                    _statusLabel.TextColor = Color.FromArgb("#0D7A5F");
                    break;
                case NarrationState.Cooldown:
                case NarrationState.Idle:
                    _isPlaying = false;
                    _playPauseBtn.Text = "▶";
                    _statusLabel.Text = "✅ Đã phát xong";
                    _statusLabel.TextColor = Color.FromArgb("#9CA3AF");
                    break;
            }
        });
    }

    // ═══════════════════════════════════════════════════════════
    // Language & Speed Selection
    // ═══════════════════════════════════════════════════════════

    private string GetCurrentLanguageDisplay()
    {
        var langCode = LocalizationService.Current.CurrentLanguage;
        return langCode.ToUpper() switch
        {
            "VI" => "VI",
            "EN" => "EN",
            "ZH" => "ZH",
            "KO" => "KO",
            "JA" => "JA",
            "FR" => "FR",
            _ => "VI"
        };
    }

    private void OnSpeedSelected(string speed)
    {
        _selectedSpeed = speed;
        UpdateSpeedChipStyles();
        
        // Parse speed value and apply to narration engine
        if (float.TryParse(speed.Replace("x", ""), out float speedValue))
        {
            _narrationEngine.Speed = speedValue;
            Console.WriteLine($"[PoiDetailPage] ⚡ Speed changed to {speedValue}x");
        }
    }

    private async void OnShowCoordinates()
    {
        if (_poi == null) return;
        
        var coordinates = $"Lat: {_poi.Latitude:F6}\nLon: {_poi.Longitude:F6}";
        await DisplayAlertAsync("Tọa độ", coordinates, "OK");
    }

    // ═══════════════════════════════════════════════════════════
    // UI Factory Methods
    // ═══════════════════════════════════════════════════════════

    private Border CreateOverlayButton(string icon)
    {
        return new Border
        {
            WidthRequest = 36, HeightRequest = 36,
            BackgroundColor = Color.FromRgba(0, 0, 0, 0.3),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = icon, FontSize = 16, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
        };
    }

    private HorizontalStackLayout CreateStatItem(string icon, string text)
    {
        return new HorizontalStackLayout
        {
            Spacing = 4,
            Children =
            {
                new Label { Text = icon, FontSize = 13 },
                new Label { Text = text, FontFamily = "InterRegular", FontSize = 12, TextColor = Color.FromArgb("#6B7280"), VerticalOptions = LayoutOptions.Center },
            }
        };
    }

    private Border CreateSpeedChip(string speed)
    {
        var isActive = speed == _selectedSpeed;
        var chipLabel = new Label
        {
            Text = speed,
            FontFamily = "InterMedium", FontSize = 11,
            TextColor = isActive ? Colors.White : Color.FromArgb("#9CA3AF"),
            HorizontalTextAlignment = TextAlignment.Center,
            VerticalTextAlignment = TextAlignment.Center,
        };

        var chip = new Border
        {
            Padding = new Thickness(12, 4),
            BackgroundColor = isActive ? Color.FromArgb("#0D7A5F") : Color.FromArgb("#F6F5F1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 },
            Stroke = Colors.Transparent,
            Content = chipLabel,
        };

        chip.GestureRecognizers.Add(new TapGestureRecognizer
        {
            Command = new Command(() => OnSpeedSelected(speed))
        });

        return chip;
    }

    private void UpdateSpeedChipStyles()
    {
        for (int i = 0; i < _speedRow.Children.Count; i++)
        {
            if (_speedRow.Children[i] is Border chip && chip.Content is Label label)
            {
                var isActive = SpeedOptions[i] == _selectedSpeed;
                chip.BackgroundColor = isActive ? Color.FromArgb("#0D7A5F") : Color.FromArgb("#F6F5F1");
                label.TextColor = isActive ? Colors.White : Color.FromArgb("#9CA3AF");
            }
        }
    }
    
    private void OnLanguageChanged()
    {
        // Reload POI data with new system language
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            try
            {
                // Check if page is still valid
                if (Content == null) return;
                
                if (_poi != null)
                {
                    // Reload POI to update description in new language
                    await LoadPoiAsync();
                }
                
                // If currently playing, restart with new language
                if (_isPlaying && _poi != null)
                {
                    _narrationEngine.Stop();
                    await Task.Delay(100); // Small delay
                    await _narrationEngine.PlayPoiAsync(_poi, LocalizationService.Current.CurrentLanguage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PoiDetailPage] ❌ Error handling language change: {ex.Message}");
            }
        });
    }
}
