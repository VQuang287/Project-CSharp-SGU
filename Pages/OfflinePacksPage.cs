using TourMap.Services;

namespace TourMap.Pages;

/// <summary>
/// Offline Packs — Figma-faithful dark/light theme (currently light).
/// Storage usage bar, list of tour packs with download/delete buttons.
/// </summary>
public class OfflinePacksPage : ContentPage
{
    private readonly LocalizationService _loc;
    private readonly DatabaseService _db;
    private readonly Label _headerTitle;
    private readonly Label _storageUsageLabel;
    private readonly ProgressBar _storageBar;
    private readonly Label _storageDesc;
    private readonly Label _syncStatusLabel;
    private readonly Button _downloadBtn;
    private readonly Label _packSizeLabel;
    private readonly Label _packsHeader;
    private readonly Label _storageHeader;
    private readonly Switch _autoDownloadSwitch;
    private readonly Label _lastSyncLabel;
    private readonly Label _autoDownloadLabel;
    private readonly Label _networkHeader;
    private readonly Label _q4Title;
    private readonly Label _q4Desc;
    private Label? _q1Title;
    private Label? _q1Desc;
    
    // Trạng thái
    private bool _isDownloaded = false;
    private bool _isDownloading = false;
    private CancellationTokenSource? _downloadCts;

    public OfflinePacksPage() : this(
        LocalizationService.Current,
        ServiceHelper.GetService<DatabaseService>())
    { }

    public OfflinePacksPage(LocalizationService loc, DatabaseService db)
    {
        _loc = loc;
        _db = db;
        _loc.LanguageChanged += OnLanguageChanged;
        
        Shell.SetNavBarIsVisible(this, false);
        BackgroundColor = Color.FromArgb("#F6F5F1");

        _headerTitle = new Label
        {
            Text = _loc["OfflineTitle"] ?? "Tour Offline",
            FontFamily = "InterBold", FontSize = 17,
            TextColor = Color.FromArgb("#18181B"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 16, 0, 16)
        };

        // ═══════════════════════════════════════════
        // STORAGE BAR SECTION
        // ═══════════════════════════════════════════
        _storageHeader = CreateSectionHeader(_loc["StorageHeader"] ?? "Dung lượng thiết bị");
        
        _storageUsageLabel = new Label
        {
            Text = _loc["StorageUsage"] ?? "TourMap đang dùng: 0 MB",
            FontFamily = "InterMedium", FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
            Margin = new Thickness(0, 0, 0, 8)
        };

        _storageBar = new ProgressBar
        {
            Progress = 0.05f, // Giả lập hệ điều hành dùng một ít
            ProgressColor = Color.FromArgb("#0D7A5F"),
            HeightRequest = 8,
        };

        _storageDesc = new Label
        {
            Text = _loc["StorageAvailable"] ?? "Dung lượng trống còn nhiều (khoảng 32 GB)",
            FontFamily = "InterRegular", FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            Margin = new Thickness(0, 8, 0, 0)
        };

        _syncStatusLabel = new Label
        {
            Text = _loc["CheckingNetwork"] ?? "Đang kiểm tra kết nối...",
            FontFamily = "InterRegular", FontSize = 11,
            TextColor = Color.FromArgb("#6B7280"),
            HorizontalOptions = LayoutOptions.Center
        };

        // Auto-download toggle
        _autoDownloadLabel = new Label
        {
            Text = _loc["AutoDownload"] ?? "Tự động tải khi có WiFi",
            FontFamily = "InterMedium", FontSize = 13,
            TextColor = Color.FromArgb("#18181B"),
            VerticalOptions = LayoutOptions.Center
        };
        
        _autoDownloadSwitch = new Switch
        {
            IsToggled = Preferences.Get("AutoDownloadEnabled", false),
            HorizontalOptions = LayoutOptions.End
        };
        _autoDownloadSwitch.Toggled += (s, e) =>
        {
            Preferences.Set("AutoDownloadEnabled", e.Value);
            Console.WriteLine($"[Offline] Auto-download: {e.Value}");
        };

        var autoDownloadRow = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            Children = { _autoDownloadLabel, _autoDownloadSwitch.WithColumn(1) }
        };

        // Last sync time
        _lastSyncLabel = new Label
        {
            Text = GetLastSyncText(),
            FontFamily = "InterRegular", FontSize = 11,
            TextColor = Color.FromArgb("#9CA3AF"),
            HorizontalOptions = LayoutOptions.Center,
            Margin = new Thickness(0, 8, 0, 0)
        };

        var storageCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(16),
            Content = new VerticalStackLayout { Children = { _storageUsageLabel, _storageBar, _storageDesc, _lastSyncLabel } }
        };

        var storageSection = new VerticalStackLayout { Children = { _storageHeader, storageCard } };

        // Network status card
        var networkCard = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(16),
            Content = new VerticalStackLayout { Children = { _syncStatusLabel, autoDownloadRow } }
        };

        _networkHeader = CreateSectionHeader(_loc["NetworkSection"] ?? "KẾT NỐI");
        var networkSection = new VerticalStackLayout { Children = { _networkHeader, networkCard } };

        // ═══════════════════════════════════════════
        // PACKS LIST SECTION
        // ═══════════════════════════════════════════
        _packsHeader = CreateSectionHeader(_loc["PacksHeader"] ?? "Các gói nội dung");

        // Gói Quận 4
        var q4Thumb = new BoxView { WidthRequest = 64, HeightRequest = 64, BackgroundColor = Color.FromArgb("#E0F5F0"), CornerRadius = 12 };
        _q4Title = new Label { Text = _loc["PackQ4Title"] ?? "Phố Ẩm Thực Vĩnh Khánh", FontFamily = "InterBold", FontSize = 14, TextColor = Color.FromArgb("#18181B") };
        _q4Desc = new Label { Text = _loc["PackQ4Desc"] ?? "Audio, Hình ảnh & Bản đồ", FontFamily = "InterRegular", FontSize = 12, TextColor = Color.FromArgb("#6B7280") };
        _packSizeLabel = new Label { Text = "45 MB", FontFamily = "InterMedium", FontSize = 11, TextColor = Color.FromArgb("#0D7A5F"), BackgroundColor = Color.FromArgb("#E0F5F0"), Padding = new Thickness(6, 2), Margin = new Thickness(0, 4, 0, 0), HorizontalOptions = LayoutOptions.Start };

        _downloadBtn = new Button
        {
            Text = _loc["Download"] ?? "Tải xuống",
            FontFamily = "InterMedium", FontSize = 12,
            BackgroundColor = Color.FromArgb("#0D7A5F"),
            TextColor = Colors.White,
            HeightRequest = 36,
            CornerRadius = 18,
            Padding = new Thickness(16, 0),
            VerticalOptions = LayoutOptions.Center
        };
        _downloadBtn.Clicked += OnDownloadQ4Clicked;

        var q4Info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _q4Title, _q4Desc, _packSizeLabel } };

        var q4Grid = new Grid
        {
            ColumnDefinitions = { new ColumnDefinition(64), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) },
            ColumnSpacing = 12,
            Children = { q4Thumb, q4Info.WithColumn(1), _downloadBtn.WithColumn(2) }
        };

        var q4Card = new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(12),
            Content = q4Grid
        };

        // Thêm một gói coming soon
        var q1Card = CreateComingSoonCard();

        var packsSection = new VerticalStackLayout { Spacing = 12, Children = { _packsHeader, q4Card, q1Card } };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 24,
                Padding = new Thickness(16, 0, 16, 16),
                Children = { _headerTitle, storageSection, networkSection, packsSection }
            }
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        OnLanguageChanged();
        RefreshStorageInfo();
        await CheckNetworkStatusAsync();
    }

    private async Task CheckNetworkStatusAsync()
    {
        try
        {
            var current = Connectivity.NetworkAccess;
            var profiles = Connectivity.ConnectionProfiles;
            
            bool isWifi = profiles.Contains(ConnectionProfile.WiFi);
            bool hasInternet = current == NetworkAccess.Internet;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (hasInternet)
                {
                    if (isWifi)
                    {
                        _syncStatusLabel.Text = _loc["WifiConnected"] ?? "✓ Đã kết nối WiFi";
                        _syncStatusLabel.TextColor = Color.FromArgb("#22C55E");
                    }
                    else
                    {
                        _syncStatusLabel.Text = _loc["MobileData"] ?? "⚠ Đang dùng dữ liệu di động";
                        _syncStatusLabel.TextColor = Color.FromArgb("#F59E0B");
                    }
                }
                else
                {
                    _syncStatusLabel.Text = _loc["NoNetwork"] ?? "✗ Không có kết nối";
                    _syncStatusLabel.TextColor = Color.FromArgb("#EF4444");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflinePacksPage] Network check error: {ex.Message}");
        }
    }

    private string GetLastSyncText()
    {
        var lastSync = Preferences.Get("LastOfflineSync", DateTime.MinValue);
        if (lastSync == DateTime.MinValue)
            return _loc["NeverSynced"] ?? "Chưa đồng bộ";
        
        var timeAgo = DateTime.UtcNow - lastSync;
        if (timeAgo.TotalMinutes < 1)
            return _loc["JustNow"] ?? "Vừa xong";
        if (timeAgo.TotalHours < 1)
            return string.Format(_loc["MinutesAgo"] ?? "{0} phút trước", (int)timeAgo.TotalMinutes);
        if (timeAgo.TotalDays < 1)
            return string.Format(_loc["HoursAgo"] ?? "{0} giờ trước", (int)timeAgo.TotalHours);
        return string.Format(_loc["DaysAgo"] ?? "{0} ngày trước", (int)timeAgo.TotalDays);
    }

    private void RefreshStorageInfo()
    {
        var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
        long totalBytes = 0;
        int fileCount = 0;
        
        if (Directory.Exists(audioFolder))
        {
            var files = Directory.GetFiles(audioFolder);
            fileCount = files.Length;
            totalBytes = files.Sum(f => new FileInfo(f).Length);
        }

        double mb = totalBytes / 1024.0 / 1024.0;
        string sizeText = mb > 1024 ? $"{mb / 1024:0.##} GB" : $"{mb:0.##} MB";
        
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lastSyncLabel.Text = GetLastSyncText();
            
            if (mb > 0)
            {
                _isDownloaded = true;
                _storageUsageLabel.Text = string.Format(_loc["StorageUsingFormat"] ?? "TourMap đang dùng: {0}", sizeText);
                _storageBar.Progress = Math.Min(0.05f + (float)(mb / 500.0), 1.0f);
                
                _downloadBtn.Text = _loc["Delete"] ?? "Xóa";
                _downloadBtn.BackgroundColor = Color.FromArgb("#FEE2E2");
                _downloadBtn.TextColor = Color.FromArgb("#EF4444");
                _packSizeLabel.Text = $"{fileCount} {_loc["FilesUnit"]}";
            }
            else
            {
                _isDownloaded = false;
                _storageUsageLabel.Text = _loc["StorageEmpty"] ?? "TourMap đang dùng: 0 MB";
                _storageBar.Progress = 0.05f;
                
                _downloadBtn.Text = _loc["Download"] ?? "Tải xuống";
                _downloadBtn.BackgroundColor = Color.FromArgb("#0D7A5F");
                _downloadBtn.TextColor = Colors.White;
                _packSizeLabel.Text = "45 MB";
            }
        });
    }

    private async void OnDownloadQ4Clicked(object? sender, EventArgs e)
    {
        if (_isDownloading) return;
        
        try
        {
            if (_isDownloaded)
            {
                // Logic xóa
                bool confirm = await DisplayAlertAsync(
                    _loc["DeleteConfirmTitle"] ?? "Xóa gói tải xuống",
                    _loc["DeleteConfirmMsg"] ?? "Việc xóa sẽ khiến bạn không thể nghe Audio khi không có mạng. Bạn chắc chắn chứ?",
                    _loc["Delete"] ?? "Xóa",
                    _loc["Cancel"] ?? "Hủy"
                );
                
                if (confirm)
                {
                    _downloadBtn.IsEnabled = false;
                    _downloadBtn.Text = _loc["Deleting"] ?? "Đang xóa...";
                    
                    var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
                    if (Directory.Exists(audioFolder)) 
                    {
                        Directory.Delete(audioFolder, true);
                        Console.WriteLine("[OfflinePacksPage] Deleted audio folder");
                    }
                    
                    // Clear last sync time
                    Preferences.Remove("LastOfflineSync");
                    
                    await DisplayAlertAsync(
                        _loc["DeleteSuccessTitle"] ?? "Thành công",
                        _loc["DeleteSuccessMsg"] ?? "Đã xóa gói Tour tham quan.",
                        _loc["OK"] ?? "OK"
                    );
                    RefreshStorageInfo();
                }
            }
            else
            {
                // Kiểm tra kết nối
                if (Connectivity.NetworkAccess != NetworkAccess.Internet)
                {
                    await DisplayAlertAsync(
                        _loc["NoNetwork"] ?? "Không có kết nối",
                        _loc["NeedNetwork"] ?? "Cần kết nối internet để tải nội dung.",
                        _loc["OK"] ?? "OK"
                    );
                    return;
                }
                
                _isDownloading = true;
                _downloadCts = new CancellationTokenSource();
                var ct = _downloadCts.Token;
                
                _downloadBtn.Text = "0%";
                _downloadBtn.BackgroundColor = Color.FromArgb("#9CA3AF");
                _downloadBtn.IsEnabled = true; // Cho phép hủy
                
                var audioFolder = Path.Combine(FileSystem.AppDataDirectory, "audio");
                Directory.CreateDirectory(audioFolder);
                
                // Simulate download with progress
                var totalFiles = 5;
                var rnd = new Random();
                
                for (int i = 1; i <= totalFiles; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    
                    int progress = (i * 100) / totalFiles;
                    _downloadBtn.Text = $"{progress}%";
                    
                    // Simulate file download
                    int fileSize = rnd.Next(5, 20);
                    File.WriteAllBytes(
                        Path.Combine(audioFolder, $"audio_{i}.mp3"),
                        new byte[fileSize * 1024 * 1024]
                    );
                    
                    await Task.Delay(300, ct);
                }
                
                // Save sync time
                Preferences.Set("LastOfflineSync", DateTime.UtcNow);
                
                _isDownloaded = true;
                _isDownloading = false;
                _downloadBtn.Text = _loc["Delete"] ?? "Xóa";
                _downloadBtn.BackgroundColor = Color.FromArgb("#FEE2E2");
                _downloadBtn.TextColor = Color.FromArgb("#EF4444");
                
                await DisplayAlertAsync(
                    _loc["DownloadSuccessTitle"] ?? "Thành công",
                    _loc["DownloadSuccessMsg"] ?? "Đã tải xong toàn bộ Audio và Bản đồ điểm đến. Bạn có thể dùng ngoại tuyến.",
                    _loc["Awesome"] ?? "Tuyệt vời"
                );
                
                RefreshStorageInfo();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[OfflinePacksPage] Download cancelled by user");
            _isDownloading = false;
            _downloadBtn.Text = _loc["Download"] ?? "Tải xuống";
            _downloadBtn.BackgroundColor = Color.FromArgb("#0D7A5F");
            _downloadBtn.TextColor = Colors.White;
            RefreshStorageInfo();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OfflinePacksPage] Error: {ex.Message}");
            _isDownloading = false;
            _downloadBtn.Text = _loc["Download"] ?? "Tải xuống";
            _downloadBtn.BackgroundColor = Color.FromArgb("#0D7A5F");
            _downloadBtn.TextColor = Colors.White;
            _downloadBtn.IsEnabled = true;
            
            await DisplayAlertAsync(
                _loc["Error"] ?? "Lỗi",
                _loc["DownloadError"] ?? $"Có lỗi xảy ra: {ex.Message}",
                _loc["OK"] ?? "OK"
            );
        }
    }

    // ═══════════════════════════════════════════════════════════
    // Language Support
    // ═══════════════════════════════════════════════════════════
    
    private void OnLanguageChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _headerTitle.Text = _loc["OfflineTitle"] ?? "Tour Offline";
            _storageHeader.Text = (_loc["StorageHeader"] ?? "Dung lượng thiết bị").ToUpper();
            _networkHeader.Text = (_loc["NetworkSection"] ?? "KẾT NỐI").ToUpper();
            _autoDownloadLabel.Text = _loc["AutoDownload"] ?? "Tự động tải khi có WiFi";
            _storageDesc.Text = _loc["StorageAvailable"] ?? "Dung lượng trống còn nhiều (khoảng 32 GB)";
            _packsHeader.Text = (_loc["PacksHeader"] ?? "Các gói nội dung").ToUpper();
            _q4Title.Text = _loc["PackQ4Title"] ?? "Phố Ẩm Thực Vĩnh Khánh";
            _q4Desc.Text = _loc["PackQ4Desc"] ?? "Audio, Hình ảnh & Bản đồ";
            if (_q1Title != null) _q1Title.Text = _loc["PackNguyenHueTitle"] ?? "Phố Đi Bộ Nguyễn Huệ";
            if (_q1Desc != null) _q1Desc.Text = _loc["ComingSoon"] ?? "Sắp ra mắt";
            
            // Update last sync with new language
            _lastSyncLabel.Text = GetLastSyncText();
            
            // Update button based on state
            if (_isDownloading)
            {
                // Keep current progress text
            }
            else if (_isDownloaded)
            {
                _downloadBtn.Text = _loc["Delete"] ?? "Xóa";
            }
            else
            {
                _downloadBtn.Text = _loc["Download"] ?? "Tải xuống";
            }
            
            RefreshStorageInfo();
            _ = CheckNetworkStatusAsync();
        });
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _loc.LanguageChanged -= OnLanguageChanged;
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

    private Border CreateComingSoonCard()
    {
        var thumb = new BoxView { WidthRequest = 64, HeightRequest = 64, BackgroundColor = Color.FromArgb("#F3F4F6"), CornerRadius = 12 };
        _q1Title = new Label { Text = _loc["PackNguyenHueTitle"] ?? "Phố Đi Bộ Nguyễn Huệ", FontFamily = "InterMedium", FontSize = 14, TextColor = Color.FromArgb("#9CA3AF") };
        _q1Desc = new Label { Text = _loc["ComingSoon"] ?? "Sắp ra mắt", FontFamily = "InterRegular", FontSize = 12, TextColor = Color.FromArgb("#D1D5DB") };
        var info = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Children = { _q1Title, _q1Desc } };

        return new Border
        {
            BackgroundColor = Colors.White,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            Stroke = Colors.Transparent,
            Padding = new Thickness(12),
            Content = new Grid
            {
                ColumnDefinitions = { new ColumnDefinition(64), new ColumnDefinition(GridLength.Star) },
                ColumnSpacing = 12,
                Children = { thumb, info.WithColumn(1) }
            }
        };
    }
}


