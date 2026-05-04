using BarcodeScanning;
using TourMap.Services;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace TourMap.Pages;

/// <summary>
/// QR Scanner Page — Figma-faithful dark theme.
/// Scan frame, Torch button, success overlay.
/// Hỗ trợ format: "audiotour://poi/{id}" hoặc POI ID trực tiếp.
/// </summary>
public class QrScannerPage : ContentPage
{
    private readonly DatabaseService _dbService;
    private readonly NarrationEngine _narrationEngine;
    private readonly LocalizationService _loc;

    // UI elements
    private readonly CameraView _cameraView;
    private readonly Label _statusLabel;
    private readonly BoxView _scanBox;
    private readonly Border _successCard;
    private readonly Label _successPoiTitle;
    private readonly Label _successPoiDesc;
    private readonly Label _qrHeaderLabel;
    private readonly Label _qrInstructionsLabel;
    private readonly Label _successTitleLabel;
    private readonly Button _qrPlayButton;
    private readonly Button _qrScanAgainButton;
    
    // State
    private bool _isProcessing;
    private CancellationTokenSource? _cameraStartCts;
    private Models.Poi? _scannedPoi;

    public QrScannerPage() : this(
        ServiceHelper.GetService<DatabaseService>(),
        ServiceHelper.GetService<NarrationEngine>())
    {
    }

    public QrScannerPage(DatabaseService dbService, NarrationEngine narrationEngine)
    {
        _dbService = dbService;
        _narrationEngine = narrationEngine;
        _loc = LocalizationService.Current;

        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#181818");

        // ═══════════════════════════════════════════
        // CAMERA VIEW
        // ═══════════════════════════════════════════
        _cameraView = new CameraView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            CameraFacing = CameraFacing.Back,
            CaptureQuality = CaptureQuality.Medium,
            ForceInverted = false,
            IsVisible = true, // Ensure camera view is visible
        };
        _cameraView.OnDetectionFinished += OnQrDetected;

        // ═══════════════════════════════════════════
        // OVERLAYS
        // ═══════════════════════════════════════════
        // Darkened background layer with clear center hole
        var overlayGrid = new Grid
        {
            InputTransparent = true,
            Padding = 0,
            Margin = 0,
        };

        // Header (chỉ còn title, không có back button và torch)
        _qrHeaderLabel = new Label { Text = _loc["QrHeader"] ?? "Quét mã QR", FontFamily = "InterBold", FontSize = 18, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        _qrInstructionsLabel = new Label { Text = _loc["QrInstructions"] ?? "Đưa mã vào khung để quét", FontFamily = "InterRegular", FontSize = 12, TextColor = Color.FromRgba(1.0, 1.0, 1.0, 0.7), HorizontalTextAlignment = TextAlignment.Center };

        var headerTitle = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { _qrHeaderLabel, _qrInstructionsLabel }
        };

        var headerGrid = new Grid
        {
            Margin = new Thickness(16, 44, 16, 0),
            VerticalOptions = LayoutOptions.Start,
            HorizontalOptions = LayoutOptions.Center
        };
        headerGrid.Add(headerTitle);

        // Scan Frame
        _scanBox = new BoxView
        {
            WidthRequest = 220, HeightRequest = 220,
            Color = Colors.Transparent,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };

        var cornersLayout = new Grid
        {
            WidthRequest = 220, HeightRequest = 220,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
        };
        var cornerSize = 40;
        var thick = 4;
        var radius = 16;
        
        // Simulating corners using borders
        cornersLayout.Add(new Border { BackgroundColor = Colors.Transparent, Stroke = Colors.White, StrokeThickness = thick, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(radius, 0, 0, 0) }, WidthRequest = cornerSize, HeightRequest = cornerSize, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.Start });
        cornersLayout.Add(new Border { BackgroundColor = Colors.Transparent, Stroke = Colors.White, StrokeThickness = thick, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(0, radius, 0, 0) }, WidthRequest = cornerSize, HeightRequest = cornerSize, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.Start });
        cornersLayout.Add(new Border { BackgroundColor = Colors.Transparent, Stroke = Colors.White, StrokeThickness = thick, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(0, 0, 0, radius) }, WidthRequest = cornerSize, HeightRequest = cornerSize, HorizontalOptions = LayoutOptions.Start, VerticalOptions = LayoutOptions.End });
        cornersLayout.Add(new Border { BackgroundColor = Colors.Transparent, Stroke = Colors.White, StrokeThickness = thick, StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = new CornerRadius(0, 0, radius, 0) }, WidthRequest = cornerSize, HeightRequest = cornerSize, HorizontalOptions = LayoutOptions.End, VerticalOptions = LayoutOptions.End });

        _statusLabel = new Label
        {
            Text = "Tự động nhận diện mã QR",
            FontFamily = "InterRegular", FontSize = 12,
            TextColor = Color.FromRgba(255, 255, 255, 0.6),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 300, 0, 0), // Push below scan frame
        };

        // Success Card
        var checkIcon = new Border
        {
            WidthRequest = 56, HeightRequest = 56,
            BackgroundColor = Color.FromArgb("#E0F5F0"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 28 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = "✓", FontSize = 28, TextColor = Color.FromArgb("#0D7A5F"), HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
            HorizontalOptions = LayoutOptions.Center,
        };

        _successPoiTitle = new Label { FontFamily = "InterBold", FontSize = 15, TextColor = Color.FromArgb("#18181B"), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };
        _successPoiDesc = new Label { FontFamily = "InterRegular", FontSize = 12, TextColor = Color.FromArgb("#6B7280"), MaxLines = 1, LineBreakMode = LineBreakMode.TailTruncation };

        var poiInfoBox = new Border
        {
            BackgroundColor = Color.FromArgb("#F6F5F1"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(12),
            Margin = new Thickness(0, 16, 0, 16),
            Content = new HorizontalStackLayout
            {
                Spacing = 12,
                Children =
                {
                    new Label { Text = "📍", FontSize = 24, VerticalOptions = LayoutOptions.Center },
                    new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _successPoiTitle, _successPoiDesc } }
                }
            }
        };

        var triggerBtn = _qrPlayButton = new Button
        {
            Text = _loc["QrPlayButton"] ?? "Mở và phát thuyết minh",
            FontFamily = "InterMedium", FontSize = 14,
            BackgroundColor = Color.FromArgb("#0D7A5F"),
            TextColor = Colors.White,
            CornerRadius = 12,
            HeightRequest = 48,
        };
        triggerBtn.Clicked += OnPlaySuccessPoi;

        var scanAgainBtn = _qrScanAgainButton = new Button
        {
            Text = _loc["QrScanAgain"] ?? "Quét mã khác",
            FontFamily = "InterRegular", FontSize = 13,
            BackgroundColor = Colors.Transparent,
            TextColor = Color.FromArgb("#9CA3AF"),
            HeightRequest = 44,
        };
        scanAgainBtn.Clicked += (s, e) => ResetScanner();

        _successCard = new Border
        {
            IsVisible = false,
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(24),
            Margin = new Thickness(24, 0),
            VerticalOptions = LayoutOptions.Center,
            Shadow = new Shadow { Brush = Colors.Black, Offset = new Point(0, 10), Radius = 20, Opacity = 0.2f },
            Content = new VerticalStackLayout
            {
                Spacing = 0,
                Children =
                {
                    checkIcon,
                    (_successTitleLabel = new Label { Text = _loc["QrSuccessTitle"] ?? "Mã QR đã nhận diện!", FontFamily = "InterBold", FontSize = 16, TextColor = Color.FromArgb("#18181B"), HorizontalOptions = LayoutOptions.Center, Margin = new Thickness(0, 12, 0, 4) }),
                    poiInfoBox,
                    triggerBtn,
                    scanAgainBtn
                }
            }
        };

        overlayGrid.Children.Add(headerGrid);
        overlayGrid.Children.Add(cornersLayout);
        overlayGrid.Children.Add(_statusLabel);

        Content = new Grid
        {
            Children = { _cameraView, overlayGrid, _successCard }
        };
    }

    protected override async void OnAppearing()
    {
        Console.WriteLine("[QR] OnAppearing START");
        base.OnAppearing();
        // Subscribe to language changes
        _loc.LanguageChanged += OnLanguageChanged;
        OnLanguageChanged();
        
        
        try
        {
            // Reset scanner state
            ResetScanner();
            
            // Ensure camera view is properly reset
            if (_cameraView != null)
            {
                _cameraView.OnDetectionFinished -= OnQrDetected;  // Prevent duplicate handlers
                _cameraView.OnDetectionFinished += OnQrDetected;
                _cameraView.CameraEnabled = false;
                _cameraView.PauseScanning = false;
            }
            
            // Create new cancellation token for this session
            _cameraStartCts?.Cancel();
            _cameraStartCts?.Dispose();
            _cameraStartCts = new CancellationTokenSource();
            var ct = _cameraStartCts.Token;
            
            Console.WriteLine($"[QR] CameraView state - IsVisible: {_cameraView?.IsVisible}, CameraEnabled: {_cameraView?.CameraEnabled}");
            
            _statusLabel.Text = _loc["CameraCheckingPermission"] ?? "Đang kiểm tra quyền camera...";
            
            // Check permissions with detailed logging
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            Console.WriteLine($"[QR] Initial camera permission status: {status}");
            
            if (status != PermissionStatus.Granted)
            {
                _statusLabel.Text = _loc["CameraRequesting"] ?? "Đang yêu cầu quyền camera...";
                status = await Permissions.RequestAsync<Permissions.Camera>();
                Console.WriteLine($"[QR] Camera permission after request: {status}");
            }

            if (status != PermissionStatus.Granted)
            {
                _statusLabel.Text = _loc["CameraPermissionDenied"] ?? "Cần quyền camera để tiếp tục. Vui lòng cấp quyền trong Settings.";
                ShowCameraError(_loc["CameraPermissionDeniedShort"] ?? "Quyền camera bị từ chối");
                return;
            }

            // Start camera with enhanced retry
            try
            {
                await StartCameraWithRetryAsync(ct);
            }
            catch (TaskCanceledException) 
            { 
                Console.WriteLine("[QR] Camera start cancelled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QR] Camera start failed: {ex.Message}");
                _statusLabel.Text = _loc["CameraStartError"] ?? "Lỗi khởi động camera. Vui lòng thử lại.";
                ShowCameraError($"Lỗi: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QR] OnAppearing ERROR: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[QR] Stack trace: {ex.StackTrace}");
            _statusLabel.Text = _loc["CameraStartFailed"] ?? "Lỗi khởi động camera";
            ShowCameraError("Lỗi không xác định");
        }
        
        Console.WriteLine("[QR] OnAppearing END");
    }

    private async Task StartCameraWithRetryAsync(CancellationToken ct)
    {
        const int maxAttempts = 5;
        
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                ct.ThrowIfCancellationRequested();
                
                Console.WriteLine($"[QR] Camera start attempt {attempt}/{maxAttempts}");
                _statusLabel.Text = string.Format(_loc["CameraStartingFormat"] ?? "Đang khởi động camera... ({0}/{1})", attempt, maxAttempts);
                
                // Progressive delay - camera needs more time on cold start
                var delayMs = 500 * attempt;
                Console.WriteLine($"[QR] Waiting {delayMs}ms before attempt {attempt}");
                await Task.Delay(delayMs, ct);
                
                // Full reset sequence
                if (_cameraView == null)
                {
                    Console.WriteLine("[QR] ERROR: CameraView is null");
                    throw new InvalidOperationException("CameraView not initialized");
                }
                
                // Disable first
                _cameraView.CameraEnabled = false;
                await Task.Delay(300, ct);
                
                // Ensure scanning is not paused
                _cameraView.PauseScanning = false;
                await Task.Delay(100, ct);
                
                // Enable camera
                Console.WriteLine("[QR] Enabling camera...");
                _cameraView.CameraEnabled = true;
                
                // Wait for initialization
                await Task.Delay(1000, ct);
                
                // Verify camera is actually enabled
                if (_cameraView.CameraEnabled)
                {
                    Console.WriteLine("[QR] Camera started successfully");
                    _statusLabel.Text = _loc["QrScanHint"] ?? "Tự động nhận diện mã QR";
                    _statusLabel.TextColor = Colors.White;
                    return;
                }
                
                Console.WriteLine($"[QR] Camera not enabled after attempt {attempt}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[QR] Camera start cancelled by token");
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QR] Attempt {attempt} failed: {ex.GetType().Name} - {ex.Message}");
                
                if (attempt == maxAttempts)
                {
                    Console.WriteLine("[QR] All camera start attempts failed");
                    _statusLabel.Text = _loc["CameraStartFailed"] ?? "Không thể khởi động camera";
                    _statusLabel.TextColor = Color.FromArgb("#FF6B6B");
                    ShowCameraError(string.Format(_loc["CameraErrorAfterRetry"] ?? "Lỗi sau {0} lần thử: {1}", maxAttempts, ex.Message));
                    return;
                }
            }
        }
    }
    
    private void ShowCameraError(string message)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            // Add retry button to status area
            var retryBtn = new Button
            {
                Text = _loc["CameraRetry"] ?? "Thử lại",
                BackgroundColor = Color.FromArgb("#0D7A5F"),
                TextColor = Colors.White,
                CornerRadius = 8,
                HeightRequest = 40,
                Margin = new Thickness(20, 8)
            };
            retryBtn.Clicked += async (s, e) =>
            {
                retryBtn.IsEnabled = false;
                retryBtn.Text = _loc["CameraRetrying"] ?? "Đang thử...";
                await RetryCameraAsync();
            };
            
            // Find parent container and add retry button
            if (_statusLabel.Parent is VerticalStackLayout vsl)
            {
                // Remove existing retry button if any
                var retryText = _loc["CameraRetry"] ?? "Thử lại";
                var existingBtn = vsl.Children.FirstOrDefault(c => c is Button b && b.Text?.Contains(retryText) == true);
                if (existingBtn != null)
                    vsl.Children.Remove(existingBtn);
                
                vsl.Children.Add(retryBtn);
            }
        });
    }
    
    private async Task RetryCameraAsync()
    {
        Console.WriteLine("[QR] Manual retry requested");
        
        // Clean up first
        if (_cameraView != null)
        {
            _cameraView.CameraEnabled = false;
            _cameraView.PauseScanning = true;
        }
        
        await Task.Delay(500);
        
        // Reinitialize
        _cameraStartCts?.Cancel();
        _cameraStartCts?.Dispose();
        _cameraStartCts = new CancellationTokenSource();
        
        await StartCameraWithRetryAsync(_cameraStartCts.Token);
    }

    protected override void OnDisappearing()
    {
        Console.WriteLine("[QR] OnDisappearing - cleaning up camera resources");
        base.OnDisappearing();
        
        // Unsubscribe from language changes
        _loc.LanguageChanged -= OnLanguageChanged;
        
        // Cancel any pending camera start operations
        _cameraStartCts?.Cancel();
        _cameraStartCts?.Dispose();
        _cameraStartCts = null;
        
        // Full camera cleanup sequence
        if (_cameraView != null)
        {
            try
            {
                _cameraView.OnDetectionFinished -= OnQrDetected;
                _cameraView.PauseScanning = true;
                _cameraView.CameraEnabled = false;
                Console.WriteLine("[QR] Camera cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QR] Error during camera cleanup: {ex.Message}");
            }
        }
        
        _isProcessing = false;
        Console.WriteLine("[QR] OnDisappearing complete");
    }

    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                if (Content == null) return;
                
                _qrHeaderLabel.Text = _loc["QrHeader"] ?? "Quét mã QR";
                _qrInstructionsLabel.Text = _loc["QrInstructions"] ?? "Đưa mã vào khung để quét";
                
                // Update success overlay labels
                if (_successTitleLabel != null)
                    _successTitleLabel.Text = _loc["QrSuccessTitle"] ?? "Mã QR đã nhận diện!";
                
                if (_qrPlayButton != null)
                    _qrPlayButton.Text = _loc["QrPlayButton"] ?? "Mở và phát thuyết minh";
                
                if (_qrScanAgainButton != null)
                    _qrScanAgainButton.Text = _loc["QrScanAgain"] ?? "Quét mã khác";
                
                // Update status label only if not currently processing
                if (!_isProcessing && _statusLabel != null)
                {
                    _statusLabel.Text = _loc["QrScanHint"] ?? "Tự động nhận diện mã QR";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[QrScannerPage] Error in OnLanguageChanged: {ex.Message}");
            }
        });
    }

    private void ResetScanner()
    {
        _isProcessing = false;
        _successCard.IsVisible = false;
        _cameraView.PauseScanning = false;
        _statusLabel.Text = _loc["QrScanHint"] ?? "Tự động nhận diện mã QR";
        _statusLabel.TextColor = Colors.White;
    }

    private async void OnQrDetected(object? sender, OnDetectionFinishedEventArg e)
    {
        try
        {
            if (_isProcessing || e.BarcodeResults == null || e.BarcodeResults.Count == 0)
                return;

            _isProcessing = true;
            _cameraView.PauseScanning = true;

            var rawValue = ExtractBarcodePayload(e.BarcodeResults.First());
            if (string.IsNullOrEmpty(rawValue))
            {
                Console.WriteLine("[QR] Empty payload from barcode result.");
                ResetScanner();
                return;
            }

            Console.WriteLine($"[QR] Payload detected: {TruncateForLog(rawValue)}");

            var poiId = ParsePoiId(rawValue);
            if (string.IsNullOrEmpty(poiId))
            {
                Console.WriteLine($"[QR] Unable to parse POI ID from payload: {TruncateForLog(rawValue)}");
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    _statusLabel.Text = _loc["CameraInvalidQr"] ?? "Mã QR không hợp lệ. Đang quét lại...";
                    await Task.Delay(2000);
                    ResetScanner();
                });
                return;
            }
            var poi = await _dbService.GetPoiByIdAsync(poiId);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                if (poi != null)
                {
                    _scannedPoi = poi;
                    _successPoiTitle.Text = poi.Title;
                    _successPoiDesc.Text = poi.Description;
                    _successCard.IsVisible = true;
                    // KHÔNG tự động mở POI - chờ user nhấn nút "Mở và phát thuyết minh"
                }
                else
                {
                    // POI không tồn tại trong DB - vẫn navigate để hiển thị thông báo lỗi
                    _statusLabel.Text = _loc["QrScanHint"] ?? "Đang mở chi tiết POI...";
                    await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={poiId}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QrScannerPage] Error processing QR code: {ex.Message}");
            ResetScanner();
        }
    }

    private async void OnPlaySuccessPoi(object? sender, EventArgs e)
    {
        try
        {
            if (_scannedPoi == null) return;

            await OpenPoiDetailAsync(_scannedPoi, _scannedPoi.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QrScannerPage] Error playing POI: {ex.Message}");
        }
    }

    private async Task OpenPoiDetailAsync(Models.Poi poi, string poiId)
    {
        await _narrationEngine.OnPOITriggeredAsync(poi, "QR");
        await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={poiId}");
    }

    // SUG-06: Validate POI ID format (must be valid GUID or safe alphanumeric)
    private static string? ParsePoiId(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return null;

        var normalized = NormalizeScannedValue(rawValue);
        var decoded = normalized.Contains('%') ? WebUtility.UrlDecode(normalized) : normalized;

        var poiId = TryExtractPoiIdFromCandidate(normalized)
            ?? TryExtractPoiIdFromCandidate(decoded);

        if (string.IsNullOrWhiteSpace(poiId))
            return null;

        if (IsSafePoiId(poiId))
            return poiId;

        Console.WriteLine($"[QR] ⚠️ Invalid POI ID format rejected: {poiId}");
        return null;
    }

    private static string? TryExtractPoiIdFromCandidate(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate))
            return null;

        var poiId = DeepLinkHelper.ExtractPoiId(candidate);
        if (!string.IsNullOrWhiteSpace(poiId))
            return poiId.Trim();

        // Android referrer style payload: deep_link=audiotour%3A%2F%2Fpoi%2F{id}
        if (candidate.Contains("deep_link=", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var part in candidate.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = part.Split('=', 2);
                if (kv.Length != 2)
                    continue;

                if (!kv[0].Equals("deep_link", StringComparison.OrdinalIgnoreCase))
                    continue;

                var deepLinkValue = WebUtility.UrlDecode(kv[1]);
                poiId = DeepLinkHelper.ExtractPoiId(deepLinkValue);
                if (!string.IsNullOrWhiteSpace(poiId))
                    return poiId.Trim();
            }
        }

        // Android intent URL form: intent://poi/{id}#Intent;...
        if (candidate.StartsWith("intent://poi/", StringComparison.OrdinalIgnoreCase))
        {
            var remainder = candidate.Substring("intent://poi/".Length);
            var hashIndex = remainder.IndexOf('#');
            poiId = (hashIndex >= 0 ? remainder[..hashIndex] : remainder).Trim();
            if (!string.IsNullOrWhiteSpace(poiId))
                return poiId;
        }

        // Some scanners strip scheme from URL, e.g. 127.0.0.1:5042/Launch/{id}
        if (!candidate.Contains("://", StringComparison.Ordinal)
            && candidate.Contains('/')
            && candidate.Contains(':'))
        {
            var reconstructed = $"http://{candidate}";
            poiId = DeepLinkHelper.ExtractPoiId(reconstructed);
            if (!string.IsNullOrWhiteSpace(poiId))
                return poiId.Trim();
        }

        // Fallback: pick a GUID anywhere in the scanned text.
        var guidMatch = Regex.Match(candidate, @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b");
        if (guidMatch.Success)
            return guidMatch.Value;

        // Final fallback: last path-like segment.
        var tail = candidate
            .Split('?', '#', '&')[0]
            .TrimEnd('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .LastOrDefault();

        return string.IsNullOrWhiteSpace(tail) ? null : tail.Trim();
    }

    private static string NormalizeScannedValue(string value)
    {
        var normalized = value.Trim();

        // Remove common invisible characters emitted by some scanner engines.
        normalized = normalized
            .Replace("\uFEFF", string.Empty, StringComparison.Ordinal)
            .Replace("\u200B", string.Empty, StringComparison.Ordinal)
            .Replace("\u200C", string.Empty, StringComparison.Ordinal)
            .Replace("\u200D", string.Empty, StringComparison.Ordinal);

        return normalized;
    }

    private static bool IsSafePoiId(string poiId)
    {
        if (Guid.TryParse(poiId, out _))
            return true;

        return poiId.Length > 0
            && poiId.Length <= 128
            && poiId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
    }

    private static string? ExtractBarcodePayload(object? barcodeResult)
    {
        if (barcodeResult == null)
            return null;

        var triedProperties = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var propertyName in new[] { "DisplayValue", "RawValue", "Value", "Text", "Data" })
        {
            triedProperties.Add(propertyName);
            var candidate = TryReadBarcodeProperty(barcodeResult, propertyName);
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        // Library versions may rename the payload property; inspect all readable properties as fallback.
        foreach (var prop in barcodeResult.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            if (triedProperties.Contains(prop.Name))
                continue;

            var value = prop.GetValue(barcodeResult);
            var candidate = value switch
            {
                null => null,
                string s => s,
                byte[] bytes when bytes.Length > 0 => Encoding.UTF8.GetString(bytes),
                _ => null
            };

            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;
        }

        var fallback = barcodeResult.ToString();
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    private static string? TryReadBarcodeProperty(object barcodeResult, string propertyName)
    {
        var prop = barcodeResult.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (prop == null)
            return null;

        var value = prop.GetValue(barcodeResult);
        return value switch
        {
            null => null,
            string s => s,
            byte[] bytes when bytes.Length > 0 => Encoding.UTF8.GetString(bytes),
            _ => value.ToString()
        };
    }

    private static string TruncateForLog(string value, int max = 160)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;

        return value[..max] + "...";
    }
}
