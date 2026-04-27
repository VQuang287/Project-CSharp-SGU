# TourMap - Hướng dẫn Setup Project

## Yêu cầu hệ thống

### Backend (AdminWeb)
- **.NET 9 SDK** hoặc cao hơn
- **SQL Server 2019+** hoặc **SQL Server Express**
- **Visual Studio 2022** hoặc **VS Code**

### Mobile App
- **.NET 10 SDK**
- **Android SDK** (API 31+)
- **Android Emulator** hoặc **thiết bị thật**
- **JDK 17**

---

## 1. Clone & Cấu trúc Project

```
Project-CSharp-SGU-MobileApp/
├── TourMap/                          # Mobile App (.NET MAUI)
│   ├── Platforms/Android/            # Android-specific code
│   ├── Platforms/iOS/                # iOS-specific code (mới thêm)
│   ├── Pages/                        # UI Pages
│   ├── Services/                     # Business Logic
│   └── TourMap.csproj
│
├── TourMap.AdminWeb/                 # Backend (ASP.NET Core)
│   ├── Controllers/                  # API Controllers
│   ├── Data/                         # DbContext
│   ├── Models/                       # Entity Models
│   ├── Views/                        # Razor Views
│   └── TourMap.AdminWeb.csproj
│
└── scripts/                          # PowerShell scripts
    └── Start-NgrokTunnel.ps1
```

---

## 2. Setup Database

### 2.1 Tạo Database SQL Server

**Cách 1: Dùng script SQL**
```sql
-- Mở file TourMap.AdminWeb/create_database.sql trong SSMS
-- Execute script
```

**Cách 2: Dùng EF Core Migrations**
```powershell
cd TourMap.AdminWeb
dotnet ef database update
```

### 2.2 Cấu hình Connection String

File: `TourMap.AdminWeb/appsettings.json`
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=VQUANG\\SQLEXPRESS;Database=TourMapAdmin;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

**Thay đổi:**
- `VQUANG\SQLEXPRESS` → Server name của bạn
- `TourMapAdmin` → Tên database

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
dotnet run --urls "http://localhost:5042"
```

Hoặc dùng Visual Studio: `F5` (Debug) hoặc `Ctrl+F5` (Run without debug)

### 3.3 Kiểm tra
- **Admin Web:** http://localhost:5042
- **Swagger API:** http://localhost:5042/swagger (chỉ Development)

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
// Thêm IP của PC chạy backend
candidates.Add("http://192.168.1.X:5042");  // Thay X bằng IP thật
```

**Tìm IP:**
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
dotnet build -t:Run -f net10.0-android -p:AndroidSdkDirectory="%ANDROID_HOME%"
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

Rebuild và cài đặt lại app.

---

## 6. Lệnh hữu ích

### Build Mobile App
```powershell
dotnet build TourMap/TourMap.csproj -f net10.0-android
```

### Clean & Rebuild
```powershell
dotnet clean
dotnet build
```

### Xem Logs (Android)
```powershell
adb logcat -s "TourMap" -s "QR" -s "TTS" -s "Narration"
```

### Deploy mới nhất
```powershell
dotnet build -t:Run -f net10.0-android
```

---

## 7. Troubleshooting

### Lỗi: "Cannot connect to backend"
- Kiểm tra firewall (cho phép port 5042)
- Kiểm tra IP trong `BackendEndpoints.cs`
- Ping test: `ping 192.168.1.X` từ điện thoại

### Lỗi: "Camera permission denied"
- Vào Settings → Apps → TourMap → Permissions
- Bật Camera permission

### Lỗi: "Database connection failed"
- Kiểm tra SQL Server đang chạy
- Kiểm tra connection string
- Thử dùng SQL Server Authentication thay vì Windows Auth

### Lỗi Build
```powershell
dotnet clean
rd /s /q obj bin  # Xóa thư mục build cũ
dotnet restore
dotnet build
```

---

## 8. Cấu trúc Database

| Bảng | Mô tả |
|------|-------|
| `AdminUsers` | Tài khoản admin |
| `MobileUsers` | Tài khoản mobile app |
| `Pois` | Điểm tham quan (POI) |
| `Tours` | Tour du lịch |
| `TourPoiMappings` | Liên kết Tour-POI |
| `PlaybackHistories` | Lịch sử phát audio |
| `DeviceConnections` | Kết nối real-time |
| `QrCodeEntries` | QR codes |
| `UserLocationLogs` | Log vị trí user |

---

## 9. Tính năng chính

### Mobile App
- ✅ Quét QR code → phát audio thuyết minh
- ✅ GPS tự động phát khi đến gần POI
- ✅ Ngôn ngữ: Tiếng Việt, English, 中文, 한국어, 日本語, Français
- ✅ Offline mode (download POI data)
- ✅ Guest mode (không cần đăng ký)

### Admin Web
- ✅ Quản lý POIs (CRUD)
- ✅ Quản lý Tours
- ✅ Upload audio files
- ✅ QR code generation
- ✅ Real-time device tracking
- ✅ Playback analytics

---

## Liên hệ hỗ trợ

Nếu gặp vấn đề, kiểm tra:
1. Log files trong `TourMap.AdminWeb/logs/`
2. Console output của mobile app
3. Network connectivity giữa device và PC

---

**Last Updated:** 2025-04-27
