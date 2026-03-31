# 🎵 AUDIO TOUR APP

## Product Requirements Document (PRD)

| | |
|---|---|
| **Phiên bản** | 2.0.0 |
| **Nền tảng** | .NET MAUI 8.0 (Android / iOS) |
| **Ngày** | Tháng 3, 2026 |
| **Môn học** | Đồ án Lập trình Mobile |
| **Sinh viên** | Nhóm Audio Tour |
| **Trạng thái** | Bản cập nhật v2.0 — Bổ sung Offline Sync, Settings, Tour/QR Management, NFR, Deployment |

---

## MỤC LỤC

1. [Tổng quan Dự án](#1-tổng-quan-dự-án)
2. [Kiến trúc Hệ thống Tổng quan](#2-kiến-trúc-hệ-thống-tổng-quan)
3. [Geofence Engine & Narration Engine](#3-geofence-engine--narration-engine)
4. [Hệ thống App Người dùng (Mobile App)](#4-hệ-thống-app-người-dùng-mobile-app)
5. [Chiến lược Đồng bộ & Offline-First](#5-chiến-lược-đồng-bộ--offline-first) *(MỚI)*
6. [Hệ thống Admin — CMS Web](#6-hệ-thống-admin--cms-web)
7. [Thiết kế Cơ sở Dữ liệu](#7-thiết-kế-cơ-sở-dữ-liệu)
8. [Thiết kế REST API](#8-thiết-kế-rest-api)
9. [Yêu cầu Phi chức năng](#9-yêu-cầu-phi-chức-năng)
10. [Kiến trúc Triển khai & Môi trường](#10-kiến-trúc-triển-khai--môi-trường) *(MỚI)*
11. [Kế hoạch Triển khai & Lộ trình](#11-kế-hoạch-triển-khai--lộ-trình)
12. [Rủi ro & Giải pháp Dự phòng](#12-rủi-ro--giải-pháp-dự-phòng)
13. [Phụ lục](#13-phụ-lục)

---

## 1. TỔNG QUAN DỰ ÁN

### 1.1 Mô tả sản phẩm

Audio Tour App là ứng dụng di động hướng dẫn thuyết minh tự động cho khách tham quan, du lịch. Dựa trên vị trí GPS của người dùng, ứng dụng tự động kích hoạt nội dung âm thanh (TTS hoặc file thu sẵn) khi người dùng tiếp cận các điểm tham quan (Point of Interest - POI) được định nghĩa sẵn.

Ứng dụng hướng tới việc số hóa trải nghiệm tham quan, thay thế tour guide truyền thống bằng công nghệ định vị và âm thanh hiện đại, phù hợp với nhu cầu du lịch tự do và các tuyến di sản văn hóa đô thị.

### 1.2 Mục tiêu

- Cung cấp trải nghiệm thuyết minh tự động, không cần kết nối mạng (offline-first)
- Hỗ trợ đa ngôn ngữ (Tiếng Việt, Tiếng Anh, tiếng khác qua TTS)
- Tích hợp QR code để kích hoạt nội dung tại các điểm dừng xe buýt, bảng thông tin
- Cung cấp hệ thống CMS cho Admin quản lý nội dung linh hoạt
- Thu thập dữ liệu phân tích ẩn danh để cải thiện trải nghiệm

### 1.3 Phạm vi & Đối tượng

| Hạng mục | Chi tiết |
|---|---|
| Đối tượng chính | Du khách tham quan khu di sản, phường Khánh Hội, TP.HCM |
| Platform | Android (API 26+) & iOS (14+) — .NET MAUI 8.0 |
| Backend | ASP.NET Core Web API + SQLite/SQL Server + File Storage |
| Admin CMS | ASP.NET Core MVC — Quản lý POI, Audio, Analytics |
| Địa lý PoC | Các phường Khánh Hội, Quận 4, TP.HCM (giai đoạn đầu) |

---

## 2. KIẾN TRÚC HỆ THỐNG TỔNG QUAN

### 2.1 Sơ đồ kiến trúc tổng quát

Hệ thống gồm 3 thành phần chính: **Mobile App** (người dùng cuối), **Backend API**, và **Admin Web CMS**.

```mermaid
graph TB
    subgraph MOBILE["📱 MOBILE APP (.NET MAUI)"]
        direction TB
        UI["UI Layer<br/>Map View | POI List | Settings | QR Scanner"]
        VM["ViewModel Layer<br/>MapViewModel | POIViewModel | AudioViewModel"]
        
        subgraph ENGINES["⚙️ Core Engines"]
            GEO["Geofence Engine<br/>Haversine Distance Calculator<br/>Priority Resolver"]
            NAR["Narration Engine<br/>Audio Queue Manager<br/>Debounce & Cooldown"]
        end
        
        subgraph SERVICES["🔧 Platform Services"]
            GPS["GPS Service<br/>FusedLocationProvider<br/>Foreground Service"]
            TTS["TTS Service<br/>Android TextToSpeech<br/>Azure Cognitive"]
            AUDIO["Audio Player<br/>MediaPlayer / Plugin"]
            SYNC["Sync Service<br/>Delta Sync & Cache"]
        end
        
        DB_LOCAL["💾 SQLite<br/>POI Cache | Audio Files<br/>Playback History"]
    end
    
    subgraph BACKEND["🖥️ BACKEND SERVER (.NET Core)"]
        direction TB
        API["REST API<br/>SyncController | AuthController"]
        CMS["Admin CMS Web<br/>ASP.NET Core MVC"]
        
        subgraph BK_SERVICES["Backend Services"]
            TTS_API["AI TTS Generator<br/>Google/Azure Speech API"]
            ANALYTICS["Analytics Engine<br/>Log Processor | Heatmap"]
            AUTH["Auth Service<br/>JWT + Role-based Access"]
        end
        
        DB_SERVER["🗄️ SQL Server / SQLite<br/>POI | Translations | Users<br/>Tours | Analytics Logs"]
        STORAGE["📁 Object Storage<br/>Audio Files (MP3)<br/>POI Images"]
    end
    
    subgraph EXTERNAL["☁️ External Services"]
        MAPS_SDK["Google Maps SDK<br/>/ Mapbox"]
        CLOUD_TTS["Cloud TTS API<br/>Google / Azure"]
        CDN["CDN<br/>Static Assets"]
    end
    
    UI --> VM
    VM --> ENGINES
    VM --> SERVICES
    ENGINES --> GPS
    NAR --> TTS
    NAR --> AUDIO
    SERVICES --> DB_LOCAL
    SYNC --> API
    
    CMS --> DB_SERVER
    CMS --> TTS_API
    TTS_API --> CLOUD_TTS
    API --> DB_SERVER
    ANALYTICS --> DB_SERVER
    
    CMS --> STORAGE
    STORAGE --> CDN
    
    GPS -.->|"Location Data"| MAPS_SDK
    SYNC -.->|"Fetch POI data"| API
    SYNC -.->|"Download audio"| CDN
```

### 2.2 Các thành phần kiến trúc

| Thành phần | Công nghệ | Mô tả |
|---|---|---|
| Mobile App | .NET MAUI 8.0 | Ứng dụng Android/iOS — GPS, Geofence, TTS, Map, QR |
| GPS Service | FusedLocationProvider / MAUI Essentials | Theo dõi vị trí foreground + background service |
| Geofence Engine | Custom C# + Haversine | Xác định POI trong bán kính, debounce & cooldown |
| Narration Engine | Android TTS / Azure Cognitive | Quản lý hàng đợi audio, chọn TTS/file, chống lặp |
| Local DB | SQLite + EF Core | Lưu POI offline, log phát, cache settings |
| Backend API | ASP.NET Core 8 | REST API — CRUD POI, Analytics, Auth, Sync |
| Database | SQL Server / SQLite | Lưu trữ POI, audio metadata, play logs, users |
| File Storage | Local / Azure Blob / CDN | Lưu file audio .mp3/.m4a, ảnh POI |
| Admin CMS | ASP.NET Core MVC | Quản lý POI, audio, analytics, cài đặt hệ thống |

### 2.3 Luồng hoạt động tổng thể

```mermaid
flowchart TD
    A["Khởi động App"] --> B["Tải danh sách POI từ Server / SQLite Offline"]
    B --> C["Hiển thị Bản đồ + Vị trí người dùng"]
    C --> D["Background GPS Service Chạy"]
    D --> E["Geofence Engine kiểm tra khoảng cách Haversine"]
    E --> F{"Vào vùng POI?"}
    F -->|Không| G["Không làm gì — tiếp tục theo dõi"]
    F -->|Có| H["Narration Engine"]
    H --> I{"Đã phát gần đây?<br/>Cooldown OK?"}
    I -->|Đã phát| J["Skip — chống spam"]
    I -->|Chưa phát| K["Phát TTS hoặc Audio File"]
    K --> L["Ghi log → SQLite"]
    L --> M["Đồng bộ Analytics → Server"]
    G --> D
```

---

## 3. GEOFENCE ENGINE & NARRATION ENGINE

### 3.1 Geofence Engine

Geofence Engine là trái tim của ứng dụng — liên tục so sánh vị trí người dùng với danh sách POI để quyết định khi nào cần phát nội dung.

```mermaid
flowchart TD
    A["User GPS Position (lat, lng)"] --> B["Lọc POI trong vùng R_max = 500m"]
    B --> C["Tính khoảng cách Haversine<br/>d = haversine(user, poi)"]
    C --> D{"d < radius kích hoạt?"}
    D -->|Không| E["Không trigger — bỏ qua"]
    D -->|Có| F{"Cooldown OK?<br/>(debounce 30s)"}
    F -->|Không| G["Bỏ qua — chống spam"]
    F -->|Có| H["Priority check<br/>(POI ưu tiên cao nhất trong vùng)"]
    H --> I["Gửi Event → Narration Engine"]
```

**Thông số kỹ thuật Geofence:**

| Tham số | Giá trị mặc định | Mô tả |
|---|---|---|
| GPS Update Interval | 5 giây (foreground) | Tần suất cập nhật vị trí khi app ở foreground |
| GPS Update Interval (BG) | 15 giây (background) | Tần suất cập nhật khi app ở background — tiết kiệm pin |
| GPS Accuracy | Medium (100m) | Độ chính xác GPS — cân bằng pin vs chính xác |
| Min Trigger Radius | 20m | Bán kính tối thiểu để trigger |
| Default POI Radius | 50–200m | Bán kính mặc định của POI (admin cấu hình) |
| Debounce Time | 30 giây | Thời gian chờ tối thiểu sau lần trigger đầu tiên |
| Cooldown Per POI | 10 phút | Không phát lại cùng 1 POI trong vòng 10 phút |
| Max POIs in Range | Top 3 | Chỉ xử lý 3 POI gần nhất/ưu tiên cao nhất |
| Distance Formula | Haversine | Tính khoảng cách theo đường cong bề mặt trái đất |

### 3.2 Narration Engine

Narration Engine quản lý toàn bộ vòng đời của audio: nhận sự kiện từ Geofence, quyết định phát TTS hay audio file, quản lý hàng đợi, và ghi log.

```mermaid
sequenceDiagram
    participant Geo as 🎯 Geofence Engine
    participant Nar as 🎧 Narration Engine
    participant Queue as 📋 Audio Queue
    participant Player as 🔊 Audio Player
    participant Log as 📊 Logger

    Geo->>Nar: Event OnPOIEntered(poi_id)
    Nar->>Nar: Kiểm tra trạng thái (IDLE / PLAYING / COOLDOWN)
    
    alt IDLE & Chưa phát gần đây
        Nar->>Nar: Kiểm tra nguồn audio
        alt Có file audio sẵn (.mp3)
            Nar->>Queue: Thêm audio file vào queue
        else Không có file → dùng TTS
            Nar->>Queue: Tạo TTS từ script & thêm vào queue
        end
        Queue->>Player: Dequeue & Play
        Player->>Player: Pause nếu có notification/cuộc gọi
        Player->>Log: Ghi log (poi_id, timestamp, duration, trigger_type)
        Player->>Nar: OnComplete → chuyển COOLDOWN (10 phút)
    else Đang PLAYING
        alt POI mới priority > POI đang phát
            Nar->>Player: Stop current
            Nar->>Queue: Thêm POI mới vào đầu queue
        else POI mới priority thấp hơn
            Nar->>Queue: Thêm vào cuối queue (đợi)
        end
    else COOLDOWN
        Nar->>Nar: Bỏ qua (chống spam)
    end
```

**Trạng thái Narration Engine:**

```mermaid
stateDiagram-v2
    direction LR
    
    [*] --> IDLE : Khởi động Narration System
    
    IDLE --> PREPARE_AUDIO : Tín hiệu từ Geofence / QR Scan
    
    PREPARE_AUDIO --> PLAYING : Sẵn sàng phát (Stream/TTS Done)
    PREPARE_AUDIO --> IDLE : Lỗi hoặc Băng thông mạng kém
    
    PLAYING --> PAUSED : Người dùng tạm dừng / Nhận cuộc gọi
    PAUSED --> PLAYING : Hết cuộc gọi / Bấm tiếp tục
    
    PLAYING --> COOLDOWN : Phát hết toàn bộ kịch bản
    PLAYING --> IDLE : Người dùng tắt thủ công
    
    COOLDOWN --> IDLE : Quá 10 phút (cho phép phát lại)
```

**Ưu tiên phát audio:**

| Ưu tiên | Điều kiện | Hành động |
|---|---|---|
| 1 (Cao) | QR code scan | Phát ngay lập tức, ngắt queue hiện tại |
| 2 | POI priority cao + đi vào vùng | Thêm vào đầu hàng đợi |
| 3 | POI thường + đi vào vùng | Thêm vào cuối hàng đợi |
| 4 (Thấp) | User nhấn thủ công | Phát ngay (override queue) |

---

## 4. HỆ THỐNG APP NGƯỜI DÙNG (MOBILE APP)

### 4.1 Sơ đồ chi tiết Mobile App

```mermaid
graph TB
    subgraph APP_LIFECYCLE["📱 App Lifecycle"]
        SPLASH["Splash Screen"] --> LANG_CHECK{"Lần đầu?"}
        LANG_CHECK -->|Có| LANG_SELECT["Chọn Ngôn ngữ"]
        LANG_CHECK -->|Không| AUTH_CHECK{"Token hợp lệ?"}
        LANG_SELECT --> AUTH_CHECK
        AUTH_CHECK -->|Có| SYNC_POI["Fast Sync POI"]
        AUTH_CHECK -->|Không| LOGIN["Đăng nhập / Đăng ký"]
        LOGIN --> SYNC_POI
        SYNC_POI --> SETUP_GEO["Khởi tạo Geofence"]
        SETUP_GEO --> READY["✅ App Ready"]
    end
    
    subgraph MAIN_FEATURES["🎯 Các tính năng chính"]
        MAP_VIEW["🗺️ Map View<br/>- Hiển thị vị trí user<br/>- Hiển thị POI markers<br/>- Highlight POI gần nhất<br/>- Xem chi tiết POI"]
        
        POI_LIST["📋 POI List<br/>- Danh sách điểm tham quan<br/>- Tìm kiếm & lọc<br/>- Xem mô tả, ảnh, audio"]
        
        QR_SCAN["📷 QR Scanner<br/>- Quét mã tại trạm bus<br/>- Bypass geofence<br/>- Phát audio ngay"]
        
        SETTINGS["⚙️ Cài đặt<br/>- Chọn ngôn ngữ<br/>- Độ nhạy GPS / Bán kính<br/>- Chọn giọng TTS<br/>- Tải gói offline"]
    end
    
    subgraph BG_SERVICE["🔄 Background Processing"]
        GPS_BG["GPS Background Service"]
        GEO_ENGINE["Geofence Engine"]
        NAR_ENGINE["Narration Engine"]
        EVENT_BUS["App Event Bus"]
    end
    
    READY --> MAIN_FEATURES
    READY --> BG_SERVICE
    
    GPS_BG -->|"Lat, Lng"| GEO_ENGINE
    GEO_ENGINE -->|"OnPOIEntered"| EVENT_BUS
    EVENT_BUS -->|"POI Data"| NAR_ENGINE
```

### 4.2 Màn hình & Luồng điều hướng

| Màn hình | Chức năng | Kỹ thuật chính |
|---|---|---|
| Splash / Onboarding | Giới thiệu app, xin quyền GPS & Micro | Permissions API (MAUI Essentials) |
| Map View (Home) | Bản đồ + vị trí user + tất cả POI + POI nổi bật | MAUI Maps / Google Maps SDK + Custom pins |
| POI List | Danh sách POI, khoảng cách, trạng thái nghe | CollectionView + SQLite query |
| POI Detail | Ảnh, mô tả, audio player, link bản đồ | ScrollView + MediaElement / AVPlayer |
| Audio Player Bar | Mini player luôn hiển thị phía dưới, control phát/dừng | Overlay UI + MediaElement binding |
| QR Scanner | Quét QR → phát ngay nội dung POI tương ứng | ZXing.Net.MAUI hoặc Camera MAUI |
| **Settings** *(MỚI)* | **Chọn ngôn ngữ TTS, tốc độ nói, bán kính, gói offline** | **Preferences API + local storage** |
| **Offline Pack** *(MỚI)* | **Tải gói audio + POI để dùng offline** | **Background download + SQLite sync** |

### 4.3 Chức năng GPS Tracking

- **Foreground:** FusedLocationProviderClient (Android) / CLLocationManager (iOS) — cập nhật mỗi 5 giây
- **Background:** Foreground Service (Android) hiển thị notification liên tục — background location permission (iOS: Always)
- **Tối ưu pin:** sử dụng `PRIORITY_BALANCED_POWER_ACCURACY` thay vì HIGH_ACCURACY; tăng interval khi không di chuyển
- **Lọc nhiễu:** loại bỏ vị trí có accuracy > 50m hoặc speed không hợp lệ
- **Offline GPS:** vẫn hoạt động khi không có mạng — Geofence chỉ cần GPS

### 4.4 Chức năng Map View

- Hiển thị vị trí người dùng real-time (blue dot)
- Hiển thị toàn bộ POI dưới dạng custom marker (icon đặc trưng)
- Highlight POI gần nhất — marker to hơn, màu khác, animation pulse
- Nhấn vào marker → hiển thị popup preview → nhấn tiếp → POI Detail
- Tự động zoom/pan khi người dùng di chuyển đến POI mới
- Hỗ trợ offline map tile (Mapbox / HERE) nếu cần cache bản đồ

### 4.5 Chức năng TTS & Audio

| Tính năng | TTS (Text-to-Speech) | Pre-recorded Audio |
|---|---|---|
| Chất lượng giọng | Trung bình (Android native) / Cao (Azure) | Rất cao — giọng người thật |
| Dung lượng | Không tốn — tạo realtime | ~500KB–2MB / file mp3 |
| Offline | Native TTS hoạt động offline | Cần tải trước |
| Đa ngôn ngữ | Tốt (thay locale) | Cần thu âm từng ngôn ngữ |
| Recommended Use | POI mới, ngôn ngữ phụ, nội dung động | POI chính, bản ngữ Việt, quan trọng |

### 4.6 Màn hình Cài đặt & Tùy chỉnh *(MỚI)*

| Module | Tùy chọn | Mô tả |
|---|---|---|
| GPS & Bán kính | Độ nhạy GPS: High Accuracy / Battery Saver | Cân bằng giữa chính xác và tiết kiệm pin |
| | Bán kính kích hoạt: 20m / 50m / 100m | Phù hợp tốc độ di chuyển (đi bộ vs xe buýt) |
| | Chu kỳ GPS: 3s / 5s / 10s | Tần suất lấy tọa độ |
| Giọng TTS | Chọn giọng: Nam / Nữ | Tùy TTS engine hỗ trợ |
| | Tốc độ đọc: 0.75x – 1.5x | Chậm / Bình thường / Nhanh |
| | Âm lượng tự động | Tự điều chỉnh volume theo môi trường |
| Gói Offline | Danh sách Tour khả dụng | Hiển thị tour có thể tải trước |
| | Tải / Xóa gói | Download toàn bộ audio + data cho tour chọn |
| | Dung lượng hiển thị | Mỗi gói hiện rõ kích thước (VD: "Tour Ẩm thực Đêm - 45 MB") |
| Ngôn ngữ | Đổi ngôn ngữ | vi / en / ko / zh — re-sync translations khi đổi |
| | Dark Mode | Bản đồ Dark Theme cho di chuyển ban đêm |
| | Thông báo | Bật/tắt notification khi tiến gần POI |

---

## 5. CHIẾN LƯỢC ĐỒNG BỘ & OFFLINE-FIRST *(MỚI)*

### 5.1 Mô hình Offline-First

Mọi dữ liệu POI, audio và bản đồ đều được cache cục bộ trên SQLite & file system, đảm bảo ứng dụng vận hành 100% khi mất kết nối.

```mermaid
sequenceDiagram
    participant App as 📱 Mobile App
    participant Cache as 💾 SQLite Local
    participant API as 🖥️ Backend API
    participant DB as 🗄️ Server DB
    participant CDN as ☁️ CDN Storage

    Note over App,CDN: === Lần đầu mở App (Full Sync) ===
    
    App->>API: GET /api/sync/full
    API->>DB: Query all active POI + Translations
    DB-->>API: POI Data List
    API-->>App: JSON Response (POI + metadata)
    App->>Cache: Lưu toàn bộ vào SQLite
    
    loop Mỗi POI có audio_url
        App->>CDN: Download MP3 file
        CDN-->>App: Audio binary
        App->>Cache: Lưu file vào local storage
    end
    
    Note over App,CDN: === Mở App các lần sau (Delta Sync) ===
    
    App->>Cache: Đọc last_sync_timestamp
    App->>API: GET /api/sync/delta?since=timestamp
    API->>DB: Query POI updated since timestamp
    DB-->>API: Changed POI list
    API-->>App: Delta JSON
    
    alt Có thay đổi
        App->>Cache: Upsert POI mới/cập nhật
        App->>Cache: Xóa POI đã bị vô hiệu
        loop Audio mới hoặc cập nhật
            App->>CDN: Download MP3 mới
            App->>Cache: Thay thế audio cũ
        end
    end
    
    Note over App,CDN: === Khi mất mạng (Offline Mode) ===
    App->>Cache: Đọc 100% từ SQLite
    App->>App: Phát audio đã tải / fallback TTS offline
```

### 5.2 Chi tiết cơ chế đồng bộ

| Cơ chế | Mô tả |
|---|---|
| **Full Sync** | Lần đầu mở app: tải toàn bộ danh sách POI + translations + audio files theo ngôn ngữ đã chọn |
| **Delta Sync** | Gửi `last_sync_timestamp` → server chỉ trả về POI mới/cập nhật/xóa kể từ timestamp |
| **Offline Mode** | App dùng 100% SQLite cache. Audio cached phát bình thường. Audio chưa cache → fallback TTS nội bộ |
| **Conflict Resolution** | Server-wins: CMS là nguồn sự thật duy nhất. Server data ghi đè local khi sync |
| **Tải gói Tour** | User chọn Tour → download trước toàn bộ audio pack → dùng offline trọn vẹn |
| **Analytics offline** | Log ghi tạm vào SQLite → auto sync lên server khi có mạng trở lại |
| **Dung lượng ước tính** | ~50MB cho 50 POI (audio + ảnh) |

---

## 6. HỆ THỐNG ADMIN — CMS WEB

### 6.1 Tổng quan Admin System

Hệ thống CMS là ứng dụng web dành cho người quản trị, cho phép quản lý toàn bộ nội dung, theo dõi analytics và cấu hình hệ thống mà không cần can thiệp vào code.

```mermaid
graph TB
    subgraph CMS_WEB["💻 Admin CMS Web (ASP.NET Core MVC)"]
        direction TB
        
        subgraph AUTH_LAYER["🔐 Authentication"]
            LOGIN_PAGE["Trang Đăng nhập"]
            RBAC["Role-Based Access Control<br/>Super Admin | Content Admin | Editor | Viewer"]
        end
        
        subgraph DASHBOARD["📊 Dashboard"]
            STATS["Thống kê tổng quan"]
            TOP_POI["Top POI nghe nhiều nhất"]
            HEATMAP["Heat Map vị trí user"]
            TIME_CHART["Biểu đồ Time Spent"]
        end
        
        subgraph POI_MGMT["📍 Quản lý POI"]
            POI_LIST["Danh sách POI"]
            POI_CREATE["Tạo mới + Sửa + Xóa"]
            POI_MAP_PICKER["Map Picker chọn tọa độ"]
        end
        
        subgraph AUDIO_MGMT["🎵 Quản lý Audio"]
            UPLOAD_MP3["Upload file MP3/WAV"]
            AI_TTS["Tạo audio bằng AI TTS"]
            AUDIO_PREVIEW["Nghe thử trên browser"]
        end
        
        subgraph TOUR_MGMT["🗺️ Quản lý Tour (MỚI)"]
            TOUR_CRUD["CRUD Tour/Lộ trình"]
            TOUR_POI["Gán POI vào Tour + thứ tự"]
            TOUR_PREVIEW["Preview tuyến trên bản đồ"]
        end
        
        subgraph QR_MGMT["📱 Quản lý QR Code"]
            QR_GEN["Sinh QR Code cho POI"]
            QR_PRINT["Xuất file in ấn PNG/SVG"]
            QR_MAP["Gán QR → Trạm bus"]
        end
        
        subgraph ANALYTICS["📈 Phân tích dữ liệu"]
            LISTEN_LOG["Lịch sử phát audio"]
            USER_ROUTES["Tuyến di chuyển ẩn danh"]
            EXPORT["Xuất báo cáo CSV/Excel"]
        end
    end
```

### 6.2 Phân quyền người dùng Admin

| Quyền | Super Admin | Content Admin | Editor | Viewer |
|---|:---:|:---:|:---:|:---:|
| Xem Dashboard | ✅ | ✅ | ✅ | ✅ |
| Quản lý POI | ✅ | ✅ | ✅ (Edit) | ❌ |
| Upload Audio | ✅ | ✅ | ✅ | ❌ |
| Quản lý Tour | ✅ | ✅ | ❌ | ❌ |
| Analytics | ✅ | ✅ | ❌ | ✅ (Xem) |
| Quản lý User | ✅ | ❌ | ❌ | ❌ |
| System Config | ✅ | ❌ | ❌ | ❌ |
| Gen QR Code | ✅ | ✅ | ❌ | ❌ |

### 6.3 Module Quản lý POI

**Form tạo/sửa POI:**
- Tên POI (đa ngôn ngữ: VI, EN, KO, ZH...)
- Tọa độ: lat/lng (có thể pick trên bản đồ trực tiếp)
- Bán kính kích hoạt: 20m – 500m (slider)
- Mức ưu tiên: 1 (thấp) – 5 (cao)
- Trạng thái: Active / Inactive / Seasonal
- Ảnh minh họa: upload PNG/JPG, tự resize
- Mô tả văn bản: rich text editor
- Script TTS: textarea cho từng ngôn ngữ
- File audio: upload mp3/m4a, preview trực tiếp
- Link bản đồ: tự động generate Google Maps URL

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> ClickAddPOI : Bấm "Thêm mới Địa điểm"
    ClickAddPOI --> InputBasicData : Nhập Tọa độ, Bán kính, Ưu Tiên, Tên
    
    state check_translation <<choice>>
    InputBasicData --> check_translation
    
    check_translation --> PickLanguage : Thêm Bản Dịch
    check_translation --> ValidateData : Không thêm bản dịch nữa
    
    PickLanguage --> InputTranslationData : Nhập Tiêu đề, Kịch bản TTS
    InputTranslationData --> UploadAudioFiles : Upload MP3 (Optional)
    UploadAudioFiles --> check_translation : Vòng lặp thêm bản dịch khác
    
    state check_valid_db <<choice>>
    ValidateData --> check_valid_db
    
    check_valid_db --> ClickAddPOI : Lỗi (Thiếu data, Tọa độ sai)
    check_valid_db --> SaveToDatabase : Dữ liệu hợp lệ
    
    SaveToDatabase --> TriggerMobileUpdate : Gửi cờ thay đổi xuống Mobile App
    TriggerMobileUpdate --> [*]
```

### 6.4 Module Audio Management

- Upload file audio (drag & drop), tự encode sang AAC/MP3 128kbps
- Preview trực tiếp trong trình duyệt
- Quản lý phiên bản: v1, v2 — rollback nếu cần
- Liên kết audio với POI và ngôn ngữ cụ thể
- Tạo TTS preview ngay trên web (Azure/Google Cognitive API)
- Thống kê: số lượt phát, thời gian phát TB, drop-off point

```mermaid
sequenceDiagram
    participant Admin
    participant CMS as CMS Backend
    participant Storage as File Storage
    participant TTS as 3rd Party TTS API

    Admin->>CMS: Nhập script "Chào mừng đến phố ốc..."
    Admin->>CMS: Bấm [Tạo âm thanh AI] (Chọn giọng Nữ, Tiếng Việt)
    
    CMS->>CMS: Chuẩn hóa kịch bản
    CMS->>TTS: POST /v1/synthesize (Text, Voice_Id, Speed)
    TTS-->>CMS: Trả về Buffer Audio MP3
    
    CMS->>Storage: Lưu file MP3
    Storage-->>CMS: Trả về URL
    
    CMS->>CMS: Lưu URL vào DB
    CMS-->>Admin: Hiển thị "Nghe thử" trên trình duyệt
```

### 6.5 Module Analytics Dashboard

- Bản đồ nhiệt (Heat Map): mật độ người dùng theo khu vực (ẩn danh)
- Top 10 POI được nghe nhiều nhất trong 7/30 ngày
- Thời gian nghe trung bình per POI
- Lượt kích hoạt theo trigger type: GPS / QR / Manual
- Biểu đồ lượt nghe theo giờ/ngày/tuần
- Tỷ lệ hoàn thành audio (% nghe đến cuối)
- Export báo cáo: CSV, Excel

### 6.6 Module Quản lý Tour *(MỚI)*

| Chức năng | Mô tả |
|---|---|
| CRUD Tour | Tạo / Sửa / Xóa lộ trình tham quan (VD: "Tour Ẩm thực Ban Đêm") |
| Gán POI vào Tour | Kéo thả hoặc chọn checkbox POI → sắp xếp thứ tự ghé thăm (order_index) |
| Preview tuyến | Xem tuyến tour trên bản đồ mini trong CMS |
| Kích hoạt / Vô hiệu | Toggle trạng thái active cho từng tour |

### 6.7 Module QR Code

- Chọn POI → Generate QR Code PNG/SVG
- In QR với logo, tên điểm, khung viền chuyên nghiệp
- Gán QR → trạm xe buýt cụ thể (VD: Trạm 3, Phường Khánh Hội)
- QR Deep Link: `audiotour://poi/{poi_id}` → mở app và phát ngay
- Theo dõi lượt quét QR trong analytics
- Xuất file kích thước in poster (A4 / A3)

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> OpenCamera : Mở tính năng Quét QR
    OpenCamera --> ScanningQR : Quét mã trên cột trạm
    
    ScanningQR --> ValidateQRData
    
    state check_valid_qr <<choice>>
    ValidateQRData --> check_valid_qr
    
    check_valid_qr --> ShowErrorMsg : Không thuộc hệ thống TourMap
    check_valid_qr --> ExtractPOI_ID : Payload hợp lệ
    
    ShowErrorMsg --> ScanningQR : Đợi quét lại
    
    ExtractPOI_ID --> FetchPOIDetails : Lấy data theo ngôn ngữ hiện tại
    FetchPOIDetails --> BypassGeofenceQueue : Ép Narration Engine phát ngay
    
    BypassGeofenceQueue --> ShowPOIDetails : Zoom bản đồ & Hiện Popup
    ShowPOIDetails --> [*] : Kết thúc phiên quét
```

---

## 7. THIẾT KẾ CƠ SỞ DỮ LIỆU

### 7.1 ERD Diagram

```mermaid
erDiagram
    ADMIN_USERS ||--o{ ANALYTICS_LOGS : "tracks"
    ADMIN_USERS {
        int id PK
        string email UK
        string password_hash
        string display_name
        string role "SuperAdmin | ContentAdmin | Editor | Viewer"
        datetime created_at
        datetime last_login
    }

    MOBILE_USERS ||--o{ ANALYTICS_LOGS : "generates"
    MOBILE_USERS {
        int id PK
        string device_id UK "Anonymous tracking"
        string preferred_lang "vi | en | ko | zh"
        datetime first_opened
        datetime last_synced
    }

    POI ||--o{ AUDIO_CONTENT : "has audio"
    POI ||--o{ POI_IMAGES : "has images"
    POI ||--o{ ANALYTICS_LOGS : "tracked by"
    POI ||--o{ QR_CODES : "linked to"
    POI {
        int id PK
        string name
        text description
        float latitude
        float longitude
        float radius_meters "default 50-200m"
        int priority "1-5"
        bool is_active
        string image_url
        datetime created_at
        datetime updated_at
    }

    AUDIO_CONTENT {
        int id PK
        int poi_id FK
        string language_code "vi | en | ko | zh"
        string audio_file_url
        text tts_script
        int duration_sec
        string source "manual | ai_generated"
        bool is_active
        datetime created_at
        datetime updated_at
    }

    POI_IMAGES {
        int id PK
        int poi_id FK
        string image_url
        string caption
        int display_order
    }

    TOURS ||--|{ TOUR_POI_MAPPING : "contains"
    TOURS {
        int id PK
        string tour_code UK
        string name
        text description
        bool is_active
        datetime created_at
    }

    TOUR_POI_MAPPING {
        int id PK
        int tour_id FK
        int poi_id FK
        int order_index
    }

    QR_CODES {
        int id PK
        int poi_id FK
        string qr_data
        string location_desc "Trạm bus Khánh Hội - Trạm 3"
        datetime created_at
        datetime expires_at
    }

    ANALYTICS_LOGS {
        int id PK
        string user_anon_id
        int poi_id FK
        string event_type "listened | entered | exited"
        datetime event_time
        int duration_seconds
        float latitude
        float longitude
        string trigger_type "gps | qr_scan | manual"
    }

    USER_LOCATION_LOG {
        int id PK
        string user_anon_id
        float latitude
        float longitude
        datetime recorded_at
    }

    SYNC_LOG {
        int id PK
        string device_id
        datetime sync_time
        string sync_type "full | delta"
        int items_count
        bool success
    }
```

### 7.2 Mô tả bảng dữ liệu

| Bảng | Trường chính | Mô tả |
|---|---|---|
| poi | id, lat, lng, radius, priority | Điểm tham quan với tọa độ và thông số geofence |
| audio_content | poi_id, language, audio_url | File audio hoặc script TTS theo từng ngôn ngữ |
| analytics_logs | poi_id, user_anon_id, event_time | Ghi log mỗi lần phát — analytics + chống lặp |
| tours | id, name, is_active | Nhóm các POI thành tuyến tham quan |
| tour_poi_mapping | tour_id, poi_id, order_index | Liên kết POI với Tour theo thứ tự |
| qr_codes | poi_id, qr_data, expires_at | Dữ liệu QR code, thời gian hết hạn |
| user_location_log | user_anon_id, lat, lng | Lịch sử di chuyển ẩn danh cho heat map |
| admin_users | id, email, role | Tài khoản admin CMS với phân quyền |
| mobile_users | device_id, preferred_lang | Thông tin thiết bị ẩn danh |
| sync_log | device_id, sync_time, sync_type | Lịch sử đồng bộ |

---

## 8. THIẾT KẾ REST API

### 8.1 Các endpoint chính

| Method | Endpoint | Mô tả |
|---|---|---|
| GET | `/api/v1/pois` | Lấy danh sách tất cả POI active |
| GET | `/api/v1/pois/{id}` | Chi tiết một POI kèm audio content |
| GET | `/api/v1/pois/nearby?lat=&lng=&radius=` | Tìm POI trong bán kính (Haversine) |
| POST | `/api/v1/pois` | [Admin] Tạo POI mới |
| PUT | `/api/v1/pois/{id}` | [Admin] Cập nhật POI |
| DELETE | `/api/v1/pois/{id}` | [Admin] Xóa / vô hiệu hóa POI |
| GET | `/api/v1/audio/{poi_id}?lang=vi` | Lấy file audio URL theo ngôn ngữ |
| POST | `/api/v1/audio/upload` | [Admin] Upload file audio |
| POST | `/api/v1/analytics/play` | Ghi log lượt phát |
| POST | `/api/v1/analytics/location` | Ghi log vị trí ẩn danh (batch) |
| GET | `/api/v1/analytics/dashboard` | [Admin] Dữ liệu dashboard |
| GET | `/api/v1/qr/{poi_id}` | Resolve QR deep link |
| POST | `/api/v1/qr/generate/{poi_id}` | [Admin] Tạo QR code PNG |
| GET | `/api/v1/sync?since=timestamp` | Đồng bộ POI (delta sync) |
| GET | `/api/v1/sync/full` | Đồng bộ toàn bộ (full sync) |
| GET | `/api/v1/tours` | Danh sách Tour active |
| GET | `/api/v1/tours/{id}/pois` | POI trong Tour theo thứ tự |

---

## 9. YÊU CẦU PHI CHỨC NĂNG

### 9.1 Hiệu năng

| Tiêu chí | Yêu cầu |
|---|---|
| Thời gian khởi động app | ≤ 3 giây (cold start) |
| Thời gian trigger → phát audio | ≤ 2 giây từ khi vào geofence |
| GPS accuracy required | ≤ 20m cho POI radius 50m |
| Pin consumption (background) | ≤ 5% / giờ (background GPS + geofence) |
| API response time | < 500ms (p95) cho endpoint POI list |
| Delta Sync | ≤ 2 giây (Wi-Fi) / ≤ 5 giây (4G) |
| Offline capability | 100% core features hoạt động không cần internet |
| Dung lượng app | < 80MB (base app), gói offline ~50MB/50 POI |

### 9.2 Bảo mật & Quyền riêng tư

- Dữ liệu vị trí: **ẩn danh hoàn toàn** — không liên kết với tài khoản cá nhân
- API Authentication: **JWT Bearer Token** cho Admin endpoints
- **HTTPS bắt buộc** cho mọi kết nối API
- Mật khẩu CMS mã hóa **BCrypt** (salt + hash)
- Dữ liệu GPS không gửi lên server khi offline — ghi tạm SQLite
- GDPR / PDPA compliant: người dùng có thể từ chối thu thập analytics
- SQLite local có thể mã hóa bằng **SQLCipher** (tùy chọn)
- Admin CMS: 2FA cho Super Admin account

### 9.3 Khả năng mở rộng

- Hỗ trợ tối thiểu **500 POI** đồng thời trên bản đồ
- Backend xử lý **≥ 100 request/giây** đồng thời
- Analytics log xử lý **≥ 10,000 bản ghi/ngày**
- Multi-city: thêm địa điểm mới chỉ cần thêm POI — không cần deploy lại app
- Kiến trúc cho phép thêm ngôn ngữ mới mà **không cần thay đổi code**
- Plugin TTS: dễ dàng thay đổi provider (Azure ↔ Google Cloud TTS)

### 9.4 Khả dụng & Trải nghiệm (UX/Accessibility) *(MỚI)*

- App hoạt động ổn định **offline 100%** (với data đã cache)
- Font size đủ lớn, contrast ratio đạt chuẩn **WCAG AA**
- Hỗ trợ **Screen Reader** (TalkBack/VoiceOver) cho người khiếm thị
- Responsive UI: hỗ trợ màn hình từ **5" đến 10" tablet**

---

## 10. KIẾN TRÚC TRIỂN KHAI & MÔI TRƯỜNG *(MỚI)*

### 10.1 Sơ đồ Deployment

```mermaid
graph TB
    subgraph CLIENT["📱 Client Tier"]
        ANDROID["Android Device<br/>.NET MAUI APK"]
        IOS["iOS Device<br/>.NET MAUI IPA"]
    end
    
    subgraph SERVER["🖥️ Server Tier"]
        WEB_SERVER["ASP.NET Core<br/>Kestrel / IIS"]
        CMS_APP["Admin CMS (MVC)<br/>Port 5000"]
        API_APP["REST API<br/>Port 5001"]
    end
    
    subgraph DATA["🗄️ Data Tier"]
        SQL["SQL Server / SQLite"]
        FILE_ST["File Storage<br/>Audio MP3 | Images"]
    end
    
    subgraph EXTERNAL_SVC["☁️ External"]
        GOOGLE_TTS["Google Cloud TTS"]
        AZURE_TTS["Azure Cognitive Speech"]
        GOOGLE_MAPS["Google Maps Platform"]
    end
    
    ANDROID -->|HTTPS| WEB_SERVER
    IOS -->|HTTPS| WEB_SERVER
    
    WEB_SERVER --> CMS_APP
    WEB_SERVER --> API_APP
    
    CMS_APP --> SQL
    API_APP --> SQL
    CMS_APP --> FILE_ST
    
    CMS_APP -.-> GOOGLE_TTS
    CMS_APP -.-> AZURE_TTS
    ANDROID -.-> GOOGLE_MAPS
```

### 10.2 Môi trường

| Môi trường | Mục đích | Database | Ghi chú |
|---|---|---|---|
| **Development** | Dev cá nhân, debug | SQLite local | Hot reload, debug mode |
| **Staging** | Test tích hợp, UAT | SQL Server (test) | Dữ liệu mẫu, test geofence |
| **Production** | Người dùng thật | SQL Server (prod) | HTTPS, backup hàng ngày |

### 10.3 Quy trình CI/CD

1. **Code Push** → Git (GitHub/Azure DevOps)
2. **Build & Test** → Automated build + unit tests
3. **Staging Deploy** → Auto-deploy to staging server
4. **Production Release** → Manual approval → Deploy

---

## 11. KẾ HOẠCH TRIỂN KHAI & LỘ TRÌNH

### 11.1 Phân chia giai đoạn

| Giai đoạn | Thời gian | Nội dung | Deliverable |
|---|---|---|---|
| Phase 1 — POC | Tuần 1–2 | GPS tracking, Geofence Engine (Haversine), TTS cơ bản, SQLite local, Map View | App chạy được trên Android |
| Phase 2 — MVP | Tuần 3–4 | Audio file support, Queue management, POI detail, Offline sync, Background GPS | App hoàn chỉnh iOS + Android |
| Phase 3 — Backend | Tuần 5–6 | REST API, Database, File Storage, JWT Auth, Sync API | Backend deployed |
| Phase 4 — Admin | Tuần 7–8 | Admin CMS, POI management, Audio upload, QR code, **Tour management** | CMS deployed |
| Phase 5 — Analytics | Tuần 9–10 | Analytics dashboard, Heat map, Play logs, Export | Full system ready |
| Phase 6 — Polish | Tuần 11–12 | UI/UX polish, **Settings UI**, performance tuning, testing, docs | Final demo |

### 11.2 Stack công nghệ tổng hợp

| Layer | Công nghệ | Gói / Thư viện |
|---|---|---|
| Mobile Framework | .NET MAUI 8.0 | Microsoft.Maui, CommunityToolkit.Maui |
| GPS | MAUI Essentials + Native | Microsoft.Maui.Essentials (Geolocation) |
| Maps | Google Maps / MAUI Maps | Microsoft.Maui.Controls.Maps |
| Database (local) | SQLite | sqlite-net-pcl, EF Core + SQLite |
| TTS | Native TTS + Azure | Plugin.Maui.Audio, Azure.AI |
| QR Scanner | ZXing.Net.MAUI | ZXing.Net.Maui / BarcodeScanning.Maui |
| HTTP Client | HttpClient + Refit | Refit (typed API), Polly (retry) |
| Backend | ASP.NET Core 8 | Entity Framework Core, AutoMapper |
| Admin CMS | ASP.NET Core MVC | Views + Controllers |
| Database (server) | SQL Server / SQLite | EF Core provider |
| File Storage | Local / Azure Blob | Azure.Storage.Blobs + CDN |

---

## 12. RỦI RO & GIẢI PHÁP DỰ PHÒNG

| # | Rủi ro | Mức độ | Giải pháp |
|---|---|---|---|
| 1 | GPS drift / sai số lớn trong tòa nhà | Cao | Lọc vị trí có accuracy > 50m; dùng bán kính lớn hơn trong nhà |
| 2 | iOS giới hạn background GPS | Cao | Sử dụng significant-change API; hướng dẫn user cấp quyền Always |
| 3 | Tốn pin quá nhiều khi tracking liên tục | Trung bình | Giảm interval khi đứng yên; dùng BALANCED accuracy |
| 4 | TTS chất lượng thấp trên thiết bị cũ | Trung bình | Ưu tiên pre-recorded audio; Azure TTS fallback khi có mạng |
| 5 | POI trigger sai do GPS bounce | Trung bình | Debounce 30s + cooldown 10 phút + hysteresis threshold |
| 6 | Không có mạng → không sync được | Thấp | Offline-first SQLite; sync khi có mạng trở lại |
| 7 | Dung lượng audio file lớn | Thấp | Encode AAC 64kbps cho speech; lazy download theo khu vực |

---

## 13. PHỤ LỤC

### 13.1 Công thức Haversine (Geofence)

Khoảng cách giữa 2 điểm GPS trên bề mặt trái đất:

```
a = sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlng/2)
c = 2 × atan2(√a, √(1−a))
d = R × c       (R = 6,371,000 m — bán kính Trái Đất)
```

**C# Implementation:**

```csharp
public static double Haversine(double lat1, double lng1, double lat2, double lng2)
{
    const double R = 6371000;
    var dLat = (lat2 - lat1) * Math.PI / 180;
    var dLng = (lng2 - lng1) * Math.PI / 180;
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180) *
            Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
}
```

### 13.2 Deep Link Schema

```
URI Scheme:      audiotour://poi/{poi_id}
Universal Link:  https://audiotour.app/poi/{poi_id}
App Link:        https://audiotour.app/poi/{poi_id}

Ví dụ QR:        audiotour://poi/khanh-hoi-ben-tau
Flow:            Quét QR → System → App (nếu cài) hoặc Store
                 → App xử lý deep link → phát audio POI
```

### 13.3 Quyền hạn (Permissions)

| Permission | Platform | Lý do |
|---|---|---|
| ACCESS_FINE_LOCATION | Android | GPS chính xác cao khi foreground |
| ACCESS_BACKGROUND_LOCATION | Android 10+ | GPS tracking khi app ở background |
| FOREGROUND_SERVICE | Android | Background service liên tục |
| NSLocationAlwaysUsageDescription | iOS | GPS background trên iOS |
| NSCameraUsageDescription | iOS | Camera cho QR scanner |
| CAMERA | Android | Camera cho QR scanner |
| INTERNET | Android | Kết nối API, sync dữ liệu |

---

> **Audio Tour App — PRD v2.0  |  .NET MAUI  |  Tháng 3, 2026**
