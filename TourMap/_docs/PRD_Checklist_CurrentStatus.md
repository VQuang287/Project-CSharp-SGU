# Checklist Trạng Thái Dự Án Audio Tour Guide

Checklist này theo dõi tiến độ phát triển dự án dựa trên file PRD, được chia thành 6 giai đoạn phát triển chính.

**Chú thích:**
- `[x]` - Hoàn thành
- `[/]` - Đang phát triển / Có bản cơ bản
- `[ ]` - Chưa bắt đầu

---

### Phase 1: Nền tảng & MVP Cốt lõi (Foundation & Core MVP)

*Mục tiêu: Xây dựng bộ khung kỹ thuật và các chức năng tối thiểu để ứng dụng có thể hoạt động và kiểm thử được.*

- [x] Thiết lập project .NET MAUI cho ứng dụng di động.
- [x] Hiển thị bản đồ và vị trí hiện tại của người dùng.
- [x] Định nghĩa mô hình dữ liệu cho Điểm tham quan (POI).
- [x] Hiển thị các POI trên bản đồ.
- [x] Dịch vụ theo dõi vị trí GPS.
- [x] Dịch vụ chạy nền trên Android (Foreground Service) để đảm bảo GPS hoạt động ổn định.
- [x] Dịch vụ phát âm thanh cơ bản.
- [x] Xây dựng Engine Geofencing để phát hiện khi người dùng đến gần một POI.
- [x] Xây dựng Engine Tường thuật (Narration Engine) để tự động phát audio tương ứng với POI.
- [x] Thiết lập cơ sở dữ liệu cục bộ (SQLite) để lưu trữ dữ liệu offline.

---

### Phase 2: Quản lý Nội dung & Trải nghiệm Cơ bản

*Mục tiêu: Hoàn thiện hệ thống quản lý cho admin và cung cấp các tính năng cơ bản cho người dùng cuối.*

- [x] Thiết lập project Admin Web (ASP.NET Core / Blazor).
- [/] Chức năng CRUD (Thêm, Sửa, Xóa, Xem) cho các POI trên trang Admin.
- [ ] Chức năng CRUD cho các Tour (tập hợp các POI theo tuyến) trên trang Admin.
- [/] Dịch vụ đồng bộ hóa dữ liệu giữa ứng dụng và máy chủ.
- [x] Chức năng quét mã QR để truy cập nhanh POI hoặc Tour.
- [/] Tích hợp cơ bản dịch vụ Text-to-Speech (TTS).
- [/] Hỗ trợ đa ngôn ngữ cho các thành phần giao diện (UI).
- [ ] Chức năng tìm kiếm POI / Tour trong ứng dụng.

---

### Phase 3: Nâng cao Trải nghiệm & Tương tác Người dùng

*Mục tiêu: Bổ sung các tính năng giúp giữ chân người dùng và làm cho ứng dụng trở nên hữu ích hơn.*

- [/] Hệ thống xác thực người dùng đầy đủ (Đăng nhập, Đăng ký, Đăng xuất).
- [ ] Chức năng quản lý hồ sơ cá nhân (User Profile).
- [ ] Ghi lại lịch sử các tour đã tham gia.
- [ ] Hỗ trợ bản đồ offline (cho phép người dùng tải trước bản đồ khu vực).
- [ ] Hiển thị tuyến đường đề xuất của một Tour trên bản đồ.
- [ ] Hỗ trợ hiển thị nội dung đa phương tiện phong phú (thư viện ảnh, video) tại mỗi POI.
- [ ] Hỗ trợ đầy đủ nội dung đa ngôn ngữ cho cả dữ liệu của POI (tên, mô tả...).

---

### Phase 4: Thương mại hóa & Nội dung Cao cấp

*Mục tiêu: Triển khai các mô hình kinh doanh và hỗ trợ các định dạng nội dung tiên tiến.*

- [ ] Phân định các gói Tour/tính năng (miễn phí vs. trả phí).
- [ ] Tích hợp cổng thanh toán (In-App Purchase).
- [ ] Chức năng quản lý gói đăng ký (Subscription) của người dùng.
- [ ] Hỗ trợ hiển thị ảnh/video 360 độ.
- [ ] Khám phá và tích hợp tính năng thực tế tăng cường (Augmented Reality - AR) để hiển thị thông tin POI.

---

### Phase 5: Tính năng Cộng đồng & Xã hội

*Mục tiêu: Xây dựng một cộng đồng người dùng xung quanh ứng dụng.*

- [ ] Hệ thống đánh giá và xếp hạng (rating/review) cho Tours và POIs.
- [ ] Khu vực bình luận (comment) của người dùng.
- [ ] Chức năng chia sẻ lên mạng xã hội (ví dụ: chia sẻ tour đã hoàn thành).
- [ ] Cho phép người dùng đóng góp nội dung (ví dụ: đề xuất một địa điểm mới).

---

### Phase 6: Phân tích, Tối ưu hóa & Công nghệ Tương lai

*Mục tiêu: Sử dụng dữ liệu để cải tiến sản phẩm và thử nghiệm các công nghệ mới.*

- [ ] Xây dựng Bảng điều khiển (Dashboard) trên trang Admin để phân tích dữ liệu (ví dụ: tour phổ biến, số lượng người dùng...).
- [ ] Áp dụng Gamification (huy hiệu, điểm thưởng, bảng xếp hạng) để tăng tương tác.
- [ ] Tối ưu hóa hiệu năng ứng dụng (tốc độ khởi động, tải bản đồ...).
- [ ] Tích hợp hệ thống Push Notification để thông báo tour mới hoặc các chương trình khuyến mãi.
