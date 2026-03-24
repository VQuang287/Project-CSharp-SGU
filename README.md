# Project-CSharp-SGU
PRODUCT REQUIREMENTS DOCUMENT (PRD) 
Tên dự án: Ứng dụng Thuyết minh Địa điểm tự động (Location-based Audio Guide)  
Nền tảng: Mobile App (.NET MAUI) & Web CMS (ASP.NET Core MVC)  
Giai đoạn: Đồ án môn học C# 

1. TỔNG QUAN SẢN PHẨM (PRODUCT OVERVIEW) 
1.1. Mục tiêu sản phẩm 
Xây dựng một hệ sinh thái bao gồm ứng dụng di động tự động phát nội dung thuyết minh dựa trên vị trí thời gian thực (Real-time GPS) và hệ thống quản trị nội dung nền web (Web CMS). Ứng dụng phục vụ khách du lịch/người tham quan di chuyển qua các điểm cầu, mang lại trải nghiệm rảnh tay (hands-free) và liền mạch thông qua công nghệ Geofencing. 

1.2. Luồng hoạt động cốt lõi (User Flow) 
Khởi tạo & Đồng bộ: Ứng dụng Mobile gọi luồng API từ Server để tải danh sách POI (Point of Interest) và cấu hình vùng kích hoạt lưu vào Local DB (SQLite). 
Theo dõi vị trí: Khi người dùng di chuyển, Location Service liên tục bắt tọa độ GPS (Foreground/Background). 
Kích hoạt (Geofence Engine): Tính toán khoảng cách (Haversine formula). Nếu phát hiện người dùng đi vào bán kính kích hoạt của POI có mức ưu tiên cao nhất -> phát sinh sự kiện (Event). 
Kiểm duyệt (Narration Engine): Kiểm tra trạng thái rủi ro (Nội dung này đã phát trong X phút qua chưa? Tránh Loop/Spam). 
Thực thi & Ghi nhận: Khởi động Text-to-Speech (TTS), đưa vào hàng đợi Audio Queue phát tiếng. Cuối cùng, POST API ngầm trả về Server ghi nhận Lịch sử nghe để phân tích dữ liệu (Analytics). 

2. KIẾN TRÚC HỆ THỐNG GỢI Ý (SYSTEM ARCHITECTURE) 
Hệ thống được chia thành 4 lớp (layers) chính: 
Location + Geofencing Layer: Xử lý điều hướng phần cứng GPS và tính toán khoảng cách hình học, phát hiện va chạm vùng địa lý (OnEnter, OnNear). 
Narration Engine (Xử lý âm thanh): Tiếp nhận sự kiện Geofencing. Quản lý hàng đợi (Audio Queue), chống trùng lặp dội âm (Debounce/Cooldown), và tự động kết nối API của Hệ điều hành. 
Content Layer (Quản lý dữ liệu): Lưu trữ Offline-First bằng sqlite-net-pcl. Dữ liệu tĩnh luôn có sẵn dưới máy, độc lập với kết nối 4G/Wifi. 
UI/UX Layer: Render Bản đồ bản địa (OpenStreetMap qua engine Mapsui), vẽ Marker, danh sách POI trực quan, mượt mà trên nền tảng .NET MAUI thuần C#. 

3. YÊU CẦU CHỨC NĂNG CƠ BẢN (GIAI ĐOẠN 1 - MOBILE) 
3.1. GPS Tracking theo thời gian thực 
Foreground Tracking: Lấy vị trí liên tục với sai số thấp khi mở màn hình hiển thị. 
Background Tracking: Hỗ trợ cấu hình FOREGROUND_SERVICE_LOCATION duy trì theo dõi GPS không bị hệ điều hành đóng băng khi tắt màn hình điện thoại. 

3.2. Geofence & Kích hoạt điểm (POI Trigger) 
Thuộc tính POI cơ bản: Id, Title, Tọa độ (Latitude/Longitude), Bán kính kích hoạt (RadiusMeters), Mức độ ưu tiên (Priority). 
Cơ chế chống Spam/Nhiễu (Debounce & Cooldown): 
Debounce: Yêu cầu thiết bị nằm trong vùng kích hoạt ít nhất X giây để lọc nhiễu sóng GPS (hạn chế false-positive trigger). 
Cooldown lock: POI sau khi phát xong sẽ bị khóa (Lock) trong vòng Y phút để không lặp lại tiếng khi người dùng dừng chân ăn uống tại đó. 

3.3. Thuyết minh tự động (Narration) 
Tích hợp công nghệ Text-to-Speech (TTS) bằng Microsoft.Maui.Media.TextToSpeech. 
Linh hoạt, nhỏ nhẹ, hỗ trợ đọc các file văn bản (Description) thành ngôn ngữ tự nhiên, không tốn tài nguyên bộ nhớ mạng tải Media mp3. 

3.4. Quản lý dữ liệu & Hàng chờ 
Xử lý hàng chờ (Queue) nếu lỡ đi ngang 2 điểm POI gần nhau cùng lúc $\rightarrow$ điểm nào Priority cao hơn sẽ được chèn lên đầu hàng chờ. 
Tính năng Duck Volume: Áp dụng luồng tiến trình tự động dừng/nhường khi có cuộc gọi nội bộ (OS-level interruptions). 

3.5. Bản đồ (Map View UI) 
Tích hợp Bản đồ mã nguồn mở OpenStreetMap (thông qua Mapsui.Maui) thay thế Google Maps (tối ưu chi phí API). 
Tracking vị trí thực tế của người dùng thời gian thực (Blue dot marker). 
Đổ toàn bộ danh sách POI từ SQLite thành Red Marker (Ghim đỏ). Ống kính bản đồ tự động focus vào người dùng. 

4. YÊU CẦU NÂNG CAO (GIAI ĐOẠN 2 - WEB ADMIN & CLOUD) 
4.1. Hệ thống Admin CMS (ASP.NET Core MVC) 
Giao diện Admin quản trị ứng dụng mô hình MVC, kết nối với Database độc lập thông qua Entity Framework Core. 
Hệ thống CRUD: Thêm, sửa, xóa, quản trị danh sách Điểm tham quan (POI), quản lý thư viện Audio và Text nội dung. 

4.2. API & Quản trị Analytics / Phân tích dữ liệu 
Bổ sung cấu trúc bảng dữ liệu PlaybackHistory (Lịch sử sử dụng). 
Hệ thống API Đồng bộ: Bộc lộ các RESTFul Endpoints: 
GET /api/sync/pois: Mobile tự động chép dữ liệu mới. 
POST /api/analytics/history: Mobile tự động báo cáo ẩn danh dữ liệu người dùng vừa nghe điểm nào. 
Thống kê Dashboard (Web): Tính năng Heat Map & Report mật độ tập trung đánh giá mức độ hấp dẫn của các POI. 

4.3. Kích hoạt nội dung bổ sung (QR Code) 
Use case bổ sung: Cho phép người dùng trực tiếp mở Camera App tích hợp để quyét mã QR dán tại trạm xe/địa điểm -> Bỏ qua Geofence -> Đẩy thẳng vào hàng đợi TTS Audio. 

5. YÊU CẦU PHI CHỨC NĂNG (NON-FUNCTIONAL REQUIREMENTS) 
Kiến trúc Offline-First: 100% Core Map logic và Audio Playback lấy từ SQLite. Server Die app vẫn sống khỏe. 
Hiệu năng: Tốc độ phản hồi (Response Time) từ khi chạm vùng Radius Geofence đến lệnh Fire Audio phải <= 2 giây. 
Design Pattern: Code áp dụng Dependency Injection (DI) Service Registration mạnh mẽ chuẩn .NET, Singleton Service (cho Audio/GPS) và MVVM architecture tách
biệt giao diện/logic. 
