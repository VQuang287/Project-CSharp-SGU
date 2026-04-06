using TourMap.Models;
using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// Trang chi tiết POI — hiển thị ảnh, mô tả, audio player, link bản đồ.
/// Nhận poiId qua query parameter.
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

    private readonly Image _poiImage;
    private readonly Label _titleLabel;
    private readonly Label _descriptionLabel;
    private readonly Button _playAudioBtn;
    private readonly Button _stopBtn;
    private readonly Label _statusLabel;

    private Poi? _poi;

    public PoiDetailPage(DatabaseService dbService, NarrationEngine narrationEngine)
    {
        _dbService = dbService;
        _narrationEngine = narrationEngine;

        var loc = LocalizationService.Current;

        // === UI Components ===
        _poiImage = new Image
        {
            HeightRequest = 200,
            Aspect = Aspect.AspectFill,
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#E0E0E0")
        };

        _titleLabel = new Label
        {
            FontSize = 24,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(16, 12, 16, 4)
        };

        _descriptionLabel = new Label
        {
            FontSize = 16,
            Margin = new Thickness(16, 0, 16, 12),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#555555")
        };

        _statusLabel = new Label
        {
            FontSize = 13,
            Margin = new Thickness(16, 0),
            TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#888888"),
            HorizontalOptions = LayoutOptions.Center
        };

        _playAudioBtn = new Button
        {
            Text = loc["PlayBtn"],
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#1B5E20"),
            TextColor = Colors.White,
            CornerRadius = 10,
            Margin = new Thickness(16, 8),
            FontAttributes = FontAttributes.Bold
        };
        _playAudioBtn.Clicked += OnPlayAudioClicked;

        _stopBtn = new Button
        {
            Text = loc["StopBtn"],
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#B71C1C"),
            TextColor = Colors.White,
            CornerRadius = 10,
            Margin = new Thickness(16, 4),
            FontAttributes = FontAttributes.Bold
        };
        _stopBtn.Clicked += OnStopClicked;


        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    _poiImage,
                    _titleLabel,
                    _descriptionLabel,
                    _statusLabel,
                    _playAudioBtn,
                    _stopBtn
                }
            }
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = LocalizationService.Current["PoiDetailTitle"];
        _narrationEngine.StateChanged += OnNarrationStateChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _narrationEngine.StateChanged -= OnNarrationStateChanged;
    }

    private async Task LoadPoiAsync()
    {
        if (string.IsNullOrEmpty(_poiId)) return;

        _poi = await _dbService.GetPoiByIdAsync(_poiId);
        if (_poi == null) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            _titleLabel.Text = _poi.Title;
            var lang = LocalizationService.Current.CurrentLanguage;
            _descriptionLabel.Text = lang switch {
                "en" => _poi.DescriptionEn,
                "zh" => _poi.DescriptionZh,
                "ko" => _poi.DescriptionKo,
                "ja" => _poi.DescriptionJa,
                "fr" => _poi.DescriptionFr,
                _ => _poi.Description
            } ?? _poi.Description;

            if (!string.IsNullOrEmpty(_poi.ImageUrl))
            {
                _poiImage.Source = _poi.ImageUrl;
            }

            if (lang == "vi" && !string.IsNullOrEmpty(_poi.AudioLocalPath) && File.Exists(_poi.AudioLocalPath))
                _statusLabel.Text = "🎵 Có file audio sẵn (MP3)";
            else
                _statusLabel.Text = "🗣️ Sẽ dùng hệ thống TTS để đọc";
        });
    }

    private async void OnPlayAudioClicked(object? sender, EventArgs e)
    {
        if (_poi == null) return;
        await _narrationEngine.OnPOITriggeredAsync(_poi, "Manual");
    }

    private void OnStopClicked(object? sender, EventArgs e)
    {
        _narrationEngine.Stop();
    }

    private void OnNarrationStateChanged(NarrationState state, Poi? poi)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case NarrationState.Playing:
                    _statusLabel.Text = $"🔊 Đang phát: {poi?.Title}";
                    _statusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#1B5E20");
                    break;
                case NarrationState.Cooldown:
                    _statusLabel.Text = $"✅ Đã phát xong";
                    _statusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#888888");
                    break;
                case NarrationState.Idle:
                    _statusLabel.Text = _poi != null && !string.IsNullOrEmpty(_poi.AudioLocalPath)
                        ? "🎵 Có file audio sẵn (MP3)" : "🗣️ Sẽ dùng TTS để đọc mô tả";
                    _statusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#888888");
                    break;
            }
        });
    }
}
