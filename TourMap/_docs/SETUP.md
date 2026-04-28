# TourMap - Hướng dẫn Setup Project

Ứng dụng thuyết minh tự động tại các điểm du lịch sử dụng công nghệ GPS và QR Code.

---

## Yêu cầu hệ thống

### Backend (AdminWeb)
- **.NET 10 SDK** (khuyến nghị .NET 10.0+)
- **SQL Server 2019+** hoặc **SQL Server Express**
- **Visual Studio 2022** hoặc **VS Code + C# Dev Kit**

### Mobile App
- **.NET 10 SDK**
- **Android SDK** (API 34+, target Android 14+)
- **Android Emulator** hoặc **thiết bị thật**
- **JDK 17**

---

## 1. Clone & Cấu trúc Project

```
Project-CSharp-SGU-MobileApp/
├── TourMap/                          # Mobile App (.NET MAUI)
│   ├── Platforms/
│   │   ├── Android/                  # Android-specific code (TTS, GPS, QR)
│   │   └── iOS/                      # iOS-specific code
│   ├── Pages/                        # UI Pages (Map, QR, POI Details)
│   ├── Services/                     # Business Logic
│   │   ├── Audio/                    # TTS & Audio Player
│   │   ├── Data/                     # Database & Sync
│   │   ├── Tracking/                 # GPS & Geofence
│   │   └── Infrastructure/           # Backend endpoints
│   ├── Models/                       # Entity Models
│   ├── ViewModels/                   # MVVM ViewModels
│   └── TourMap.csproj
│
├── TourMap.AdminWeb/                 # Backend (ASP.NET Core)
│   ├── Controllers/                  # MVC + API Controllers
│   ├── Hubs/                         # SignalR Real-time
│   ├── Data/                         # DbContext & EF Core
│   ├── Models/                       # Entity Models
│   ├── Views/                        # Razor Views
│   └── appsettings.json
│
├── _docs/                            # Documentation
├── scripts/                          # PowerShell scripts
└── database_setup.sql                # SQL Server setup script
```

---

## 2. Setup Database

### 2.1 Tạo Database SQL Server

**Cách 1: Dùng script SQL (Khuyến nghị)**
```powershell
-- Mở file _docs/database_setup.sql trong SQL Server Management Studio (SSMS)
-- Execute script
USE master;
CREATE DATABASE TourMapAdmin;
GO
```

**Cách 2: Dùng EF Core Migrations**
```powershell
cd TourMap.AdminWeb
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2.2 Cấu hình Connection String

File: `TourMap.AdminWeb/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=TourMapAdmin;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "AppLinks": {
    "AndroidPackage": "com.companyname.tourmap"
  },
  "Admin": {
    "Username": "admin",
    "Password": "admin2026"
  }
}
```

**Thay đổi:**
- `YOUR_SERVER` → Server name của bạn (ví dụ: `localhost\SQLEXPRESS`)

---

## 3. Chạy Backend (AdminWeb)

### 3.1 Restore & Build
```powershell
cd TourMap.AdminWeb
dotnet restore
dotnet build
```

### 3.2 Chạy Server
```powershell
dotnet run --urls "http://localhost:5042;https://localhost:5043"
```

Hoặc dùng Visual Studio: `F5` (Debug) hoặc `Ctrl+F5` (Run without debug)

### 3.3 Kiểm tra
- **Admin Web:** http://localhost:5042
- **API Docs:** http://localhost:5042/swagger (chỉ Development)

**Default Admin Account:**
- Username: `admin`
- Password: `admin2026` (có thể đổi trong `appsettings.json`)

---

## 4. Chạy Mobile App

### 4.1 Restore Packages
```powershell
cd TourMap
dotnet restore
```

### 4.2 Cấu hình Backend URL

File: `TourMap/Services/Infrastructure/BackendEndpoints.cs`
```csharp
// Thêm IP của PC chạy backend (thay YOUR_IP bằng IP thật)
candidates.Add("http://YOUR_IP:5042");

// Hoặc dùng Ngrok cho thiết bị từ xa
// AddAuthorityFromUrl(candidates, "https://abc123.ngrok-free.dev");
```

**Tìm IP máy:**
```powershell
ipconfig | findstr IPv4
```

### 4.3 Build & Deploy

**Android Emulator:**
```powershell
dotnet build -t:Run -f net10.0-android
```

**Android Device (qua USB):**
```powershell
dotnet build -t:Run -f net10.0-android
```

**Hoặc dùng Visual Studio:**
- Chọn target device → `F5` hoặc `Ctrl+F5`

---

## 5. Chạy qua Internet (Ngrok) - Cho thiết bị từ xa

### 5.1 Cài đặt Ngrok
```powershell
choco install ngrok
# hoặc
winget install ngrok.ngrok
```

Đăng ký tài khoản Ngrok (free): https://ngrok.com/
```powershell
ngrok config add-authtoken YOUR_AUTH_TOKEN
```

### 5.2 Start Ngrok Tunnel
```powershell
cd scripts
.\Start-NgrokTunnel.ps1 -Port 5042
```

### 5.3 Cập nhật URL trong Mobile App

Copy URL từ Ngrok (ví dụ: `https://abc123.ngrok-free.dev`)

File: `TourMap/Services/Infrastructure/BackendEndpoints.cs`
```csharp
AddAuthorityFromUrl(candidates, "https://abc123.ngrok-free.dev");
```

**Rebuild và cài đặt lại app.**

---

## 6. Lệnh hữu ích

### Build & Deploy
```powershell
# Build Mobile App (Android)
dotnet build TourMap/TourMap.csproj -f net10.0-android

# Deploy và chạy (Android)
dotnet build -t:Run -f net10.0-android

# Build Admin Web
dotnet build TourMap.AdminWeb/TourMap.AdminWeb.csproj

# Run Admin Web
dotnet run --project TourMap.AdminWeb --urls "http://localhost:5042"
```

### Clean & Rebuild
```powershell
dotnet clean
rd /s /q TourMap/obj TourMap/bin /q 2>$null
dotnet restore
dotnet build
```

### Xem Logs (Android)
```powershell
# Tất cả log liên quan đến app
adb logcat -s "TourMap" -s "QR" -s "TTS" -s "Narration" -s "Database" -s "Sync"

# Chỉ TTS & Narration
adb logcat -s "TTS" -s "Narration"

# Chỉ Database
adb logcat -s "Database" -s "Sync"
```

### Database Management
```powershell
# Kiểm tra devices
adb devices

# Truy cập SQLite database trên device (debug)
adb shell
run-as com.companyname.tourmap
cd files
ls *.db

# Pull database về máy
adb shell run-as com.companyname.tourmap cp files/TourMap.db /sdcard/
adb pull /sdcard/TourMap.db C:\Temp\
```

---

## 7. Troubleshooting

### Lỗi: "Cannot connect to backend"
**Nguyên nhân:**
- Backend không chạy
- Firewall chặn port 5042
- Sai IP trong `BackendEndpoints.cs`

**Giải pháp:**
```powershell
# Kiểm tra backend có chạy không
curl http://localhost:5042/api/v1/pois/sync/pois

# Kiểm tra firewall
netsh advfirewall firewall add rule name="TourMap" dir=in action=allow protocol=TCP localport=5042

# Ping test từ điện thoại
adb shell ping YOUR_IP
```

### Lỗi: "Camera permission denied"
**Giải pháp:**
- Vào Settings → Apps → TourMap → Permissions
- Bật Camera và Location
- Hoặc uninstall → reinstall app

### Lỗi: "Database connection failed"
**Giải pháp:**
- Kiểm tra SQL Server đang chạy: `services.msc` → SQL Server
- Thử dùng SQL Server Authentication:
```json
"DefaultConnection": "Server=localhost;Database=TourMapAdmin;User Id=sa;Password=YOUR_PASSWORD;TrustServerCertificate=True"
```

### Lỗi Build: Java Out of Memory
**Giải pháp:**
```powershell
# Tăng Java heap size
$env:JAVA_TOOL_OPTIONS = "-Xmx4g"
dotnet build
```

### Lỗi: "TTS không hoạt động"
**Giải pháp:**
1. Kiểm tra log: `adb logcat -s "TTS"`
2. Kiểm tra ngôn ngữ đã được hỗ trợ trên device
3. Xóa app data và cài đặt lại

---

## 8. Cấu trúc Database

### SQL Server (AdminWeb)

| Bảng | Mô tả |
|------|-------|
| `AdminUsers` | Tài khoản admin CMS |
| `MobileUsers` | Tài khoản mobile app (Guest, User, Admin) |
| `Pois` | Điểm tham quan (POI) với TTS scripts |
| `Tours` | Tour du lịch |
| `TourPoiMappings` | Liên kết Tour-POI với thứ tự |
| `PlaybackHistories` | Lịch sử phát audio (analytics) |
| `DeviceConnections` | Kết nối real-time qua SignalR |
| `QrCodeEntries` | Danh sách QR codes đã tạo |
| `UserLocationLogs` | Log vị trí user |

### SQLite (Mobile App)

| Bảng | Mô tả |
|------|-------|
| `Poi` | POI data (local cache) |
| `Tour` | Tour data |
| `TourPoiMapping` | Tour-POI mappings |
| `PlaybackHistoryEntry` | Lịch sử phát local |

---

## 9. Tính năng chính

### Mobile App
- **Quét QR Code:** Phát audio ngay lập tức
- **GPS Auto-play:** Tự động phát khi đến gần POI (Geofence)
- **Đa ngôn ngữ:** Tiếng Việt, English, 中文 (Chinese), 한국어 (Korean), 日本語 (Japanese), Français (French)
- **Offline Mode:** Download POI data để dùng offline
- **TTS (Text-to-Speech):** Phát nội dung thuyết minh qua giọng nói
- **Guest Mode:** Không cần đăng ký để sử dụng cơ bản
- **Tour Mode:** Theo dõi lộ trình tour với nhiều POI

### Admin Web
- **Quản lý POIs:** CRUD với hỗ trợ 6 ngôn ngữ
- **Quản lý Tours:** Tạo tour từ danh sách POI
- **TTS Scripts:** Quản lý nội dung thuyết minh TTS
- **Upload Audio:** Upload file MP3 cho mỗi ngôn ngữ
- **QR Code Generator:** Tạo QR cho từng POI
- **Real-time Tracking:** Xem thiết bị đang online
- **Analytics:** Thống kê lượt phát audio
- **Device Management:** Quản lý thiết bị kết nối

---

## 10. Kiến trúc kỹ thuật

### Mobile App Architecture
```
┌─────────────────────────────────────┐
│           UI Layer (Pages)           │
├─────────────────────────────────────┤
│         ViewModels (MVVM)            │
├─────────────────────────────────────┤
│      Services (Business Logic)       │
│  ├─ GeofenceEngine (GPS + Radius)    │
│  ├─ NarrationEngine (TTS + Audio)    │
│  ├─ DatabaseService (SQLite)         │
│  ├─ SyncService (API Sync)           │
│  └─ AuthService (JWT)                │
├─────────────────────────────────────┤
│      Platform (Android/iOS)            │
│  ├─ TtsService_Android               │
│  ├─ GpsTrackingService                │
│  └─ MainActivity (QR Scanner)        │
└─────────────────────────────────────┘
```

### API Endpoints chính
- `GET /api/v1/pois/sync/pois` - Sync POIs
- `GET /api/v1/tours/sync/tours` - Sync Tours
- `POST /api/v1/pois/sync/history` - Log playback
- `POST /api/auth/login` - JWT authentication

---

## 11. Liên hệ & Hỗ trợ

**Nếu gặp vấn đề:**
1. Kiểm tra log files trong `TourMap.AdminWeb/logs/`
2. Xem console output của mobile app (`adb logcat`)
3. Kiểm tra network connectivity: `ping`, `telnet`
4. Review code trên repository

**Debug tips:**
```powershell
# Enable verbose logging trong Mobile App
# Thêm vào MauiProgram.cs:
builder.Logging.SetMinimumLevel(LogLevel.Debug);

# Kiểm tra API response
curl -v http://localhost:5042/api/v1/pois/sync/pois
```

---

**Last Updated:** 2025-04-29
