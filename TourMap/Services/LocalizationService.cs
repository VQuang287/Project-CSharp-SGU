using System.ComponentModel;
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
    public string CurrentLanguage
    {
        get => _currentLanguage;
        set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                Preferences.Default.Set(SelectedLanguageKey, value);
                
                // Set culture for entire app
                var culture = new CultureInfo(value == "zh" ? "zh-CN" : value);
                CultureInfo.CurrentCulture = culture;
                CultureInfo.CurrentUICulture = culture;
                
                // Notify all listeners
                OnPropertyChanged("Item");
                OnPropertyChanged(nameof(CurrentLanguage));
                LanguageChanged?.Invoke();
            }
        }
    }

    private LocalizationService()
    {
        var savedLanguage = Preferences.Default.Get<string>(SelectedLanguageKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(savedLanguage))
        {
            _currentLanguage = savedLanguage;
            return;
        }
        var sysLang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        if (sysLang == "en") _currentLanguage = "en";
        else if (sysLang == "zh") _currentLanguage = "zh";
        else if (sysLang == "ko") _currentLanguage = "ko";
        else if (sysLang == "ja") _currentLanguage = "ja";
        else if (sysLang == "fr") _currentLanguage = "fr";
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
        return dict.TryGetValue(key, out var text) ? text : (Vietnamese.TryGetValue(key, out var vi) ? vi : key);
    }

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
        { "QrBtn", "📷 Quét QR Code" },
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
        { "CacheSection", "💾 Audio Cache" },
        { "ClearCache", "🗑️ Xóa cache audio" },
        { "ClearCacheConfirmTitle", "Xóa cache?" },
        { "ClearCacheConfirmMsg", "Tất cả file audio đã tải sẽ bị xóa. Bạn cần đồng bộ lại." },
        { "ClearCacheOk", "Xóa" },
        { "ClearCacheCancel", "Hủy" },
        { "ClearCacheSuccess", "Đã xóa toàn bộ cache audio." },
        { "InfoSection", "ℹ️ Thông tin" },
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
        { "QrBtn", "📷 Scan QR Code" },
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
        { "CacheSection", "💾 Audio Cache" },
        { "ClearCache", "🗑️ Clear audio cache" },
        { "ClearCacheConfirmTitle", "Clear cache?" },
        { "ClearCacheConfirmMsg", "All downloaded audio files will be deleted." },
        { "ClearCacheOk", "Clear" },
        { "ClearCacheCancel", "Cancel" },
        { "ClearCacheSuccess", "All audio cache cleared." },
        { "InfoSection", "ℹ️ Info" },
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
        { "QrBtn", "📷 扫描二维码" },
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
        { "CacheSection", "💾 音频缓存" },
        { "ClearCache", "🗑️ 清除缓存" },
        { "ClearCacheConfirmTitle", "清除缓存？" },
        { "ClearCacheConfirmMsg", "所有已下载的音频文件将被删除。" },
        { "ClearCacheOk", "清除" },
        { "ClearCacheCancel", "取消" },
        { "ClearCacheSuccess", "缓存已清除。" },
        { "InfoSection", "ℹ️ 信息" },
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
        { "QrBtn", "📷 QR 코드 스캔" },
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
        { "CacheSection", "💾 오디오 캐시" },
        { "ClearCache", "🗑️ 캐시 지우기" },
        { "ClearCacheConfirmTitle", "캐시를 지울까요?" },
        { "ClearCacheConfirmMsg", "다운로드된 음성 파일이 모두 삭제됩니다." },
        { "ClearCacheOk", "삭제" },
        { "ClearCacheCancel", "취소" },
        { "ClearCacheSuccess", "캐시가 삭제되었습니다." },
        { "InfoSection", "ℹ️ 정보" },
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
        { "QrBtn", "📷 QRコードをスキャン" },
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
        { "CacheSection", "💾 音声キャッシュ" },
        { "ClearCache", "🗑️ キャッシュを削除" },
        { "ClearCacheConfirmTitle", "キャッシュを削除しますか？" },
        { "ClearCacheConfirmMsg", "ダウンロードされたすべての音声ファイルが削除されます。" },
        { "ClearCacheOk", "削除" },
        { "ClearCacheCancel", "キャンセル" },
        { "ClearCacheSuccess", "キャッシュが削除されました。" },
        { "InfoSection", "ℹ️ 情報" },
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
        { "QrBtn", "📷 Scanner QR Code" },
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
        { "CacheSection", "💾 Cache Audio" },
        { "ClearCache", "🗑️ Vider le cache" },
        { "ClearCacheConfirmTitle", "Vider le cache ?" },
        { "ClearCacheConfirmMsg", "Tous les fichiers audio téléchargés seront supprimés." },
        { "ClearCacheOk", "Supprimer" },
        { "ClearCacheCancel", "Annuler" },
        { "ClearCacheSuccess", "Cache audio vidé." },
        { "InfoSection", "ℹ️ Infos" },
    };

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
