using BarcodeScanning;
using TourMap.Services;

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
    private readonly Border _torchBtnBg;
    private readonly Label _torchIcon;
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
    private bool _torchOn;
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

        // Header
        var backBtn = new Border
        {
            WidthRequest = 40, HeightRequest = 40,
            BackgroundColor = Color.FromRgba(1.0, 1.0, 1.0, 0.15),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Content = new Label { Text = "←", FontSize = 18, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center },
        };
        backBtn.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(() => _ = Navigation.PopAsync()) });

        _qrHeaderLabel = new Label { Text = _loc["QrHeader"] ?? "Quét mã QR", FontFamily = "InterBold", FontSize = 16, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center };
        _qrInstructionsLabel = new Label { Text = _loc["QrInstructions"] ?? "Đưa mã vào khung để quét", FontFamily = "InterRegular", FontSize = 11, TextColor = Color.FromRgba(1.0, 1.0, 1.0, 0.6), HorizontalTextAlignment = TextAlignment.Center };

        var headerTitle = new VerticalStackLayout
        {
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            Children = { _qrHeaderLabel, _qrInstructionsLabel }
        };

        _torchIcon = new Label { Text = "⚡", FontSize = 16, TextColor = Colors.White, HorizontalTextAlignment = TextAlignment.Center, VerticalTextAlignment = TextAlignment.Center };
        _torchBtnBg = new Border
        {
            WidthRequest = 40, HeightRequest = 40,
            BackgroundColor = Color.FromRgba(1.0, 1.0, 1.0, 0.15),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Content = _torchIcon,
        };
        _torchBtnBg.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(ToggleTorch) });

        var headerGrid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Margin = new Thickness(16, 44, 16, 0),
            VerticalOptions = LayoutOptions.Start,
        };
        headerGrid.Add(backBtn, 0, 0);
        headerGrid.Add(headerTitle, 1, 0);
        headerGrid.Add(_torchBtnBg, 2, 0);

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

    private void ToggleTorch()
    {
        _torchOn = !_torchOn;
        if (_cameraView.CameraEnabled)
        {
            _cameraView.TorchOn = _torchOn;
        }
        _torchBtnBg.BackgroundColor = _torchOn ? Color.FromArgb("#F5A623") : Color.FromRgba(1.0, 1.0, 1.0, 0.15);
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
        _torchOn = false;
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

            var rawValue = e.BarcodeResults.First().DisplayValue;
            if (string.IsNullOrEmpty(rawValue))
            {
                ResetScanner();
                return;
            }

            var poiId = ParsePoiId(rawValue);
            if (string.IsNullOrEmpty(poiId))
            {
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
                    // Show success card
                    _scannedPoi = poi;
                    _successPoiTitle.Text = poi.Title;
                    _successPoiDesc.Text = poi.Description;
                    _successCard.IsVisible = true;
                }
                else
                {
                    _statusLabel.Text = _loc["CameraInvalidQr"] ?? "Mã QR không hợp lệ. Đang quét lại...";
                    await Task.Delay(2000);
                    ResetScanner();
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
            
            await _narrationEngine.OnPOITriggeredAsync(_scannedPoi, "QR");
            await Shell.Current.GoToAsync($"{nameof(PoiDetailPage)}?poiId={_scannedPoi.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[QrScannerPage] Error playing POI: {ex.Message}");
        }
    }

    // SUG-06: Validate POI ID format (must be valid GUID or safe alphanumeric)
    private static string? ParsePoiId(string rawValue)
    {
        string poiId;
        
        if (rawValue.StartsWith("audiotour://poi/", StringComparison.OrdinalIgnoreCase))
            poiId = rawValue.Substring("audiotour://poi/".Length).Trim();
        else if (rawValue.Contains("/poi/", StringComparison.OrdinalIgnoreCase))
        {
            var idx = rawValue.IndexOf("/poi/", StringComparison.OrdinalIgnoreCase);
            poiId = rawValue.Substring(idx + 5).Trim().TrimEnd('/');
        }
        else
            poiId = rawValue.Trim();

        // Validate POI ID format
        if (Guid.TryParse(poiId, out _))
            return poiId;

        // Also accept short alphanumeric IDs (max 64 chars, no special chars)
        if (poiId.Length > 0 && poiId.Length <= 64 && 
            poiId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            return poiId;

        Console.WriteLine($"[QR] ⚠️ Invalid POI ID format rejected: {poiId}");
        return null;
    }
}
