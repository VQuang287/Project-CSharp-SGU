using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace TourMap.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private const string SelectedLanguageKey = "selected_language";
    private static LocalizationService? _instance;
    public static LocalizationService Current => _instance ??= new LocalizationService();

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
                CultureInfo.CurrentUICulture = new CultureInfo(value == "zh" ? "zh-CN" : value);
                OnPropertyChanged("Item");
                OnPropertyChanged(nameof(CurrentLanguage));
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
    };

    private static readonly Dictionary<string, string> English = new()
    {
        { "AppTitle", "🎧 Audio Guide Tour" },
        { "AppSubtitle", "Vinh Khanh Food Street, District 4" },
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
    };

    private static readonly Dictionary<string, string> Chinese = new()
    {
        { "AppTitle", "🎧 语音导览" },
        { "AppSubtitle", "荣庆美食街，第四郡" },
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
