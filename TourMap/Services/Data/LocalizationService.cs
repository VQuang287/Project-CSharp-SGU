using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TourMap.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private const string SelectedLanguageKey = "selected_language";
    private static LocalizationService? _instance;
    public static LocalizationService Current => _instance ??= new LocalizationService();
    
    // Event for language changes
    public event Action? LanguageChanged;

#if DEBUG
    private static readonly object FallbackLogLock = new();
    private static readonly HashSet<string> LoggedFallbacks = new();
    private static bool _dictionaryValidationLogged;
#endif
    
    // 5 languages supported
    public static readonly IReadOnlyList<(string Code, string DisplayName, string Flag)> SupportedLanguages =
        new List<(string, string, string)>
        {
            ("vi", "Tiếng Việt", "🇻🇳"),
            ("en", "English", "🇬🇧"),
            ("zh", "中文", "🇨🇳"),
            ("ko", "한국어", "🇰🇷"),
            ("ja", "日本語", "🇯🇵"),
            ("fr", "Français", "🇫🇷"),
        };

    private string _currentLanguage = "vi";
    /// <summary>Chuẩn hóa mã ngôn ngữ từ các locale khác nhau (zh-CN, zh-TW, vi-VN...) về dạng đơn giản (zh, vi...)</summary>
    public static string NormalizeLanguageCode(string langCode)
    {
        if (string.IsNullOrWhiteSpace(langCode))
            return "vi";
        
        var normalized = langCode.Trim().ToLowerInvariant();
        
        // Xử lý các Chinese locales
        if (normalized.StartsWith("zh") || normalized.StartsWith("cn") || normalized.StartsWith("ch"))
            return "zh";
        
        // Xử lý các Korean locales  
        if (normalized.StartsWith("ko") || normalized.StartsWith("kr"))
            return "ko";
        
        // Xử lý các Japanese locales
        if (normalized.StartsWith("ja") || normalized.StartsWith("jp"))
            return "ja";
        
        // Xử lý các French locales
        if (normalized.StartsWith("fr"))
            return "fr";
        
        // Xử lý các English locales
        if (normalized.StartsWith("en"))
            return "en";
        
        // Xử lý các Vietnamese locales
        if (normalized.StartsWith("vi") || normalized.StartsWith("vn"))
            return "vi";
        
        // Trả về 2 ký tự đầu nếu không khớp các trường hợp đặc biệt
        return normalized.Length >= 2 ? normalized[..2] : "vi";
    }

    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            value = NormalizeLanguageCode(value);

            // Validate input
            if (string.IsNullOrWhiteSpace(value))
            {
                Console.WriteLine("[LocalizationService] ⚠️ Invalid language code (null/empty), ignoring");
                return;
            }
            
            // Validate against supported languages
            var supportedCodes = SupportedLanguages.Select(l => l.Code).ToList();
            if (!supportedCodes.Contains(value))
            {
                Console.WriteLine($"[LocalizationService] ⚠️ Unsupported language code: {value}, ignoring");
                return;
            }
            
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                Preferences.Default.Set(SelectedLanguageKey, value);
                
                // Set culture for entire app
                var culture = new CultureInfo(value == "zh" ? "zh-CN" : value);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                CultureInfo.DefaultThreadCurrentCulture = culture;
                CultureInfo.DefaultThreadCurrentUICulture = culture;
                
                // Notify all listeners
                OnPropertyChanged("Item");
                OnPropertyChanged("Item[]");
                OnPropertyChanged(nameof(CurrentLanguage));
                LanguageChanged?.Invoke();
            }
        }
    }

    private LocalizationService()
    {
        DetectSystemLanguage();

    #if DEBUG
        ValidateDictionariesOnce();
    #endif
    }

    private void DetectSystemLanguage()
    {
        var savedLanguage = Preferences.Default.Get<string>(SelectedLanguageKey, string.Empty);
        var rawSysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var fullCulture = CultureInfo.CurrentUICulture.Name;
        
        // Normalize language code để handle các locale đặc biệt
        var sysLang = NormalizeLanguageCode(rawSysLang);
        savedLanguage = NormalizeLanguageCode(savedLanguage);
        
        Console.WriteLine($"[LocalizationService] DetectSystemLanguage: raw={rawSysLang}, normalized={sysLang}, full={fullCulture}, saved={savedLanguage}");
        
        // Nếu ngôn ngữ hệ thống nằm trong danh sách hỗ trợ VÀ khác với ngôn ngữ đã lưu
        // → user đã đổi ngôn ngữ hệ thống → cập nhật theo system language
        var supportedCodes = SupportedLanguages.Select(l => l.Code).ToHashSet();
        
        if (!string.IsNullOrWhiteSpace(savedLanguage) && supportedCodes.Contains(savedLanguage))
        {
            // Kiểm tra system language có thay đổi không
            if (supportedCodes.Contains(sysLang) && sysLang != savedLanguage)
            {
                Console.WriteLine($"[LocalizationService] System language changed: {savedLanguage} → {sysLang}, updating");
                _currentLanguage = sysLang;
                Preferences.Default.Set(SelectedLanguageKey, sysLang);
            }
            else
            {
                _currentLanguage = savedLanguage;
                Console.WriteLine($"[LocalizationService] Using saved language: {savedLanguage}");
            }
        }
        else if (supportedCodes.Contains(sysLang))
        {
            _currentLanguage = sysLang;
            Console.WriteLine($"[LocalizationService] Using system language: {sysLang}");
        }
        else
        {
            // Giữ default "vi"
            Console.WriteLine($"[LocalizationService] System lang '{sysLang}' not supported, using default: vi");
        }

        Console.WriteLine($"[LocalizationService] Final language: {_currentLanguage}");
        
        var culture = new CultureInfo(_currentLanguage == "zh" ? "zh-CN" : _currentLanguage);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;

    #if DEBUG
        ValidateDictionariesOnce();
    #endif
    }

    public string this[string key] => GetString(key);

    public string GetString(string key)
    {
        var dict = _currentLanguage switch
        {
            "en" => English,
            "zh" => Chinese,
            "ko" => Korean,
            "ja" => Japanese,
            "fr" => French,
            _ => Vietnamese
        };

        if (dict.TryGetValue(key, out var text))
            return text;

        // For non-Vietnamese UI, prefer English fallback over Vietnamese.
        if (_currentLanguage != "vi" && English.TryGetValue(key, out var en))
        {
            LogFallbackOnce(key, _currentLanguage, "en");
            return en;
        }

        if (Vietnamese.TryGetValue(key, out var vi))
        {
            LogFallbackOnce(key, _currentLanguage, "vi");
            return vi;
        }

        LogMissingKeyOnce(key, _currentLanguage);
        return key;
    }

#if DEBUG
    private static void ValidateDictionariesOnce()
    {
        if (_dictionaryValidationLogged) return;
        _dictionaryValidationLogged = true;

        var baseline = Vietnamese.Keys.ToHashSet(StringComparer.Ordinal);
        ValidateDictionary("en", English, baseline);
        ValidateDictionary("zh", Chinese, baseline);
        ValidateDictionary("ko", Korean, baseline);
        ValidateDictionary("ja", Japanese, baseline);
        ValidateDictionary("fr", French, baseline);
    }

    private static void ValidateDictionary(string language, Dictionary<string, string> dict, HashSet<string> baseline)
    {
        var dictKeys = dict.Keys.ToHashSet(StringComparer.Ordinal);

        var missing = baseline.Where(k => !dictKeys.Contains(k)).OrderBy(k => k).ToList();
        var extra = dictKeys.Where(k => !baseline.Contains(k)).OrderBy(k => k).ToList();

        if (missing.Count == 0 && extra.Count == 0)
        {
            var message = $"[LocalizationService] Dictionary OK: {language} ({dict.Count} keys)";
            Debug.WriteLine(message);
            Console.WriteLine(message);
            return;
        }

        if (missing.Count > 0)
        {
            var message = $"[LocalizationService] Missing keys in {language}: {string.Join(", ", missing)}";
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }

        if (extra.Count > 0)
        {
            var message = $"[LocalizationService] Extra keys in {language}: {string.Join(", ", extra)}";
            Debug.WriteLine(message);
            Console.WriteLine(message);
        }
    }

    private static void LogFallbackOnce(string key, string fromLanguage, string fallbackLanguage)
    {
        var marker = $"{fromLanguage}|{key}|{fallbackLanguage}";
        lock (FallbackLogLock)
        {
            if (!LoggedFallbacks.Add(marker)) return;
        }

        var message = $"[LocalizationService] Fallback: '{key}' from '{fromLanguage}' -> '{fallbackLanguage}'";
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    private static void LogMissingKeyOnce(string key, string language)
    {
        var marker = $"missing|{language}|{key}";
        lock (FallbackLogLock)
        {
            if (!LoggedFallbacks.Add(marker)) return;
        }

        var message = $"[LocalizationService] Missing key: '{key}' in '{language}', no fallback found";
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }
#else
    private static void LogFallbackOnce(string key, string fromLanguage, string fallbackLanguage) { }
    private static void LogMissingKeyOnce(string key, string language) { }
#endif

    private static readonly Dictionary<string, string> Vietnamese = new()
    {
        { "AppTitle", "🎧 Audio Guide Tour" },
        { "AppSubtitle", "Phố Ẩm thực Vĩnh Khánh, Quận 4" },
        { "MapTitle", "Audio Tour" },
        { "MapSubtitle", "Khánh Hội" },
        { "SearchPlaceholder", "Tìm điểm tham quan..." },
        { "GpsOn", "GPS đang bật" },
        { "GpsOff", "GPS đang tắt" },
        { "Ready", "Sẵn sàng" },
        { "TapPoiHint", "Chạm điểm tham quan" },
        { "AutoPlayHint", "Chuẩn bị phát tự động" },
        { "AudioTour", "Audio Tour" },
        { "Playing", "Đang phát..." },
        { "Finished", "Đã phát xong" },
        { "MapBtn", "🗺️ Mở Bản Đồ" },
        { "PoiListBtn", "📍 Địa điểm" },
        { "QrBtn", "📷 Quét QR Code" },
        { "OfflineBtn", "📦 Offline" },
        { "Syncing", "🔄 Đang đồng bộ dữ liệu..." },
        { "SyncSuccess", "✅ Đồng bộ thành công" },
        { "OfflineMode", "📴 Offline — dùng dữ liệu đã cache" },
        { "ReadyToMove", "Đang chờ bạn di chuyển..." },
        { "PlayingAudio", "🔊 Đang phát Audio" },
        { "Cooldown", "⏳ Đang chờ hồi" },
        { "PoiDetailTitle", "Chi tiết Địa điểm" },
        { "PlayBtn", "🔊 Phát Audio / TTS" },
        { "StopBtn", "⏹ Dừng phát" },
        { "ScanQrPrompt", "📷 Hướng camera vào mã QR" },
        { "CloseBtn", "✕ Đóng" },
        { "LangSwitchBtn", "🌐 Ngôn ngữ" },
        { "SettingsTitle", "⚙️ Cài đặt" },
        { "LangSection", "🌐 Ngôn ngữ / Language" },
        { "AudioPrefsSection", "🎵 Tùy chỉnh âm thanh" },
        { "AutoPlayNear", "Tự động phát khi đến gần" },
        { "BackgroundPlay", "Phát khi khóa màn hình" },
        { "CacheSection", "💾 Audio Cache" },
        { "OfflineSection", "📦 Tải offline tự động" },
        { "AutoDownloadLabel", "Tự động tải audio" },
        { "WifiOnlyLabel", "Chỉ tải khi có WiFi" },
        { "DataSection", "💾 Quản lý dữ liệu" },
        { "CacheDataLabel", "Dữ liệu đệm (Cache)" },
        { "CleanupDownloads", "Dọn dẹp tải xuống" },
        { "ClearCache", "🗑️ Xóa cache audio" },
        { "ClearCacheConfirmTitle", "Xóa cache?" },
        { "ClearCacheConfirmMsg", "Tất cả file audio đã tải sẽ bị xóa. Bạn cần đồng bộ lại." },
        { "ClearCacheOk", "Xóa" },
        { "ClearCacheCancel", "Hủy" },
        { "ClearCacheSuccess", "Đã xóa toàn bộ cache audio." },
        { "InfoSection", "ℹ️ Thông tin" },
        { "VersionLabel", "Phiên bản" },
        { "OfflineTitle", "Tour Offline" },
        { "StorageHeader", "Dung lượng thiết bị" },
        { "StorageUsage", "TourMap đang dùng: 0 MB" },
        { "StorageAvailable", "Dung lượng trống còn nhiều (khoảng 32 GB)" },
        { "PacksHeader", "Các gói nội dung" },
        { "PackQ4Title", "Phố Ẩm Thực Vĩnh Khánh" },
        { "PackQ4Desc", "Audio, Hình ảnh & Bản đồ" },
        { "Download", "Tải xuống" },
        { "Delete", "Xóa" },
        { "DownloadSuccessTitle", "Thành công" },
        { "DownloadSuccessMsg", "Đã tải xong toàn bộ Audio và Bản đồ điểm đến. Bạn có thể dùng ngoại tuyến." },
        { "Awesome", "Tuyệt vời" },
        // Offline page - Network & Storage
        { "NetworkSection", "KẾT NỐI" },
        { "CheckingNetwork", "Đang kiểm tra kết nối..." },
        { "WifiConnected", "✓ Đã kết nối WiFi" },
        { "MobileData", "⚠ Đang dùng dữ liệu di động" },
        { "NoNetwork", "✗ Không có kết nối" },
        { "AutoDownload", "Tự động tải khi có WiFi" },
        { "NeverSynced", "Chưa đồng bộ" },
        { "JustNow", "Vừa xong" },
        { "MinutesAgo", "{0} phút trước" },
        { "HoursAgo", "{0} giờ trước" },
        { "DaysAgo", "{0} ngày trước" },
        { "StorageUsingFormat", "TourMap đang dùng: {0}" },
        { "StorageEmpty", "TourMap đang dùng: 0 MB" },
        { "DeleteConfirmTitle", "Xóa gói tải xuống" },
        { "DeleteConfirmMsg", "Việc xóa sẽ khiến bạn không thể nghe Audio khi không có mạng. Bạn chắc chắn chứ?" },
        { "Deleting", "Đang xóa..." },
        { "DeleteSuccessTitle", "Thành công" },
        { "DeleteSuccessMsg", "Đã xóa gói Tour tham quan." },
        { "NeedNetwork", "Cần kết nối internet để tải nội dung." },
        { "Cancel", "Hủy" },
        { "OK", "OK" },
        { "Error", "Lỗi" },
        { "DownloadError", "Có lỗi xảy ra khi tải xuống." },
        { "ComingSoon", "Sắp ra mắt" },
        { "PackNguyenHueTitle", "Phố Đi Bộ Nguyễn Huệ" },
        { "FilesUnit", "tệp" },
        { "PoiListTitle", "Địa điểm" },
        { "PoiNearbyFormat", "📍 {0} gần bạn" },
        { "PoiVisitedFormat", "🎧 {0} đã nghe" },
        { "PoiTotalFormat", "{0} tổng cộng" },
        { "PoiFilterAll", "Tất cả" },
        { "PoiFilterFood", "Ẩm thực" },
        { "PoiFilterHeritage", "Di tích" },
        { "PoiFilterTemple", "Chùa" },
        { "PoiFilterMarket", "Chợ" },
        { "PoiFilterPark", "Công viên" },
        { "PoiFilterCulture", "Văn hóa" },
        { "PoiEmptyTitle", "Không tìm thấy" },
        { "PoiEmptyHint", "Thử đổi bộ lọc hoặc từ khóa" },
        { "PoiPriorityHigh", "⭐ Ưu tiên cao" },
        { "PoiPriorityFeatured", "📍 Nổi bật" },
        { "PoiPriorityDefault", "📍 Điểm tham quan" },
        { "PoiCategoryDefault", "📍 ĐIỂM THAM QUAN" },
        { "PoiAudioRecorded", "🎙️ Audio thu sẵn" },
        { "PoiAudioTts", "🤖 Giọng TTS" },
        { "PoiStatusAudioReady", "🎵 MP3 sẵn sàng" },
        { "PoiStatusTtsReady", "🗣️ TTS sẵn sàng" },
        { "AudioTts", "🎧 TTS" },
        { "QrHeader", "Quét mã QR" },
        { "QrInstructions", "Đưa mã vào khung để quét" },
        { "QrScanHint", "Tự động nhận diện mã QR" },
        { "QrSuccessTitle", "Mã QR đã nhận diện!" },
        { "QrPlayButton", "Mở và phát thuyết minh" },
        { "QrScanAgain", "Quét mã khác" },
        { "PoiDetailDescription", "Giới thiệu" },
        // Camera errors
        { "CameraCheckingPermission", "Đang kiểm tra quyền camera..." },
        { "CameraRequesting", "Đang yêu cầu quyền camera..." },
        { "CameraPermissionDenied", "Cần quyền camera để tiếp tục. Vui lòng cấp quyền trong Settings." },
        { "CameraPermissionDeniedShort", "Quyền camera bị từ chối" },
        { "CameraStartingFormat", "Đang khởi động camera... ({0}/{1})" },
        { "CameraStartError", "Lỗi khởi động camera. Vui lòng thử lại." },
        { "CameraStartFailed", "Không thể khởi động camera" },
        { "CameraErrorAfterRetry", "Lỗi sau {0} lần thử: {1}" },
        { "CameraRetry", "Thử lại" },
        { "CameraRetrying", "Đang thử..." },
        { "CameraInvalidQr", "Mã QR không hợp lệ. Đang quét lại..." },
        { "SeeDetails", "Xem chi tiết" },
        { "PoiDetailAudio", "Thuyết minh audio" },
        { "GpsCoordinates", "Tọa độ GPS" },
        { "AudioStat", "thuyết minh" },
        { "WalkStat", "phút đi bộ" },
        // Profile
        { "ProfileTitle", "Hồ sơ" },
        { "EmailUnregistered", "Chưa đăng ký email" },
        { "GuestRole", "👤 Khách" },
        { "MemberRole", "✅ Thành viên" },
        { "PremiumRole", "⭐ Premium" },
        { "MemberSinceFormat", "Thành viên từ {0}" },
        { "GuestBenefitsHeader", "💡 Đăng ký tài khoản để:" },
        { "GuestBenefit1", "• Lưu lịch sử tour cá nhân" },
        { "GuestBenefit2", "• Đồng bộ dữ liệu đa thiết bị" },
        { "GuestBenefit3", "• Trải nghiệm đầy đủ nội dung" },
        { "RegisterNowBtn", "Đăng ký ngay" },
        { "EmailLabel", "📧 Email" },
        { "RoleLabel", "🏷️ Vai trò" },
        { "AuthMethodLabel", "🔐 Xác thực" },
        { "LogoutConfirmTitle", "Đăng xuất" },
        { "LogoutConfirmMsg", "Bạn có chắc chắn muốn đăng xuất?" },
        { "LogoutBtn", "🚪 Đăng xuất" },
        { "LogoutCancelBtn", "Hủy" },
        { "AppVersionFormat", "TourMap {0} • Audio Tour Guide" },
        { "GamificationAudio", "Thời gian nghe" },
        { "GamificationPlaces", "Địa điểm đã đến" },
        { "GamificationBadges", "Danh hiệu" },
        { "MyFavorites", "Yêu thích của tôi" },
        { "NoFavorites", "Chưa có địa điểm yêu thích" },
        { "ChangePasswordBtn", "🔐 Đổi mật khẩu" },
    };

    private static readonly Dictionary<string, string> English = new()
    {
        { "AppTitle", "🎧 Audio Guide Tour" },
        { "AppSubtitle", "Vinh Khanh Food Street, District 4" },
        { "MapTitle", "Audio Tour" },
        { "MapSubtitle", "Vinh Khanh" },
        { "SearchPlaceholder", "Search for places..." },
        { "GpsOn", "GPS is on" },
        { "GpsOff", "GPS is off" },
        { "Ready", "Ready" },
        { "TapPoiHint", "Tap a point of interest" },
        { "AutoPlayHint", "Auto-play ready" },
        { "AudioTour", "Audio Tour" },
        { "Playing", "Playing..." },
        { "Finished", "Finished playing" },
        { "MapBtn", "🗺️ Open Map" },
        { "PoiListBtn", "📍 Places" },
        { "QrBtn", "📷 Scan QR Code" },
        { "OfflineBtn", "📦 Offline" },
        { "Syncing", "🔄 Syncing data..." },
        { "SyncSuccess", "✅ Sync successful" },
        { "OfflineMode", "📴 Offline — using cached data" },
        { "ReadyToMove", "Waiting for your movement..." },
        { "PlayingAudio", "🔊 Playing Audio" },
        { "Cooldown", "⏳ Cooldown" },
        { "PoiDetailTitle", "Location Details" },
        { "PlayBtn", "🔊 Play Audio / TTS" },
        { "StopBtn", "⏹ Stop playback" },
        { "ScanQrPrompt", "📷 Point camera at QR code" },
        { "CloseBtn", "✕ Close" },
        { "LangSwitchBtn", "🌐 Language" },
        { "SettingsTitle", "⚙️ Settings" },
        { "LangSection", "🌐 Language" },
        { "AudioPrefsSection", "🎵 Audio Preferences" },
        { "AutoPlayNear", "Auto-play when nearby" },
        { "BackgroundPlay", "Play when screen is locked" },
        { "CacheSection", "💾 Audio Cache" },
        { "OfflineSection", "📦 Auto Offline Download" },
        { "AutoDownloadLabel", "Auto-download audio" },
        { "WifiOnlyLabel", "WiFi only download" },
        { "DataSection", "💾 Data Management" },
        { "CacheDataLabel", "Cached data" },
        { "CleanupDownloads", "Clean up downloads" },
        { "ClearCache", "🗑️ Clear audio cache" },
        { "ClearCacheConfirmTitle", "Clear cache?" },
        { "ClearCacheConfirmMsg", "All downloaded audio files will be deleted." },
        { "ClearCacheOk", "Clear" },
        { "ClearCacheCancel", "Cancel" },
        { "ClearCacheSuccess", "All audio cache cleared." },
        { "InfoSection", "ℹ️ Info" },
        { "VersionLabel", "Version" },
        { "OfflineTitle", "Offline Tour" },
        { "StorageHeader", "Device Storage" },
        { "StorageUsage", "TourMap using: 0 MB" },
        { "StorageAvailable", "Plenty of space left (about 32 GB)" },
        { "PacksHeader", "Content Packs" },
        { "PackQ4Title", "Vinh Khanh Food Street" },
        { "PackQ4Desc", "Audio, Images & Maps" },
        { "Download", "Download" },
        { "Delete", "Delete" },
        { "DownloadSuccessTitle", "Success" },
        { "DownloadSuccessMsg", "All Audio and Maps downloaded. You can use offline." },
        { "Awesome", "Great" },
        // Offline page - Network & Storage
        { "NetworkSection", "CONNECTION" },
        { "CheckingNetwork", "Checking connection..." },
        { "WifiConnected", "✓ WiFi connected" },
        { "MobileData", "⚠ Using mobile data" },
        { "NoNetwork", "✗ No connection" },
        { "AutoDownload", "Auto-download on WiFi" },
        { "NeverSynced", "Never synced" },
        { "JustNow", "Just now" },
        { "MinutesAgo", "{0} minutes ago" },
        { "HoursAgo", "{0} hours ago" },
        { "DaysAgo", "{0} days ago" },
        { "StorageUsingFormat", "TourMap using: {0}" },
        { "StorageEmpty", "TourMap using: 0 MB" },
        { "DeleteConfirmTitle", "Delete download pack?" },
        { "DeleteConfirmMsg", "Deleting will prevent you from listening offline. Are you sure?" },
        { "Deleting", "Deleting..." },
        { "DeleteSuccessTitle", "Success" },
        { "DeleteSuccessMsg", "Tour pack deleted." },
        { "NeedNetwork", "Internet connection required to download content." },
        { "Cancel", "Cancel" },
        { "OK", "OK" },
        { "Error", "Error" },
        { "DownloadError", "An error occurred while downloading." },
        { "ComingSoon", "Coming soon" },
        { "PackNguyenHueTitle", "Nguyen Hue Walking Street" },
        { "FilesUnit", "files" },
        { "PoiListTitle", "Places" },
        { "PoiNearbyFormat", "📍 {0} nearby" },
        { "PoiVisitedFormat", "🎧 {0} listened" },
        { "PoiTotalFormat", "{0} total" },
        { "PoiFilterAll", "All" },
        { "PoiFilterFood", "Food" },
        { "PoiFilterHeritage", "Heritage" },
        { "PoiFilterTemple", "Temple" },
        { "PoiFilterMarket", "Market" },
        { "PoiFilterPark", "Park" },
        { "PoiFilterCulture", "Culture" },
        { "PoiEmptyTitle", "No results" },
        { "PoiEmptyHint", "Try a different filter or keyword" },
        { "PoiPriorityHigh", "⭐ High priority" },
        { "PoiPriorityFeatured", "📍 Featured" },
        { "PoiPriorityDefault", "📍 Point of interest" },
        { "PoiCategoryDefault", "📍 POINT OF INTEREST" },
        { "PoiAudioRecorded", "🎙️ Recorded audio" },
        { "PoiAudioTts", "🤖 TTS voice" },
        { "PoiStatusAudioReady", "🎵 MP3 ready" },
        { "PoiStatusTtsReady", "🗣️ TTS ready" },
        { "AudioTts", "🎧 TTS" },
        { "QrHeader", "Scan QR Code" },
        { "QrInstructions", "Point camera at QR code" },
        { "QrScanHint", "Auto-detecting QR code" },
        { "QrSuccessTitle", "QR code recognized!" },
        { "QrPlayButton", "Open and play narration" },
        { "QrScanAgain", "Scan another code" },
        { "PoiDetailDescription", "Description" },
        // Camera errors
        { "CameraCheckingPermission", "Checking camera permission..." },
        { "CameraRequesting", "Requesting camera permission..." },
        { "CameraPermissionDenied", "Camera permission required. Please grant permission in Settings." },
        { "CameraPermissionDeniedShort", "Camera permission denied" },
        { "CameraStartingFormat", "Starting camera... ({0}/{1})" },
        { "CameraStartError", "Error starting camera. Please try again." },
        { "CameraStartFailed", "Unable to start camera" },
        { "CameraErrorAfterRetry", "Error after {0} attempts: {1}" },
        { "CameraRetry", "Retry" },
        { "CameraRetrying", "Retrying..." },
        { "CameraInvalidQr", "Invalid QR code. Rescanning..." },
        { "SeeDetails", "See details" },
        { "PoiDetailAudio", "Audio narration" },
        { "GpsCoordinates", "GPS Coordinates" },
        { "AudioStat", "audio" },
        { "WalkStat", "min walk" },
        // Profile
        { "ProfileTitle", "Profile" },
        { "EmailUnregistered", "Email not registered" },
        { "GuestRole", "👤 Guest" },
        { "MemberRole", "✅ Member" },
        { "PremiumRole", "⭐ Premium" },
        { "MemberSinceFormat", "Member since {0}" },
        { "GuestBenefitsHeader", "💡 Register an account to:" },
        { "GuestBenefit1", "• Save personal tour history" },
        { "GuestBenefit2", "• Sync data across devices" },
        { "GuestBenefit3", "• Experience full content" },
        { "RegisterNowBtn", "Register now" },
        { "EmailLabel", "📧 Email" },
        { "RoleLabel", "🏷️ Role" },
        { "AuthMethodLabel", "🔐 Auth" },
        { "LogoutConfirmTitle", "Logout" },
        { "LogoutConfirmMsg", "Are you sure you want to log out?" },
        { "LogoutBtn", "🚪 Logout" },
        { "LogoutCancelBtn", "Cancel" },
        { "AppVersionFormat", "TourMap {0} • Audio Tour Guide" },
        { "GamificationAudio", "Time listened" },
        { "GamificationPlaces", "Places visited" },
        { "GamificationBadges", "Badges" },
        { "MyFavorites", "My Favorites" },
    };

    private static readonly Dictionary<string, string> Chinese = new()
    {
        { "AppTitle", "🎧 语音导览" },
        { "AppSubtitle", "荣庆美食街，第四郡" },
        { "MapTitle", "语音导览" },
        { "MapSubtitle", "荣庆" },
        { "SearchPlaceholder", "搜索景点..." },
        { "GpsOn", "GPS已开启" },
        { "GpsOff", "GPS已关闭" },
        { "Ready", "准备就绪" },
        { "TapPoiHint", "点击景点" },
        { "AutoPlayHint", "自动播放就绪" },
        { "AudioTour", "语音导览" },
        { "Playing", "播放中..." },
        { "Finished", "播放完成" },
        { "MapBtn", "🗺️ 打开地图" },
        { "PoiListBtn", "📍 地点" },
        { "QrBtn", "📷 扫描二维码" },
        { "OfflineBtn", "📦 离线" },
        { "Syncing", "🔄 正在同步数据..." },
        { "SyncSuccess", "✅ 同步成功" },
        { "OfflineMode", "📴 离线模式" },
        { "ReadyToMove", "等待您移动..." },
        { "PlayingAudio", "🔊 正在播放" },
        { "Cooldown", "⏳ 冷却中" },
        { "PoiDetailTitle", "地点详情" },
        { "PlayBtn", "🔊 播放音频" },
        { "StopBtn", "⏹ 停止" },
        { "ScanQrPrompt", "📷 将相机对准二维码" },
        { "CloseBtn", "✕ 关闭" },
        { "LangSwitchBtn", "🌐 语言" },
        { "SettingsTitle", "⚙️ 设置" },
        { "LangSection", "🌐 语言" },
        { "AudioPrefsSection", "🎵 音频偏好" },
        { "AutoPlayNear", "靠近时自动播放" },
        { "BackgroundPlay", "锁屏时播放" },
        { "CacheSection", "💾 音频缓存" },
        { "OfflineSection", "📦 自动离线下载" },
        { "AutoDownloadLabel", "自动下载音频" },
        { "WifiOnlyLabel", "仅WiFi下载" },
        { "DataSection", "💾 数据管理" },
        { "CacheDataLabel", "缓存数据" },
        { "CleanupDownloads", "清理下载内容" },
        { "ClearCache", "🗑️ 清除缓存" },
        { "ClearCacheConfirmTitle", "清除缓存？" },
        { "ClearCacheConfirmMsg", "所有已下载的音频文件将被删除。" },
        { "ClearCacheOk", "清除" },
        { "ClearCacheCancel", "取消" },
        { "ClearCacheSuccess", "缓存已清除。" },
        { "InfoSection", "ℹ️ 信息" },
        { "VersionLabel", "版本" },
        { "PackNguyenHueTitle", "阮惠步行街" },
        { "FilesUnit", "文件" },
        { "PoiListTitle", "景点" },
        { "PoiNearbyFormat", "📍 {0}附近" },
        { "PoiVisitedFormat", "🎧 {0}已听" },
        { "PoiTotalFormat", "{0}总数" },
        { "PoiFilterAll", "全部" },
        { "PoiFilterFood", "美食" },
        { "PoiFilterHeritage", "遗迹" },
        { "PoiFilterTemple", "寺庙" },
        { "PoiFilterMarket", "市场" },
        { "PoiFilterPark", "公园" },
        { "PoiFilterCulture", "文化" },
        { "PoiEmptyTitle", "未找到结果" },
        { "PoiEmptyHint", "尝试更改筛选条件或关键词" },
        { "PoiPriorityHigh", "⭐ 高优先级" },
        { "PoiPriorityFeatured", "📍 精选" },
        { "PoiPriorityDefault", "📍 景点" },
        { "PoiCategoryDefault", "📍 景点" },
        { "PoiAudioRecorded", "🎙️ 录制音频" },
        { "PoiAudioTts", "🤖 TTS语音" },
        { "PoiStatusAudioReady", "🎵 MP3已就绪" },
        { "PoiStatusTtsReady", "🗣️ TTS已就绪" },
        { "AudioTts", "🎧 文本转语音" },
        { "QrHeader", "扫描二维码" },
        { "QrInstructions", "将摄像头对准二维码" },
        { "QrScanHint", "自动识别二维码" },
        { "QrSuccessTitle", "二维码已识别！" },
        { "QrPlayButton", "打开并播放解说" },
        { "QrScanAgain", "扫描另一个代码" },
        { "PoiDetailDescription", "介绍" },
        // Camera errors
        { "CameraCheckingPermission", "正在检查相机权限..." },
        { "CameraRequesting", "正在请求相机权限..." },
        { "CameraPermissionDenied", "需要相机权限。请在设置中授予权限。" },
        { "CameraPermissionDeniedShort", "相机权限被拒绝" },
        { "CameraStartingFormat", "正在启动相机... ({0}/{1})" },
        { "CameraStartError", "相机启动错误。请重试。" },
        { "CameraStartFailed", "无法启动相机" },
        { "CameraErrorAfterRetry", "{0}次尝试后出错：{1}" },
        { "CameraRetry", "重试" },
        { "CameraRetrying", "正在重试..." },
        { "CameraInvalidQr", "无效的二维码。正在重新扫描..." },
        { "SeeDetails", "查看详情" },
        { "PoiDetailAudio", "语音讲解" },
        { "GpsCoordinates", "GPS坐标" },
        { "AudioStat", "解说" },
        { "WalkStat", "分钟步行" },
    };

    private static readonly Dictionary<string, string> Korean = new()
    {
        { "AppTitle", "🎧 오디오 가이드 투어" },
        { "AppSubtitle", "빈칸 푸드 스트리트, 4군" },
        { "MapTitle", "오디오 투어" },
        { "MapSubtitle", "빈칸" },
        { "SearchPlaceholder", "장소 검색..." },
        { "GpsOn", "GPS 켜짐" },
        { "GpsOff", "GPS 꺼짐" },
        { "Ready", "준비 완료" },
        { "TapPoiHint", "관심 지점 탭" },
        { "AutoPlayHint", "자동 재생 준비" },
        { "AudioTour", "오디오 투어" },
        { "Playing", "재생 중..." },
        { "Finished", "재생 완료" },
        { "MapBtn", "🗺️ 지도 열기" },
        { "PoiListBtn", "📍 장소" },
        { "QrBtn", "📷 QR 코드 스캔" },
        { "OfflineBtn", "📦 오프라인" },
        { "Syncing", "🔄 데이터 동기화 중..." },
        { "SyncSuccess", "✅ 동기화 성공" },
        { "OfflineMode", "📴 오프라인 모드" },
        { "ReadyToMove", "이동을 기다리는 중..." },
        { "PlayingAudio", "🔊 재생 중" },
        { "Cooldown", "⏳ 쿨다운" },
        { "PoiDetailTitle", "장소 세부 정보" },
        { "PlayBtn", "🔊 오디오 재생" },
        { "StopBtn", "⏹ 정지" },
        { "ScanQrPrompt", "📷 QR 코드에 카메라를 향하세요" },
        { "CloseBtn", "✕ 닫기" },
        { "LangSwitchBtn", "🌐 언어" },
        { "SettingsTitle", "⚙️ 설정" },
        { "LangSection", "🌐 언어" },
        { "AudioPrefsSection", "🎵 오디오 설정" },
        { "AutoPlayNear", "근처 도착 시 자동 재생" },
        { "BackgroundPlay", "화면 잠금 시 재생" },
        { "CacheSection", "💾 오디오 캐시" },
        { "OfflineSection", "📦 자동 오프라인 다운로드" },
        { "AutoDownloadLabel", "오디오 자동 다운로드" },
        { "WifiOnlyLabel", "WiFi에서만 다운로드" },
        { "DataSection", "💾 데이터 관리" },
        { "CacheDataLabel", "캐시 데이터" },
        { "CleanupDownloads", "다운로드 정리" },
        { "ClearCache", "🗑️ 캐시 지우기" },
        { "ClearCacheConfirmTitle", "캐시를 지울까요?" },
        { "ClearCacheConfirmMsg", "다운로드된 음성 파일이 모두 삭제됩니다." },
        { "ClearCacheOk", "삭제" },
        { "ClearCacheCancel", "취소" },
        { "ClearCacheSuccess", "캐시가 삭제되었습니다." },
        { "InfoSection", "ℹ️ 정보" },
        { "VersionLabel", "버전" },
        { "PackNguyenHueTitle", "응우옌휘 보행자 거리" },
        { "FilesUnit", "파일" },
        { "PoiListTitle", "장소" },
        { "PoiNearbyFormat", "📍 {0}근처" },
        { "PoiVisitedFormat", "🎧 {0}들었음" },
        { "PoiTotalFormat", "{0}총" },
        { "PoiFilterAll", "모두" },
        { "PoiFilterFood", "음식" },
        { "PoiFilterHeritage", "유산" },
        { "PoiFilterTemple", "사찰" },
        { "PoiFilterMarket", "시장" },
        { "PoiFilterPark", "공원" },
        { "PoiFilterCulture", "문화" },
        { "PoiEmptyTitle", "결과 없음" },
        { "PoiEmptyHint", "필터 또는 검색어를 변경해 보세요" },
        { "PoiPriorityHigh", "⭐ 높은 우선순위" },
        { "PoiPriorityFeatured", "📍 추천" },
        { "PoiPriorityDefault", "📍 관심 장소" },
        { "PoiCategoryDefault", "📍 관심 장소" },
        { "PoiAudioRecorded", "🎙️ 녹음 오디오" },
        { "PoiAudioTts", "🤖 TTS 음성" },
        { "PoiStatusAudioReady", "🎵 MP3 준비됨" },
        { "PoiStatusTtsReady", "🗣️ TTS 준비됨" },
        { "AudioTts", "🎧 TTS" },
        { "QrHeader", "QR 코드 스캔" },
        { "QrInstructions", "카메라를 QR 코드에 향하세요" },
        { "QrScanHint", "QR 코드 자동 감지 중" },
        { "QrSuccessTitle", "QR 코드 인식됨!" },
        { "QrPlayButton", "열기 및 해설 재생" },
        { "QrScanAgain", "다른 코드 스캔" },
        { "PoiDetailDescription", "소개" },
        // Camera errors
        { "CameraCheckingPermission", "카메라 권한 확인 중..." },
        { "CameraRequesting", "카메라 권한 요청 중..." },
        { "CameraPermissionDenied", "카메라 권한이 필요합니다. 설정에서 권한을 부여해주세요." },
        { "CameraPermissionDeniedShort", "카메라 권한이 거부되었습니다" },
        { "CameraStartingFormat", "카메라 시작 중... ({0}/{1})" },
        { "CameraStartError", "카메라 시작 오류입니다. 다시 시도해주세요." },
        { "CameraStartFailed", "카메라를 시작할 수 없습니다." },
        { "CameraErrorAfterRetry", "{0}번 시도 후 오류: {1}" },
        { "CameraRetry", "재시도" },
        { "CameraRetrying", "재시도 중..." },
        { "CameraInvalidQr", "유효하지 않은 QR 코드입니다. 다시 스캔 중..." },
        { "SeeDetails", "세부 정보 보기" },
        { "PoiDetailAudio", "오디오 해설" },
        { "GpsCoordinates", "GPS 좌표" },
        { "AudioStat", "해설" },
        { "WalkStat", "분 도보" },
    };

    private static readonly Dictionary<string, string> Japanese = new()
    {
        { "AppTitle", "🎧 音声ガイドツアー" },
        { "AppSubtitle", "ヴィンカン グルメ通り、4区" },
        { "MapTitle", "オーディオツアー" },
        { "MapSubtitle", "ヴィンカン" },
        { "SearchPlaceholder", "スポットを検索..." },
        { "GpsOn", "GPSオン" },
        { "GpsOff", "GPSオフ" },
        { "Ready", "準備完了" },
        { "TapPoiHint", "スポットをタップ" },
        { "AutoPlayHint", "自動再生準備" },
        { "AudioTour", "オーディオツアー" },
        { "Playing", "再生中..." },
        { "Finished", "再生完了" },
        { "MapBtn", "🗺️ 地図を開く" },
        { "PoiListBtn", "📍 スポット" },
        { "QrBtn", "📷 QRコードをスキャン" },
        { "OfflineBtn", "📦 オフライン" },
        { "Syncing", "🔄 データを同期中..." },
        { "SyncSuccess", "✅ 同期成功" },
        { "OfflineMode", "📴 オフラインモード" },
        { "ReadyToMove", "移動をお待ちしています..." },
        { "PlayingAudio", "🔊 再生中" },
        { "Cooldown", "⏳ クールダウン" },
        { "PoiDetailTitle", "スポット詳細" },
        { "PlayBtn", "🔊 音声を再生" },
        { "StopBtn", "⏹ 停止" },
        { "ScanQrPrompt", "📷 カメラをQRコードに向けてください" },
        { "CloseBtn", "✕ 閉じる" },
        { "LangSwitchBtn", "🌐 言語" },
        { "SettingsTitle", "⚙️ 設定" },
        { "LangSection", "🌐 言語" },
        { "AudioPrefsSection", "🎵 オーディオ設定" },
        { "AutoPlayNear", "近くで自動再生" },
        { "BackgroundPlay", "画面ロック時も再生" },
        { "CacheSection", "💾 音声キャッシュ" },
        { "OfflineSection", "📦 自動オフラインDL" },
        { "AutoDownloadLabel", "オーディオ自動DL" },
        { "WifiOnlyLabel", "WiFiのみでDL" },
        { "DataSection", "💾 データ管理" },
        { "CacheDataLabel", "キャッシュデータ" },
        { "CleanupDownloads", "ダウンロードを整理" },
        { "ClearCache", "🗑️ キャッシュを削除" },
        { "ClearCacheConfirmTitle", "キャッシュを削除しますか？" },
        { "ClearCacheConfirmMsg", "ダウンロードされたすべての音声ファイルが削除されます。" },
        { "ClearCacheOk", "削除" },
        { "ClearCacheCancel", "キャンセル" },
        { "ClearCacheSuccess", "キャッシュが削除されました。" },
        { "InfoSection", "ℹ️ 情報" },
        { "VersionLabel", "バージョン" },
        { "PackNguyenHueTitle", "応援グエン通行歩道" },
        { "FilesUnit", "ファイル" },
        { "PoiListTitle", "スポット" },
        { "PoiNearbyFormat", "📍 {0}近く" },
        { "PoiVisitedFormat", "🎧 {0}聞きました" },
        { "PoiTotalFormat", "{0}合計" },
        { "PoiFilterAll", "すべて" },
        { "PoiFilterFood", "食べ物" },
        { "PoiFilterHeritage", "遺産" },
        { "PoiFilterTemple", "寺院" },
        { "PoiFilterMarket", "市場" },
        { "PoiFilterPark", "公園" },
        { "PoiFilterCulture", "文化" },
        { "PoiEmptyTitle", "結果がありません" },
        { "PoiEmptyHint", "フィルターまたはキーワードを変更してみてください" },
        { "PoiPriorityHigh", "⭐ 高優先度" },
        { "PoiPriorityFeatured", "📍 おすすめ" },
        { "PoiPriorityDefault", "📍 スポット" },
        { "PoiCategoryDefault", "📍 スポット" },
        { "PoiAudioRecorded", "🎙️ 録音済み音声" },
        { "PoiAudioTts", "🤖 TTS音声" },
        { "PoiStatusAudioReady", "🎵 MP3準備完了" },
        { "PoiStatusTtsReady", "🗣️ TTS準備完了" },
        { "AudioTts", "🎧 テキスト音声" },
        { "QrHeader", "QRコードをスキャン" },
        { "QrInstructions", "カメラをQRコードに向けてください" },
        { "QrScanHint", "QRコード自動検出中" },
        { "QrSuccessTitle", "QRコードが認識されました！" },
        { "QrPlayButton", "開いて解説を再生" },
        { "QrScanAgain", "別のコードをスキャン" },
        { "PoiDetailDescription", "説明" },
        // Camera errors
        { "CameraCheckingPermission", "カメラの許可を確認中..." },
        { "CameraRequesting", "カメラの許可をリクエスト中..." },
        { "CameraPermissionDenied", "カメラの許可が必要です。設定で許可を付与してください。" },
        { "CameraPermissionDeniedShort", "カメラ権限が拒否されました" },
        { "CameraStartingFormat", "カメラを起動中... ({0}/{1})" },
        { "CameraStartError", "カメラの起動に失敗しました。もう一度お試しください。" },
        { "CameraStartFailed", "カメラを起動できません" },
        { "CameraErrorAfterRetry", "{0}回の試行後にエラー：{1}" },
        { "CameraRetry", "再試行" },
        { "CameraRetrying", "再試行中..." },
        { "CameraInvalidQr", "無効なQRコード。再スキャン中..." },
        { "SeeDetails", "詳細を見る" },
        { "PoiDetailAudio", "音声解説" },
        { "GpsCoordinates", "GPS座標" },
        { "AudioStat", "解説" },
        { "WalkStat", "分徒歩" },
    };

    private static readonly Dictionary<string, string> French = new()
    {
        { "AppTitle", "🎧 Visite Audio Guidée" },
        { "AppSubtitle", "Rue Gastronomique Vinh Khanh, Arrondissement 4" },
        { "MapTitle", "Visite Audio" },
        { "MapSubtitle", "Vinh Khanh" },
        { "SearchPlaceholder", "Rechercher des lieux..." },
        { "GpsOn", "GPS activé" },
        { "GpsOff", "GPS désactivé" },
        { "Ready", "Prêt" },
        { "TapPoiHint", "Touchez un point d'intérêt" },
        { "AutoPlayHint", "Lecture auto prête" },
        { "AudioTour", "Visite Audio" },
        { "Playing", "Lecture..." },
        { "Finished", "Lecture terminée" },
        { "MapBtn", "🗺️ Ouvrir la carte" },
        { "PoiListBtn", "📍 Lieux" },
        { "QrBtn", "📷 Scanner QR Code" },
        { "OfflineBtn", "📦 Hors ligne" },
        { "Syncing", "🔄 Synchronisation en cours..." },
        { "SyncSuccess", "✅ Synchronisation réussie" },
        { "OfflineMode", "📴 Mode hors ligne" },
        { "ReadyToMove", "En attente de votre déplacement..." },
        { "PlayingAudio", "🔊 Lecture en cours" },
        { "Cooldown", "⏳ Pause" },
        { "PoiDetailTitle", "Détails du lieu" },
        { "PlayBtn", "🔊 Lire l'audio" },
        { "StopBtn", "⏹ Arrêter" },
        { "ScanQrPrompt", "📷 Pointez la caméra vers le QR code" },
        { "CloseBtn", "✕ Fermer" },
        { "LangSwitchBtn", "🌐 Langue" },
        { "SettingsTitle", "⚙️ Paramètres" },
        { "LangSection", "🌐 Langue" },
        { "AudioPrefsSection", "🎵 Préférences audio" },
        { "AutoPlayNear", "Lecture auto à proximité" },
        { "BackgroundPlay", "Lire écran verrouillé" },
        { "CacheSection", "💾 Cache Audio" },
        { "OfflineSection", "📦 DL Offline Auto" },
        { "AutoDownloadLabel", "Téléchargement auto" },
        { "WifiOnlyLabel", "WiFi uniquement" },
        { "DataSection", "💾 Gestion des données" },
        { "CacheDataLabel", "Données en cache" },
        { "CleanupDownloads", "Nettoyer les téléchargements" },
        { "ClearCache", "🗑️ Vider le cache" },
        { "ClearCacheConfirmTitle", "Vider le cache ?" },
        { "ClearCacheConfirmMsg", "Tous les fichiers audio téléchargés seront supprimés." },
        { "ClearCacheOk", "Supprimer" },
        { "ClearCacheCancel", "Annuler" },
        { "ClearCacheSuccess", "Cache audio vidé." },
        { "InfoSection", "ℹ️ Infos" },
        { "VersionLabel", "Version" },
        { "PackNguyenHueTitle", "Rue piétonne Nguyen Hue" },
        { "FilesUnit", "fichiers" },
        { "PoiListTitle", "Lieux" },
        { "PoiNearbyFormat", "📍 {0} à proximité" },
        { "PoiVisitedFormat", "🎧 {0} écouté" },
        { "PoiTotalFormat", "{0} total" },
        { "PoiFilterAll", "Tous" },
        { "PoiFilterFood", "Nourriture" },
        { "PoiFilterHeritage", "Patrimoine" },
        { "PoiFilterTemple", "Temple" },
        { "PoiFilterMarket", "Marché" },
        { "PoiFilterPark", "Parc" },
        { "PoiFilterCulture", "Culture" },
        { "PoiEmptyTitle", "Aucun résultat" },
        { "PoiEmptyHint", "Essayez un filtre ou un mot-clé différent" },
        { "PoiPriorityHigh", "⭐ Haute priorité" },
        { "PoiPriorityFeatured", "📍 En vedette" },
        { "PoiPriorityDefault", "📍 Point d'intérêt" },
        { "PoiCategoryDefault", "📍 POINT D'INTÉRÊT" },
        { "PoiAudioRecorded", "🎙️ Audio enregistré" },
        { "PoiAudioTts", "🤖 Voix TTS" },
        { "PoiStatusAudioReady", "🎵 MP3 prêt" },
        { "PoiStatusTtsReady", "🗣️ TTS prêt" },
        { "AudioTts", "🎧 TTS" },
        { "QrHeader", "Scanner QR Code" },
        { "QrInstructions", "Pointez la caméra en direction du QR code" },
        { "QrScanHint", "Détection automatique du QR code" },
        { "QrSuccessTitle", "QR code reconnu!" },
        { "QrPlayButton", "Ouvrir et lire la narration" },
        { "QrScanAgain", "Scaner un autre code" },
        { "PoiDetailDescription", "Description" },
        // Camera errors
        { "CameraCheckingPermission", "Vérification de la permission caméra..." },
        { "CameraRequesting", "Demande de permission caméra..." },
        { "CameraPermissionDenied", "Permission caméra requise. Veuillez accorder la permission dans Paramètres." },
        { "CameraPermissionDeniedShort", "Permission caméra refusée" },
        { "CameraStartingFormat", "Démarrage de la caméra... ({0}/{1})" },
        { "CameraStartError", "Erreur de démarrage de la caméra. Veuillez réessayer." },
        { "CameraStartFailed", "Impossible de démarrer la caméra" },
        { "CameraErrorAfterRetry", "Erreur après {0} tentatives: {1}" },
        { "CameraRetry", "Réessayer" },
        { "CameraRetrying", "Nouvelle tentative..." },
        { "CameraInvalidQr", "QR code invalide. Nouvelle tentative de scan..." },
        { "SeeDetails", "Voir les détails" },
        { "PoiDetailAudio", "Narration audio" },
        { "GpsCoordinates", "Coordonnées GPS" },
        { "AudioStat", "audio" },
        { "WalkStat", "min à pied" },
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
