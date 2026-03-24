# Project-CSharp-SGU
# 📍 Project-CSharp-SGU: Location-based Audio Guide
> **Ứng dụng Thuyết minh Địa điểm tự động** > Hệ sinh thái di động hỗ trợ khách tham quan dựa trên vị trí thời gian thực.

---

## 1. TỔNG QUAN SẢN PHẨM (PRODUCT OVERVIEW)

### 1.1. Mục tiêu sản phẩm
Xây dựng một hệ sinh thái bao gồm ứng dụng di động tự động phát nội dung thuyết minh dựa trên vị trí thời gian thực (Real-time GPS) và hệ thống quản trị nội dung nền web (Web CMS). Ứng dụng mang lại trải nghiệm rảnh tay (hands-free) và liền mạch thông qua công nghệ **Geofencing**.

* **Nền tảng Mobile:** .NET MAUI (C#)
* **Nền tảng Web CMS:** ASP.NET Core MVC
* **Giai đoạn:** Đồ án môn học C#

### 1.2. Luồng hoạt động cốt lõi (User Flow)

graph TD
    A[Khởi tạo & Đồng bộ] -->|Tải POI từ Server| B[(Local DB - SQLite)]
    B --> C[Location Service: Theo dõi GPS]
    C --> D{Vào vùng Geofence?}
    D -- Có --> E[Narration Engine: Kiểm duyệt]
    E -->|Tránh Loop/Spam| F[Thực thi TTS - Audio Queue]
    F --> G[POST API: Ghi nhận Analytics]
    D -- Không --> C

2. KIẾN TRÚC HỆ THỐNG GỢI Ý

Lớp (Layer),Chức năng chính,Công nghệ sử dụng
Location,"Xử lý GPS, tính Haversine, phát hiện va chạm địa lý.",Native Location API
Narration,"Quản lý Audio Queue, chống trùng lặp (Debounce).",Microsoft.Maui.Media
Content,"Lưu trữ Offline-First, quản lý dữ liệu tĩnh.",sqlite-net-pcl
UI/UX,"Render bản đồ, Marker, điều hướng MVVM.",Mapsui.Maui

3. YÊU CẦU CHỨC NĂNG (GIAI ĐOẠN 1 - MOBILE)
3.1. GPS Tracking & Geofencing
Tracking: Hỗ trợ cả Foreground và Background (duy trì khi tắt màn hình).

Cơ chế POI Trigger:

Debounce: Lọc nhiễu sóng GPS (yêu cầu ở trong vùng X giây).

Cooldown Lock: Khóa POI trong Y phút sau khi phát để tránh lặp nội dung.

3.2. Thuyết minh tự động (Narration)
Sử dụng công nghệ Text-to-Speech (TTS): Tiết kiệm bộ nhớ và băng thông.

Priority Queue: Ưu tiên phát các điểm có mức độ quan trọng cao hơn nếu vào nhiều vùng cùng lúc.

Duck Volume: Tự động giảm/nhường âm thanh khi có cuộc gọi.

3.3. Bản đồ (Map View UI)
Tích hợp OpenStreetMap (qua Mapsui) để tối ưu chi phí.

Tự động Focus ống kính vào vị trí thực tế của người dùng (Blue dot).

4. YÊU CẦU NÂNG CAO (GIAI ĐOẠN 2 - WEB & CLOUD)
4.1. Hệ thống Admin CMS (ASP.NET Core MVC)
Quản trị CRUD: Thêm/Sửa/Xóa các điểm tham quan (POI).

Quản lý thư viện nội dung Text và Audio.

4.2. API & Analytics
GET /api/sync/pois: Mobile đồng bộ dữ liệu mới nhất.

POST /api/analytics/history: Báo cáo dữ liệu nghe ẩn danh.

Dashboard: Hiển thị Heat Map mật độ tập trung của khách du lịch.

4.3. QR Code Integration
Cho phép quét mã QR tại trạm để kích hoạt thuyết minh ngay lập tức (Bỏ qua Geofence).

5. YÊU CẦU PHI CHỨC NĂNG
Offline-First: Hoạt động tốt ngay cả khi không có kết nối mạng (trừ phần Sync).

Hiệu năng: Độ trễ kích hoạt âm thanh khi vào vùng Radius <= 2 giây.

Design Pattern: Sử dụng Dependency Injection (DI), Singleton Service và mô hình MVVM.
