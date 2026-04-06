using BarcodeScanning;
using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// QR Scanner Page — quét mã QR để phát POI ngay lập tức (bypass geofence).
/// Hỗ trợ format: "audiotour://poi/{id}" hoặc POI ID trực tiếp.
/// </summary>
public class QrScannerPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly NarrationEngine _narrationEngine;
    private readonly CameraView _cameraView;
    private readonly Label _statusLabel;
    private bool _isProcessing;
    private CancellationTokenSource? _cameraStartCts;

    public QrScannerPage(DatabaseService dbService, NarrationEngine narrationEngine)
    {
        _dbService = dbService;
        _narrationEngine = narrationEngine;

        var loc = LocalizationService.Current;
        Title = loc["QrBtn"];
        BackgroundColor = Colors.Black;

        _statusLabel = new Label
        {
            Text = loc["ScanQrPrompt"],
            FontSize = 16,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(20)
        };

        _cameraView = new CameraView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            CameraFacing = CameraFacing.Back,
            CaptureQuality = CaptureQuality.Medium,
            ForceInverted = false,
        };
        _cameraView.OnDetectionFinished += OnQrDetected;

        var closeBtn = new Button
        {
            Text = loc["CloseBtn"],
            BackgroundColor = Microsoft.Maui.Graphics.Color.FromArgb("#B71C1C"),
            TextColor = Colors.White,
            CornerRadius = 10,
            Margin = new Thickness(16, 8),
            HorizontalOptions = LayoutOptions.Center,
            WidthRequest = 150
        };
        closeBtn.Clicked += async (s, e) => await Navigation.PopAsync();

        Content = new Grid
        {
            RowDefinitions =
            {
                new RowDefinition(GridLength.Star),
                new RowDefinition(GridLength.Auto),
                new RowDefinition(GridLength.Auto)
            },
            Children = { _cameraView, _statusLabel, closeBtn }
        };
        Grid.SetRow(_cameraView, 0);
        Grid.SetRow(_statusLabel, 1);
        Grid.SetRow(closeBtn, 2);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _cameraStartCts?.Cancel();
        _cameraStartCts = new CancellationTokenSource();
        var ct = _cameraStartCts.Token;

        _statusLabel.Text = LocalizationService.Current["ScanQrPrompt"];
        _statusLabel.TextColor = Colors.White;
        _isProcessing = false;

        // Cần đảm bảo camera tắt trước khi check
        _cameraView.CameraEnabled = false;

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            _statusLabel.Text = "❌ Cần cấp quyền Camera để quét QR!";
            _statusLabel.TextColor = Colors.OrangeRed;
            return;
        }

        try
        {
            await StartCameraWithRetryAsync(ct);
        }
        catch (TaskCanceledException)
        {
            // Page disappeared while camera was starting.
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "❌ Không thể khởi tạo camera, vui lòng thử lại.";
            _statusLabel.TextColor = Colors.OrangeRed;
            Console.WriteLine($"[QR] Camera init error: {ex.Message}");
        }
    }

    private async Task StartCameraWithRetryAsync(CancellationToken ct)
    {
        const int maxAttempts = 3;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            // Android camera init đôi khi fail ở lần bật đầu sau permission dialog.
            await Task.Delay(220, ct);
            _cameraView.CameraEnabled = false;
            await Task.Delay(120, ct);
            _cameraView.CameraEnabled = true;
            await Task.Delay(260, ct);

            if (_cameraView.CameraEnabled)
            {
                _statusLabel.Text = LocalizationService.Current["ScanQrPrompt"];
                _statusLabel.TextColor = Colors.White;
                return;
            }

            Console.WriteLine($"[QR] Camera init retry {attempt}/{maxAttempts} failed");
        }

        _statusLabel.Text = "❌ Không mở được camera. Hãy kiểm tra quyền camera trong Settings.";
        _statusLabel.TextColor = Colors.OrangeRed;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _cameraStartCts?.Cancel();

        // Tắt camera khi rời trang để tiết kiệm pin
        _cameraView.CameraEnabled = false;
        _isProcessing = false;
    }

    private async void OnQrDetected(object? sender, OnDetectionFinishedEventArg e)
    {
        if (_isProcessing || e.BarcodeResults == null || e.BarcodeResults.Count == 0)
            return;

        _isProcessing = true;

        var firstResult = e.BarcodeResults.First();
        var rawValue = firstResult.DisplayValue;

        if (string.IsNullOrEmpty(rawValue))
        {
            _isProcessing = false;
            return;
        }

        Console.WriteLine($"[QR] 📱 Quét được: {rawValue}");

        // Parse QR data: "audiotour://poi/{id}" hoặc chỉ ID
        var poiId = ParsePoiId(rawValue);

        // Tìm POI trong SQLite
        var poi = await _dbService.GetPoiByIdAsync(poiId);

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (poi != null)
            {
                _statusLabel.Text = $"✅ Tìm thấy: {poi.Title}";
                _statusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#4CAF50");

                // Bypass geofence → phát narration ngay lập tức
                await _narrationEngine.OnPOITriggeredAsync(poi, "QR");

                // Quay lại trang trước sau 2 giây
                await Task.Delay(2000);
                await Navigation.PopAsync();
            }
            else
            {
                _statusLabel.Text = $"❌ Không tìm thấy POI: {poiId}";
                _statusLabel.TextColor = Microsoft.Maui.Graphics.Color.FromArgb("#F44336");

                // Cho phép quét lại sau 2 giây
                await Task.Delay(2000);
                _isProcessing = false;
            }
        });
    }

    /// <summary>
    /// Parse POI ID từ QR data.
    /// Formats: "audiotour://poi/{id}" hoặc "https://audiotour.app/poi/{id}" hoặc raw ID.
    /// </summary>
    private static string ParsePoiId(string rawValue)
    {
        // audiotour://poi/abc-123
        if (rawValue.StartsWith("audiotour://poi/", StringComparison.OrdinalIgnoreCase))
            return rawValue.Substring("audiotour://poi/".Length).Trim();

        // https://audiotour.app/poi/abc-123
        if (rawValue.Contains("/poi/", StringComparison.OrdinalIgnoreCase))
        {
            var idx = rawValue.IndexOf("/poi/", StringComparison.OrdinalIgnoreCase);
            return rawValue.Substring(idx + 5).Trim().TrimEnd('/');
        }

        // Raw ID
        return rawValue.Trim();
    }
}
