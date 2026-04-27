# Ngrok Setup - Cho phép thiết bị từ xa kết nối

## Bước 1: Cài đặt Ngrok

### Cách 1: Chocolatey (Khuyến nghị)
```powershell
choco install ngrok
```

### Cách 2: Winget
```powershell
winget install ngrok.ngrok
```

### Cách 3: Manual
1. Tải từ https://ngrok.com/download
2. Giải nén ngrok.exe vào `C:\Tools\ngrok\`
3. Thêm vào PATH: `[Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Tools\ngrok", "User")`

## Bước 2: Khởi động Server

Terminal 1:
```powershell
cd TourMap.AdminWeb
run-admin
# hoặc
dotnet run --urls "http://0.0.0.0:5042"
```

## Bước 3: Khởi động Ngrok Tunnel

Terminal 2:
```powershell
# Cách 1: Dùng script có sẵn
.\scripts\Start-NgrokTunnel.ps1

# Cách 2: Chạy ngrok trực tiếp
ngrok http http://localhost:5042 --host-header=localhost
```

## Bước 4: Cập nhật URL trong Mobile App

Ngrok sẽ hiển thị URL dạng: `https://abc123.ngrok-free.dev`

Sửa file `Services/Infrastructure/BackendEndpoints.cs`:

```csharp
// Thay dòng này:
AddAuthorityFromUrl(candidates, "https://nectar-fade-repose.ngrok-free.dev");

// Bằng URL ngrok mới:
AddAuthorityFromUrl(candidates, "https://abc123.ngrok-free.dev");
```

Rebuild mobile app và cài lại.

## Lưu ý quan trọng

1. **Ngrok URL thay đổi mỗi lần chạy** (với free tier)
2. **Phải cập nhật URL mới** trong BackendEndpoints.cs mỗi lần
3. **Ngrok free có giới hạn**: 40 connections/minute, 1 online process
4. **Để URL ổn định**: Cần ngrok paid hoặc dùng Cloud (Azure/AWS)

## Workflow nhanh

```powershell
# Terminal 1 - Server
cd TourMap.AdminWeb
run-admin

# Terminal 2 - Ngrok (sau khi server chạy)
cd TourMap
.\scripts\Start-NgrokTunnel.ps1

# Copy URL ngrok cung cấp -> BackendEndpoints.cs
# Rebuild mobile -> Deploy
```

## Troubleshooting

### Lỗi "Session Status: reconnecting"
→ Kiểm tra server đã chạy chưa: `http://localhost:5042`

### Lỗi "failed to auth"
→ Chưa config authtoken: `ngrok authtoken YOUR_TOKEN` (lấy từ dashboard.ngrok.com)

### Mobile không kết nối được
→ Kiểm tra URL trong BackendEndpoints.cs có đúng không
→ Kiểm tra http/https (mobile yêu cầu https hoặc clear-text traffic config)
