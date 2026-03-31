# 🗺️ Tổng hợp Biểu đồ Hệ thống TourMap / Culinary Tourism

Tài liệu này cung cấp cái nhìn chi tiết nhất về luồng dữ liệu, thao tác và vòng đời của từng tính năng độc lập trong cả hai hệ thống: **Khách hàng (Mobile App)** và **Quản trị viên (Admin CMS)**.

---

## PHẦN I: HỆ THỐNG KHÁCH HÀNG (MOBILE APP)

### 1. Chức năng: Khởi động, Đăng nhập & Chọn ngôn ngữ
**Biểu đồ: Sơ đồ hoạt động (Activity Diagram)**  
Mô tả toàn cảnh 1 người dùng từ lúc bấm logo ứng dụng tới lúc họ tải thành công ứng dụng với ngôn ngữ do họ chọn.

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> SplashApp : Nhấn mở ứng dụng
    
    SplashApp --> CheckSettings
    CheckSettings --> LanguageScreen : Lần đầu tiên mở App
    CheckSettings --> AuthCheck : Đã từng thiết lập

    state SelectLanguage <<choice>>
    LanguageScreen --> SelectLanguage
    SelectLanguage --> SetDefaultVN : Bỏ qua / Mặc định
    SelectLanguage --> SaveCustomLang : Chọn ngôn ngữ (Anh/Hàn/Trung...)
    
    SetDefaultVN --> AuthCheck
    SaveCustomLang --> AuthCheck
    
    AuthCheck --> LoginScreen : Lần đầu / Chưa đăng nhập
    AuthCheck --> LoadCacheMap : Có Token lưu sẵn
    
    state login_action <<choice>>
    LoginScreen --> login_action
    login_action --> RegisterScreen : Bấm "Đăng ký tài khoản"
    login_action --> VerifyLogin : Gõ Tên Đăng Nhập & Mật Khẩu
    
    state check_login <<choice>>
    VerifyLogin --> check_login
    check_login --> SaveTokenData : Thành công (Lưu Token)
    check_login --> LoginScreen : Thất bại (Báo lỗi sai Pass)
    
    RegisterScreen --> InputRegisterData : Điền Username, Pass, Re-Pass
    state check_register <<choice>>
    InputRegisterData --> check_register
    check_register --> RegisterScreen : Lỗi (Pass không khớp / Trùng tên)
    check_register --> LoginScreen : Đăng ký thành công (Kèm thông báo)
    
    SaveTokenData --> FetchPOIByLanguage
    LoadCacheMap --> FetchPOIByLanguage
    
    FetchPOIByLanguage --> SetupGeofence : Nạp danh sách điểm đến
    SetupGeofence --> Ready : Khởi tạo thành công
    Ready --> [*]
```

### 2. Chức năng: Định vị Nhận diện Điểm đến (Geofencing GPS)
**Biểu đồ: Sơ đồ tuần tự (Sequence Diagram)**  
Chức năng cốt lõi theo dõi GPS người dùng, bao gồm cả khi chuyển App xuống chạy ngầm (Background Tracking).

```mermaid
sequenceDiagram
    participant User
    participant App
    participant GPS as Background GPS Service
    participant GeoEngine as Geofence Engine
    participant EventBus as App Event Bus

    User->>App: Cấp quyền "Luôn theo dõi vị trí"
    App->>GPS: Khởi động Background Tracking Service
    App->>GeoEngine: Chuyển mảng [Danh sách Tọa độ POI]
    
    loop Chu kỳ 3-5 giây (hoặc khi có rung lắc/Vibe)
        GPS->>GeoEngine: Bắn sự kiện tọa độ: Lat, Lng
        GeoEngine->>GeoEngine: Duyệt mảng POI, tính toán Haversine Formula
        
        alt Khoảng cách <= Bán kính kích hoạt & POI ưu tiên cao nhất
            GeoEngine->>EventBus: Broadcast Event "OnPOIEntered(POI_ID)"
            EventBus->>App: Xác nhận User đang ở trong vùng tham quan
        else Rời khỏi vùng
            GeoEngine->>EventBus: Broadcast Event "OnPOIExited(POI_ID)"
        end
    end
```

### 3. Chức năng: Phát âm thanh Thuyết minh tự động (Narration & Queue)
**Biểu đồ: Sơ đồ trạng thái (State Diagram)**  
Mô tả "Động cơ" xử lý âm thanh tự động, cách hệ thống tránh làm phiền khách hàng với cơ chế Cooldown (Chống Spam).

```mermaid
stateDiagram-v2
    direction LR
    
    [*] --> IDLE : Khởi động Narration System
    
    IDLE --> PREPARE_AUDIO : Tín hiệu từ Event Bus (Geofence)
    
    PREPARE_AUDIO --> PLAYING : Sẵn sàng phát (Stream/TTS Done)
    PREPARE_AUDIO --> IDLE : Lỗi hoặc Băng thông mạng kém
    
    PLAYING --> PAUSED : Người dùng thao tác tạm dừng / Nhận cuộc gọi
    PAUSED --> PLAYING : Hết cuộc gọi / Bấm tiếp tục
    
    PLAYING --> COOLDOWN : Phát hết toàn bộ kịch bản
    PLAYING --> IDLE : Người dùng tắt thủ công hoàn toàn
    
    COOLDOWN --> IDLE : Quá 3 phút (Cho phép điểm danh / nhắc lại nếu User vẫn đứng đó)
```

### 4. Chức năng: Quét mã QR tại trạm (Trạm dừng xe buýt Phường / Điểm tĩnh)
**Biểu đồ: Sơ đồ hoạt động (Activity Diagram)**  
Hỗ trợ du khách lười đi bộ, chỉ cần lấy máy quét mã QR gắn ngoài cột chờ xe buýt để nghe tích tắc.

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
    
    ExtractPOI_ID --> FetchPOIDetails : Lấy data theo Ngôn ngữ hiện tại
    FetchPOIDetails --> BypassGeofenceQueue : Ép Narration Engine phát ngay lập tức
    
    BypassGeofenceQueue --> ShowPOIDetails : Zoom bản đồ & Hiện Popup
    ShowPOIDetails --> [*] : Kết thúc phiên quét
```

---

## PHẦN II: HỆ THỐNG QUẢN TRỊ (ADMIN CMS WEB)

### 1. Kiến trúc Dữ liệu Đa ngôn ngữ (Database ERD - Entity Relationship)
**Biểu đồ: Thực Thể Ràng Buộc**  
Thiết kế dữ liệu cốt lõi phân rã sự đa ngôn ngữ thông qua bảng `POI_TRANSLATIONS`.

```mermaid
erDiagram
    USERS ||--o{ ANALYTICS_LOGS : tracks
    USERS {
        int id PK
        string email
        string password_hash
        string role "Admin, Editor, SubAdmin"
    }

    POI ||--o{ POI_TRANSLATIONS : holds
    POI {
        int id PK
        float lat
        float lng
        float radius
        int priority_level
    }

    POI_TRANSLATIONS {
        int id PK
        int poi_id FK
        string lang_code "vi, en, ko, zh"
        string title
        string auto_tts_script "Kịch bản đọc TTS"
        string custom_audio_url "Link MP3 gốc (Nếu có)"
    }

    TOURS ||--|{ TOUR_POI_MAPPING : contains
    TOURS {
        int id PK
        string tour_code "FoodTour_BanDem"
        string general_desc
    }

    TOUR_POI_MAPPING {
        int tour_id FK
        int poi_id FK
        int order_index "Thứ tự ghé thăm"
    }

    ANALYTICS_LOGS {
        int id PK
        int user_id FK
        int poi_id FK
        datetime listened_time
        int duration_seconds
    }
```

### 2. Chức năng: Quản lý Điểm đến (POI CRUD Lifecycle)
**Biểu đồ: Sơ đồ hoạt động cho Editor nhập liệu**  
Mô tả Editor tạo 1 điểm tham quan ẩm thực mới (Vd: Quán Ốc Vũ).

```mermaid
stateDiagram-v2
    direction TB
    
    [*] --> ClickAddPOI : Bấm "Thêm mới Địa điểm"
    ClickAddPOI --> InputBasicData : Nhập Tọa độ, Bán kính, Ưu Tiên, Tên
    
    state check_translation <<choice>>
    InputBasicData --> check_translation
    
    check_translation --> PickLanguage : Thêm Bản Dịch (Localization)
    check_translation --> ValidateData : Không thêm bản dịch nữa
    
    PickLanguage --> InputTranslationData : Nhập Tiêu đề, Kịch bản TTS (T.Anh/Hàn...)
    InputTranslationData --> UploadAudioFiles : Upload MP3 cho ngôn ngữ này (Optional)
    UploadAudioFiles --> check_translation : Vòng lặp thêm bản dịch khác
    
    state check_valid_db <<choice>>
    ValidateData --> check_valid_db
    
    check_valid_db --> ClickAddPOI : Lỗi (Thiếu data, Tọa độ sai)
    check_valid_db --> SaveToDatabase : Dữ liệu hợp lệ (Bấm Lưu)
    
    SaveToDatabase --> TriggerMobileUpdate : Ký xác nhận & Gửi cờ thay đổi xuống Mobile App
    TriggerMobileUpdate --> [*]
```

### 3. Chức năng: Quản lý & Tích hợp AI Text-To-Speech (Media Engine)
**Biểu đồ: Sơ đồ tuần tự tương tác Cloud AI**  
Mô tả Admin thay vì thu âm, họ chỉ việc nhập chữ, Backend sẽ nhờ AI sinh ra file Audio lưu lại tự động.

```mermaid
sequenceDiagram
    participant Admin
    participant CMS as CMS Backend
    participant Storage as Object Storage (S3/GCP)
    participant TTS as 3rd Party TTS_API (Google/Azure)

    Admin->>CMS: Nhập dòng Script (Vd: "Chào mừng đến phố ốc...")
    Admin->>CMS: Bấm [Tạo âm thanh tự động bằng AI] (Chọn Giọng Nữ, Tiếng Việt)
    
    CMS->>CMS: Chuẩn hóa kịch bản, Xóa ký tự đặc biệt
    CMS->>TTS: POST Request /v1/synthesize (Text, Voice_Id, Speed)
    
    TTS-->>CMS: Trả về Buffer Audio / Stream (Định dạng MP3)
    
    CMS->>Storage: Stream file MP3 vào Bucket lưu trữ riêng (CDN)
    Storage-->>CMS: Trả về Public_URL của Media (https://cdn.xyz/audio.mp3)
    
    CMS->>CMS: Lưu URL này vào cột `auto_tts_script` hoặc `custom_audio_url`
    CMS-->>Admin: Hiển thị thanh trình phát "Nghe thử" trên trình duyệt
```

### 4. Chức năng: Đăng nhập CMS & Phân quyền Hệ thống (Auth)
**Biểu đồ: Sơ đồ tuần tự**  
Thao tác Auth chặt chẽ bằng Role-based Access Control.

```mermaid
sequenceDiagram
    participant Editor
    participant WebClient as NextJS / React Web
    participant AuthAPI as .NET Core Backend
    participant DB as SQL Server

    Editor->>WebClient: Vào /login, gõ Email & Password
    WebClient->>AuthAPI: POST /api/auth/login
    
    AuthAPI->>DB: Query User by Email
    DB-->>AuthAPI: User Hash & Salt, Role Data
    
    AuthAPI->>AuthAPI: So khớp BCrypt / Hash
    
    alt Hợp lệ
        AuthAPI->>AuthAPI: Sinh JWT Token (Kèm dính Role="Editor")
        AuthAPI-->>WebClient: Trả về 200 OK, Cookie/Header + JWT
        WebClient->>WebClient: Cache Token
        WebClient->>Editor: Điều hướng vào Dashboard
    else Không hợp lệ
        AuthAPI-->>WebClient: 401 Unauthorized
        WebClient-->>Editor: "Sai thông tin đăng nhập"
    end
```
