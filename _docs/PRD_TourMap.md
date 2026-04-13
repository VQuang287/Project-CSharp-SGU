# TÀI LIỆU YÊU CẦU SẢN PHẨM (PRD)
## TourMap - Hệ thống Hướng dẫn Du lịch Thông minh

**Thời gian thực hiện:** 12 tuần  
**Nền tảng:** 
- Mobile: Android (.NET 10.0 MAUI)
- CMS Web: ASP.NET Core 10.0
- Database: SQLite (Mobile + Web)

---

## 1. TỔNG QUAN HỆ THỐNG

### 1.1 Giới thiệu

TourMap là **hệ thống hướng dẫn du lịch thông minh** bao gồm 3 thành phần:

1. **Mobile App (Android):** Ứng dụng cho du khách
2. **CMS Web Admin:** Hệ thống quản lý nội dung cho operator
3. **Analytics Dashboard:** Báo cáo và phân tích dữ liệu sử dụng

### 1.2 Mục tiêu đồ án

Xây dựng hệ thống hoàn chỉnh trong 12 tuần:

| Thành phần | Mục tiêu chính |
|------------|----------------|
| **Mobile App** | GPS tracking, Geofencing, Audio narration đa ngôn ngữ |
| **CMS Web** | Quản lý POI, nội dung đa ngôn ngữ, upload media |
| **Analytics** | Thống kê lượt nghe, heatmap, báo cáo |

### 1.3 Phạm vi dự án (12 tuần)

**Trong phạm vi (CORE - bắt buộc):**
- Mobile App Android (API 31+, .NET 10 MAUI)
- CMS Web Admin (ASP.NET Core 10, Bootstrap UI)
- Analytics Dashboard (đơn giản, chart cơ bản)
- Hỗ trợ 6 ngôn ngữ: vi, en, zh, ko, ja, fr
- Tối đa 10 POIs cho demo
- Database: SQLite (mobile) + SQLite (web đơn giản)
- **Không cần backend phức tạp:** Web CMS truy cập trực tiếp DB

**Ngoài phạm vi (FUTURE - nếu còn thời gian):**
- REST API riêng (Mobile ↔ Server)
- Cloud hosting (Azure/AWS)
- Push notification (Firebase)
- iOS version
- Real-time sync

---

## 2. KIẾN TRÚC HỆ THỐNG

### 2.1 Sơ đồ tổng quan

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         TOURMAP SYSTEM                                  │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                         │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │                    MOBILE APP (Android)                         │  │
│  │                    .NET 10 MAUI                               │  │
│  │                                                                 │  │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐       │  │
│  │  │  Map UI  │  │  Audio   │  │  Settings│  │   POI    │       │  │
│  │  │  (Mapsui)│  │  Player  │  │(Language)│  │  Detail  │       │  │
│  │  └──────────┘  └──────────┘  └──────────┘  └──────────┘       │  │
│  │                                                                 │  │
│  │  Services:                                                      │  │
│  │  - GPS Tracking (FusedLocationProvider)                        │  │
│  │  - GeofenceEngine (Haversine)                                  │  │
│  │  - NarrationEngine (TTS + MP3)                                 │  │
│  │  - LocalizationService (6 languages)                         │  │
│  │                                                                 │  │
│  │  Data: SQLite (local)                                          │  │
│  │  - 10 POIs × 6 languages = 60 audio files                     │  │
│  │                                                                 │  │
│  └───────────────────────────┬─────────────────────────────────────┘  │
│                              │                                          │
│                              │ Export/Import (JSON/Excel)              │
│                              │ (Manual sync - optional)                │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │                 CMS WEB ADMIN                                   │  │
│  │                 ASP.NET Core 10 + Bootstrap                     │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Dashboard (tổng quan)                                    │  │  │
│  │  │  - Số POI, số lượt nghe, ngôn ngữ phổ biến               │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  POI Management                                         │  │  │
│  │  │  - CRUD POI (tên, tọa độ, bán kính, ưu tiên)           │  │  │
│  │  │  - Multi-language content (6 ngôn ngữ)                   │  │  │
│  │  │  - Upload ảnh, audio files                              │  │  │
│  │  │  - TTS script editor                                    │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Language Management                                      │  │  │
│  │  │  - Quản lý 6 ngôn ngữ                                    │  │  │
│  │  │  - Kiểm tra completeness (thiếu content)                 │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  Database: SQLite (Shared với Analytics)                      │  │
│  │                                                                 │  │
│  └───────────────────────────┬─────────────────────────────────────┘  │
│                              │                                          │
│                              │ Truy vấn dữ liệu                        │
│                              ▼                                          │
│  ┌─────────────────────────────────────────────────────────────────┐  │
│  │                 ANALYTICS DASHBOARD                           │  │
│  │                 ASP.NET Core 10 + Chart.js                    │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Thống kê (Statistics)                                  │  │  │
│  │  │  - Top 10 POI được nghe nhiều nhất                     │  │  │
│  │  │  - Thời gian nghe trung bình                           │  │  │
│  │  │  - Số lượt nghe theo ngôn ngữ                          │  │  │
│  │  │  - Tổng số lượt truy cập                               │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Heatmap (Heatmap View)                                 │  │  │
│  │  │  - Hiển thị vị trí người dùng (ẩn danh)                 │  │  │
│  │  │  - Mật độ tập trung tại các POI                        │  │  │
│  │  │  - Bản đồ tương tác (Leaflet.js)                       │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  ┌─────────────────────────────────────────────────────────┐  │  │
│  │  │  Lịch sử (Playback Log)                                 │  │  │
│  │  │  - Danh sách các lần phát audio                        │  │  │
│  │  │  - Thời gian, POI, ngôn ngữ, hoàn thành               │  │  │
│  │  │  - Lọc theo ngày, POI, ngôn ngữ                        │  │  │
│  │  └─────────────────────────────────────────────────────────┘  │  │
│  │                                                                 │  │
│  │  Database: SQLite (Shared với CMS)                           │  │
│  │  - PlaybackHistory table                                      │  │
│  │  - UserLocationLogs table (heatmap)                          │  │
│  │                                                                 │  │
│  └─────────────────────────────────────────────────────────────────┘  │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.2 Luồng dữ liệu chính

```
┌─────────────┐     ┌─────────────┐     ┌─────────────┐
│  Operator   │────▶│  CMS Web    │────▶│  Database   │
│  (Admin)    │     │  Admin      │     │  (SQLite)   │
└─────────────┘     └─────────────┘     └──────┬──────┘
                                                 │
                           ┌─────────────────────┘
                           │
                           │ Export JSON / Manual Sync
                           │
                           ▼
                   ┌─────────────┐
                   │ Mobile App  │
                   │ (Tourist)   │
                   └──────┬──────┘
                          │
                          │ Ghi lại usage
                          │ (Playback events)
                          │
                          ▼
                   ┌─────────────┐     ┌─────────────┐
                   │  Database   │◀────│  Analytics  │
                   │  (Logs)     │     │  Dashboard  │
                   └─────────────┘     └─────────────┘
```

### 2.3 Công nghệ sử dụng

#### Mobile App
| Thành phần | Công nghệ |
|------------|-----------|
| Framework | .NET 10.0 MAUI |
| IDE | Visual Studio 2026 |
| Language | C# 13.0 |
| Android API | 31+ (Android 12+) |
| Database | SQLite + sqlite-net |
| Maps | Mapsui 5.0+ |
| Audio | Plugin.Maui.Audio 3.0+ |
| TTS | Android.Speech.Tts (6 locales) |
| Localization | .resx resources |

#### CMS Web + Analytics
| Thành phần | Công nghệ |
|------------|-----------|
| Framework | ASP.NET Core 10.0 |
| IDE | Visual Studio 2026 |
| Language | C# 13.0 |
| UI | Bootstrap 5 + AdminLTE (optional) |
| Database | SQLite (Entity Framework Core) |
| Charts | Chart.js |
| Maps | Leaflet.js (heatmap) |

---

## 3. MODULE 1: MOBILE APP (ANDROID)

### 3.1 Tổng quan

Ứng dụng Android cho du khách, cung cấp:
- GPS tracking và geofencing
- Audio narration đa ngôn ngữ (6 languages)
- Map hiển thị POIs
- Offline hoạt động

### 3.2 Core Features

#### M-001: Theo dõi vị trí GPS
- Cập nhật mỗi 5 giây (foreground), 10 giây (background)
- Độ chính xác 5-10m
- Yêu cầu Android API 31+ permissions

#### M-002: Geofencing (Phát hiện POI)
- Haversine formula tính khoảng cách
- Bán kính 30-50m mỗi POI
- Cooldown 10 phút chống spam

#### M-003: Audio Narration đa ngôn ngữ
- Ưu tiên: File MP3 theo ngôn ngữ hiện tại
- Fallback 1: TTS script
- Fallback 2: Tiếng Việt (default)
- Hỗ trợ 6 ngôn ngữ: vi, en, zh, ko, ja, fr

#### M-004: Map với POI Markers
- OpenStreetMap (Mapsui)
- Markers hiển thị tên theo ngôn ngữ hiện tại
- Tap để xem thông tin

#### M-005: Đổi ngôn ngữ runtime
- 6 ngôn ngữ trong Settings
- Đổi ngay không cần restart
- UI và audio đồng bộ

### 3.3 Data Model (Mobile)

```csharp
public class Poi
{
    [PrimaryKey]
    public string Id { get; set; }
    
    // Geographic (language-independent)
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public int Priority { get; set; }
    
    // Multi-language (JSON)
    public string TitleJson { get; set; }        // {"vi":"...","en":"...",...}
    public string DescriptionJson { get; set; }
    public string TtsScriptJson { get; set; }
    public string AudioPathJson { get; set; }    // {"vi":"/path/vi.mp3",...}
}
```

### 3.4 Cấu trúc thư mục

```
TourMap/
├── MauiProgram.cs
├── App.xaml.cs
├── Resources/
│   ├── Strings/
│   │   ├── Strings.resx (vi)
│   │   ├── Strings.en.resx
│   │   ├── Strings.zh.resx
│   │   ├── Strings.ko.resx
│   │   ├── Strings.ja.resx
│   │   └── Strings.fr.resx
│   └── Audio/
│       └── poi_001/ ... poi_010/
│           ├── audio_vi.mp3
│           ├── audio_en.mp3
│           └── ... (6 files per POI)
├── Models/
│   └── Poi.cs
├── Services/
│   ├── DatabaseService.cs
│   ├── GeofenceEngine.cs
│   ├── NarrationEngine.cs
│   ├── AudioPlayerService.cs
│   ├── LocalizationService.cs
│   └── TtsService_Android.cs
├── Pages/
│   ├── MapPage.xaml
│   ├── PoiDetailPage.xaml
│   └── SettingsPage.xaml
└── Platforms/Android/
    ├── GpsTrackingService_Android.cs
    └── AndroidManifest.xml (API 31+ permissions)
```

---

## 4. MODULE 2: CMS WEB ADMIN

### 4.1 Tổng quan

Hệ thống quản lý nội dung cho operator, cho phép:
- Quản lý POI (CRUD)
- Quản lý nội dung đa ngôn ngữ
- Upload media (ảnh, audio)
- Kiểm tra completeness

### 4.2 Core Features

#### W-001: Dashboard tổng quan
- Số lượng POI
- Số ngôn ngữ đang hỗ trợ
- Status completeness (mỗi POI đủ 6 ngôn ngữ chưa)
- Link nhanh đến các chức năng

#### W-002: Quản lý POI (CRUD)
**List View:**
- Bảng danh sách POI
- Filter: Tất cả / Thiếu content / Theo ngôn ngữ
- Search theo tên
- Actions: Edit, Delete, View

**Create/Edit POI:**
- Tab 1: Thông tin cơ bản
  - Tên (multi-language input)
  - Tọa độ (Lat, Lng)
  - Bán kính geofence (m)
  - Mức độ ưu tiên (1-10)
  - Ảnh đại diện (upload)

- Tab 2: Nội dung đa ngôn ngữ
  - 6 sections cho 6 ngôn ngữ
  - Mỗi section: Description, TTS Script
  - Upload audio file cho từng ngôn ngữ
  - Preview TTS (button đọc thử)

#### W-003: Language Management
- Danh sách 6 ngôn ngữ
- Kiểm tra completeness per POI
- Hiển thị warning nếu thiếu content
- Export data theo ngôn ngữ

#### W-004: Media Upload
- Upload ảnh POI (JPG/PNG, max 2MB)
- Upload audio (MP3, max 5MB per file)
- Drag & drop (optional, nice to have)
- Progress indicator

#### W-005: Export dữ liệu
- Export POI data ra JSON
- Export ra Excel (CSV)
- Dùng để import vào Mobile App (manual sync)

### 4.3 Data Model (Web)

```csharp
public class Poi
{
    public string Id { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int RadiusMeters { get; set; }
    public int Priority { get; set; }
    public string ImageUrl { get; set; }
    
    // Navigation property
    public List<PoiTranslation> Translations { get; set; }
}

public class PoiTranslation
{
    public int Id { get; set; }
    public string PoiId { get; set; }
    public string LanguageCode { get; set; }  // vi, en, zh, ko, ja, fr
    public string Title { get; set; }
    public string Description { get; set; }
    public string TtsScript { get; set; }
    public string AudioUrl { get; set; }
}
```

### 4.4 Cấu trúc thư mục

```
TourMap.AdminWeb/
├── Program.cs
├── appsettings.json
├── Controllers/
│   ├── HomeController.cs (Dashboard)
│   ├── PoisController.cs (CRUD)
│   └── AnalyticsController.cs (Redirect)
├── Models/
│   ├── Poi.cs
│   ├── PoiTranslation.cs
│   └── AdminDbContext.cs
├── Views/
│   ├── Home/Index.cshtml (Dashboard)
│   ├── Pois/Index.cshtml (List)
│   ├── Pois/Create.cshtml
│   ├── Pois/Edit.cshtml
│   └── Shared/_Layout.cshtml
├── wwwroot/
│   ├── uploads/pois/ (images)
│   ├── uploads/audio/ (mp3 files)
│   └── css/bootstrap/
└── AdminTourMap.db (SQLite)
```

### 4.5 Giao diện mockup

**Dashboard:**
```
┌─────────────────────────────────────────┐
│  TourMap Admin Dashboard                │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │ 10 POIs │ │ 6 Lang  │ │ 85%     │    │
│  │         │ │         │ │Complete │    │
│  └─────────┘ └─────────┘ └─────────┘    │
│                                         │
│  Quick Actions:                          │
│  [Quản lý POI] [Xem Analytics]          │
│                                         │
│  POIs thiếu content:                    │
│  • POI #3 - thiếu: zh, ko               │
│  • POI #7 - thiếu: ja, fr               │
│                                         │
└─────────────────────────────────────────┘
```

**POI Edit Page:**
```
┌─────────────────────────────────────────┐
│  Edit POI: Ngã 4 Hoàng Diệu            │
├─────────────────────────────────────────┤
│  [Tab: Info] [Tab: Content]             │
│                                         │
│  Tab Info:                              │
│  - Lat/Lng: [10.761889] [106.702003]    │
│  - Radius: [30] meters                  │
│  - Priority: [5]                        │
│  - Image: [Upload] [Preview]            │
│                                         │
│  Tab Content (6 languages):           │
│  [vi] [en] [zh] [ko] [ja] [fr]         │
│                                         │
│  Tab vi:                                │
│  - Title: [Ngã 4 Hoàng Diệu]            │
│  - Description: [Textarea]              │
│  - TTS Script: [Textarea]               │
│  - Audio: [Upload audio_vi.mp3] [Play]  │
│                                         │
│  [Save] [Cancel]                        │
└─────────────────────────────────────────┘
```

---

## 5. MODULE 3: ANALYTICS DASHBOARD

### 5.1 Tổng quan

Hệ thống báo cáo và phân tích dữ liệu sử dụng, bao gồm:
- Thống kê lượt nghe
- Heatmap vị trí người dùng
- Lịch sử phát audio

### 5.2 Core Features

#### A-001: Thống kê tổng quan (Statistics)

**Cards:**
- Tổng số lượt nghe (Play count)
- Số người dùng duy nhất (Unique users)
- Thời gian nghe trung bình (Avg duration)
- Top ngôn ngữ được sử dụng

**Charts:**
- Bar chart: Top 10 POI được nghe nhiều nhất
- Pie chart: Phân bố theo ngôn ngữ
- Line chart: Lượt nghe theo thời gian (7 ngày/30 ngày)

#### A-002: Heatmap (Bản đồ nhiệt)

- Hiển thị vị trí người dùng (ẩn danh)
- Mật độ tập trung tại các POI
- Tương tác: Zoom, pan bản đồ
- Lọc theo thời gian (hôm nay, 7 ngày, 30 ngày)
- **Lưu ý:** Dữ liệu ẩn danh, không lưu thông tin cá nhân

#### A-003: Lịch sử phát audio (Playback Log)

**List View:**
- Bảng các lần phát audio
- Cột: Thời gian, POI, Ngôn ngữ, Hoàn thành, Trigger type
- Filter: Theo ngày, POI, ngôn ngữ
- Export ra CSV

**Chi tiết mỗi lần phát:**
- Timestamp
- POI ID + Name
- Language
- Duration (seconds)
- IsCompleted (đã nghe hết chưa)
- TriggerType: AUTO (geofence) / MANUAL (user tap)

#### A-004: Báo cáo (Reports)

**Báo cáo đơn giản:**
- Báo cáo theo ngày (Daily report)
- Báo cáo theo POI (POI performance)
- Export PDF (nếu có thời gian, optional)

### 5.3 Data Model (Analytics)

```csharp
// Lưu lại mỗi lần phát audio
public class PlaybackHistory
{
    public int Id { get; set; }
    public string PoiId { get; set; }
    public string LanguageCode { get; set; }
    public DateTime PlayedAt { get; set; }
    public int DurationSeconds { get; set; }
    public bool IsCompleted { get; set; }
    public string TriggerType { get; set; }  // AUTO, MANUAL
    public string AnonymousUserId { get; set; }  // Device ID (ẩn danh)
}

// Lưu vị trí người dùng (cho heatmap)
public class UserLocationLog
{
    public int Id { get; set; }
    public string AnonymousUserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime RecordedAt { get; set; }
}
```

### 5.4 Cấu trúc thư mục

Analytics có thể là:
- **Option 1:** Tách project riêng (TourMap.Analytics)
- **Option 2:** Chung project với CMS (Views/Analytics folder)

**Khuyến nghị (đơn giản):** Chung project với CMS

```
TourMap.AdminWeb/
├── Controllers/
│   └── AnalyticsController.cs
├── ViewModels/
│   ├── DashboardViewModel.cs
│   ├── HeatmapViewModel.cs
│   └── PlaybackLogViewModel.cs
├── Views/
│   └── Analytics/
│       ├── Index.cshtml (Dashboard)
│       ├── Heatmap.cshtml
│       └── PlaybackLog.cshtml
└── wwwroot/
    └── js/
        ├── chart.js (Chart.js library)
        └── heatmap.js (Leaflet + heatmap plugin)
```

### 5.5 Giao diện mockup

**Analytics Dashboard:**
```
┌─────────────────────────────────────────┐
│  Analytics Dashboard                      │
├─────────────────────────────────────────┤
│                                         │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐    │
│  │ 1,250   │ │ 45s     │ │ vi: 60% │    │
│  │ Plays   │ │ Avg     │ │ en: 25% │    │
│  │         │ │ Duration│ │ zh: 10% │    │
│  └─────────┘ └─────────┘ └─────────┘    │
│                                         │
│  [Top POIs Chart - Bar]                 │
│  ┌─────────────────────────────────┐    │
│  │  ████████████                   │    │
│  │  ████████████████                 │    │
│  │  ██████████                       │    │
│  └─────────────────────────────────┘    │
│                                         │
│  [Quick Links]                           │
│  [View Heatmap] [Playback Log]          │
│                                         │
└─────────────────────────────────────────┘
```

**Heatmap Page:**
```
┌─────────────────────────────────────────┐
│  Heatmap - User Locations               │
├─────────────────────────────────────────┤
│                                         │
│  Filter: [Today ▼] [POI: All ▼]         │
│                                         │
│  ┌─────────────────────────────────┐    │
│  │                                 │    │
│  │     🔥         🔥              │    │
│  │    (heatmap    (heatmap        │    │
│  │    overlay)    overlay)       │    │
│  │                                 │    │
│  │           🔥                    │    │
│  │          (POI 3)               │    │
│  │                                 │    │
│  │  Leaflet.js Map                 │    │
│  │                                 │    │
│  └─────────────────────────────────┘    │
│                                         │
│  Legend: 🔥 Cao | 🟡 Trung bình | 🟢 Thấp│
│                                         │
└─────────────────────────────────────────┘
```

---

## 6. HỆ THỐNG ĐA NGÔN NGỮ (LOCALIZATION)

### 6.1 Tổng quan

Hệ thống hỗ trợ **6 ngôn ngữ**:
1. Tiếng Việt (vi) - Default
2. Tiếng Anh (en)
3. Tiếng Trung (zh)
4. Tiếng Hàn (ko)
5. Tiếng Nhật (ja)
6. Tiếng Pháp (fr)

### 6.2 Phân loại ngôn ngữ

| Loại | Mobile | CMS Web | Analytics |
|------|--------|---------|-----------|
| UI Language | 6 ngôn ngữ | Tiếng Việt (chính) | Tiếng Việt (chính) |
| Content Language | 6 ngôn ngữ | 6 ngôn ngữ | Hiển thị % |
| TTS Language | 6 locales | Preview TTS | - |

### 6.3 Implementation

**Mobile:**
- .resx files cho UI strings
- LocalizationService singleton
- JSON columns trong SQLite cho POI content
- Android TTS với 6 locales

**CMS Web:**
- Models: Poi + PoiTranslation (EF Core)
- UI: Vietnamese cho Admin
- Content: 6 ngôn ngữ cho POI

---

## 7. YÊU CẦU PHI CHỨC NĂNG

### 7.1 Hiệu năng

| Yêu cầu | Mobile | CMS Web |
|---------|--------|---------|
| Khởi động | < 3 giây | < 2 giây |
| Load map | < 2 giây | N/A |
| Phát audio | < 1 giây | N/A |
| CRUD POI | N/A | < 1 giây |
| Load analytics | N/A | < 3 giây |
| Heatmap | N/A | < 5 giây |

### 7.2 Bảo mật & Privacy

- **Không lưu PII:** Chỉ lưu AnonymousUserId (device hash)
- **Location data:** Chỉ dùng cho analytics, ẩn danh
- **Permissions:** Xin runtime permission đúng cách (API 31+)
- **Admin auth:** Simple cookie auth (đủ cho đồ án)

### 7.3 Tương thích

- Mobile: Android 12+ (API 31+)
- Web: Chrome, Firefox, Edge, Safari

---

## 8. LỘ TRÌNH THỰC HIỆN (12 TUẦN)

### Tuần 1–2: Setup + Architecture

**Tuần 1:**
- [ ] Cài đặt Visual Studio 2026
- [ ] Tạo solution với 2 projects:
  - TourMap (MAUI Android)
  - TourMap.AdminWeb (ASP.NET Core)
- [ ] Setup Git repository
- [ ] Cấu hình Android SDK API 31+
- [ ] Research: MAUI Localization, ASP.NET Core CRUD

**Tuần 2:**
- [ ] Thiết kế Database Schema (SQLite)
- [ ] Tạo Models (POI, Translation, Analytics)
- [ ] Setup Entity Framework Core
- [ ] Seed data mẫu (2-3 POIs)
- [ ] Viết Architecture Document

**Deliverable:** Project structure hoàn chỉnh, chạy được

---

### Tuần 3–5: Mobile Core

**Tuần 3: Localization Framework**
- [ ] Tạo 6 files .resx
- [ ] Implement LocalizationService
- [ ] Settings page đổi ngôn ngữ
- [ ] Test đổi ngôn ngữ runtime

**Tuần 4: Database + GPS**
- [ ] DatabaseService (SQLite)
- [ ] Poi model với JSON columns
- [ ] GPS Tracking (FusedLocation)
- [ ] Test lấy vị trí 5s interval

**Tuần 5: Geofencing + Audio**
- [ ] GeofenceEngine (Haversine)
- [ ] AudioPlayerService
- [ ] TtsService_Android (6 locales)
- [ ] Test phát audio đúng ngôn ngữ

**Deliverable:** Mobile app có thể phát audio khi vào vùng POI

---

### Tuần 6–8: CMS Web + Content

**Tuần 6: CMS Backend**
- [ ] Setup ASP.NET Core project
- [ ] EF Core + SQLite
- [ ] Controllers: Home, Pois
- [ ] Views: Layout, Dashboard

**Tuần 7: CMS Frontend - POI CRUD**
- [ ] POI List page
- [ ] POI Create page
- [ ] POI Edit page (2 tabs: Info, Content)
- [ ] Multi-language input forms

**Tuần 8: CMS - Media Upload**
- [ ] Image upload
- [ ] Audio upload (6 files per POI)
- [ ] Export JSON/CSV
- [ ] Dashboard completeness check

**Deliverable:** CMS Web hoàn chỉnh, quản lý được POI đa ngôn ngữ

---

### Tuần 9–10: Analytics + Map

**Tuần 9: Analytics Backend**
- [ ] PlaybackHistory logging
- [ ] UserLocationLog (cho heatmap)
- [ ] AnalyticsController
- [ ] ViewModels cho charts

**Tuần 10: Analytics Frontend + Mobile Map**
- [ ] Chart.js integration
- [ ] Top POIs bar chart
- [ ] Language pie chart
- [ ] Heatmap (Leaflet.js)
- [ ] Mobile: Mapsui integration

**Deliverable:** Analytics dashboard hoạt động, Mobile hiển thị map

---

### Tuần 11: Integration + Testing

**Tuần 11:**
- [ ] Export từ CMS → Import vào Mobile (manual)
- [ ] Test end-to-end workflow
- [ ] Bug fixing
- [ ] UI Polish (Mobile + Web)
- [ ] Performance optimization

**Deliverable:** Hệ thống hoàn chỉnh, sẵn sàng demo

---

### Tuần 12: Final + Documentation

**Tuần 12:**
- [ ] Viết báo cáo đồ án:
  - Giới thiệu hệ thống
  - Thiết kế kiến trúc
  - Thiết kế database
  - Hướng dẫn cài đặt
  - Demo video script
- [ ] Tạo video demo (5-7 phút):
  - Demo Mobile App
  - Demo CMS Web
  - Demo Analytics
- [ ] Chuẩn bị slide thuyết trình
- [ ] Nộp source code + báo cáo

**Deliverable:** Đồ án hoàn chỉnh, sẵn sàng bảo vệ

---

## 9. RỦI RO VÀ GIẢI PHÁP

### 9.1 Rủi ro kỹ thuật

| Rủi ro | Giải pháp |
|--------|-----------|
| Scope quá lớn (3 modules) | Ưu tiên Mobile trước, CMS cơ bản, Analytics đơn giản |
| Android 12+ permission phức tạp | Research sớm tuần 1-2, test trên device thật |
| Multi-language phức tạp | Dùng JSON columns (Mobile), EF relationships (Web) |
| Thiếu thời gian | Tuần 11 buffer cho bug fix, Analytics có thể đơn giản hóa |

### 9.2 Rủi ro tiến độ

| Rủi ro | Giải pháp |
|--------|-----------|
| Không hiểu rõ ASP.NET Core | Dùng MVC template có sẵn, không làm quá phức tạp |
| Heatmap khó implement | Dùng Leaflet.js đơn giản, mock data nếu cần |
| Sync Mobile ↔ Web phức tạp | Dùng manual export/import (JSON), không cần real-time API |

---

## 10. TÀI LIỆU THAM KHẢO

### 10.1 Tài liệu kỹ thuật
- .NET 10 MAUI: https://learn.microsoft.com/en-us/dotnet/maui/
- ASP.NET Core 10: https://learn.microsoft.com/en-us/aspnet/core/
- Entity Framework Core: https://learn.microsoft.com/en-us/ef/core/
- Bootstrap 5: https://getbootstrap.com/docs/5.3/
- Chart.js: https://www.chartjs.org/
- Leaflet.js: https://leafletjs.com/

### 10.2 Công cụ
- Visual Studio 2026 Community
- Android Studio (SDK Manager)
- Git/GitHub
- SQLite Browser (DB管理)

### 10.3 Assets cần chuẩn bị
- 60 file MP3 (10 POIs × 6 languages)
- 10 hình ảnh POI
- App icon, Splash screen

---

## PHỤ LỤC

### A. Glossary

| Thuật ngữ | Giải thích |
|-----------|------------|
| CMS | Content Management System - Hệ thống quản lý nội dung |
| Analytics | Phân tích dữ liệu, báo cáo thống kê |
| Heatmap | Bản đồ nhiệt, hiển thị mật độ dữ liệu |
| POI | Point of Interest - Điểm tham quan |
| CRUD | Create, Read, Update, Delete - Thao tác dữ liệu cơ bản |
| TTS | Text-to-Speech - Chuyển văn bản thành giọng nói |
| EF Core | Entity Framework Core - ORM cho .NET |
| PII | Personally Identifiable Information - Thông tin cá nhân |

### B. Checklist trước khi nộp

**Source Code:**
- [ ] TourMap (MAUI) build được
- [ ] TourMap.AdminWeb chạy được
- [ ] Database migrations hoạt động

**Documents:**
- [ ] Báo cáo PDF (theo template trường)
- [ ] Video demo (YouTube link hoặc MP4)
- [ ] Slide thuyết trình
- [ ] README.md (hướng dẫn cài đặt)

**Demo Data:**
- [ ] 10 POIs complete (6 languages)
- [ ] 60 audio files
- [ ] Analytics data (mock hoặc real)

---

**End of Document**
