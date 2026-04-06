AUDIO TOUR APP

Tài liệu đặc tả yêu cầu sản phẩm

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

## Tài liệu đặc tả yêu cầu sản phẩm (PRD)

## MỤC LỤC

1. Tổng quan dự án	4

1.1 Mô tả sản phẩm	4

1.2 Mục tiêu	4

1.3 Phạm vi và đối tượng	4

2. Kiến trúc hệ thống tổng quan	5

2.1 Sơ đồ kiến trúc tổng quát	5

2.2 Các thành phần kiến trúc	5

2.3 Luồng hoạt động tổng thể	6

3. Geofence Engine và Narration Engine	7

3.1 Geofence Engine	7

3.2 Narration Engine	8

4. Hệ thống app người dùng	9

4.1 Sơ đồ chi tiết Mobile App	9

4.2 Màn hình và luồng điều hướng	9

4.3 Chức năng GPS Tracking	10

4.4 Chức năng Map View	10

4.5 Chức năng TTS và Audio	11

4.6 Màn hình Cài đặt và Tùy chỉnh	11

5. Chiến lược đồng bộ và offline-first	12

5.1 Mô hình Offline-First	12

5.2 Chi tiết cơ chế đồng bộ	13

6. Hệ thống admin và CMS web	14

6.1 Tổng quan Admin System	14

6.2 Phân quyền người dùng Admin	14

6.3 Module Quản lý POI	15

6.4 Module Audio Management	16

6.5 Module Analytics Dashboard	17

6.6 Module Quản lý Tour	18

6.7 Mô-đun QR Code	18

7. Thiết kế cơ sở dữ liệu	19

7.1 ERD Diagram	19

7.2 Mô tả bảng dữ liệu	20

8. Thiết kế REST API	21

8.1 Các endpoint chính	21

9. Yêu cầu phi chức năng	22

9.1 Hiệu năng	22

9.2 Bảo mật và Quyền riêng tư	22

9.3 Khả năng mở rộng	23

9.4 Khả dụng và Trải nghiệm	23

10. Kiến trúc triển khai và môi trường	24

10.1 Sơ đồ Deployment	24

10.2 Môi trường	25

10.3 Quy trình CI/CD	25

11. Kế hoạch triển khai và lộ trình	26

11.1 Phân chia giai đoạn	26

11.2 Stack công nghệ tổng hợp	26

12. Rủi ro và giải pháp dự phòng	27

13. Phụ lục	28

13.1 Công thức Haversine	28

13.2 Deep Link Schema	29

13.3 Quyền hạn	29

# 1. TỔNG QUAN DỰ ÁN

## 1.1 Mô tả sản phẩm

Audio Tour App là ứng dụng di động hỗ trợ thuyết minh tự động cho khách tham quan và du lịch. Hệ thống sử dụng dữ liệu định vị GPS để kích hoạt nội dung âm thanh, bao gồm giọng nói tổng hợp (TTS) hoặc tệp thu sẵn, khi người dùng tiếp cận các điểm tham quan đã được xác định trước.

Ứng dụng hướng tới việc số hóa trải nghiệm tham quan, thay thế tour guide truyền thống bằng công nghệ định vị và âm thanh hiện đại, phù hợp với nhu cầu du lịch tự do và các tuyến di sản văn hóa đô thị.

## 1.2 Mục tiêu

Cung cấp trải nghiệm thuyết minh tự động theo định hướng offline-first, bảo đảm các chức năng cốt lõi vẫn khả dụng khi không có kết nối mạng.

Hỗ trợ đa ngôn ngữ, ưu tiên tiếng Việt và tiếng Anh, đồng thời có khả năng mở rộng sang các ngôn ngữ khác thông qua cơ chế TTS.

Tích hợp mã QR nhằm kích hoạt nội dung tại các điểm dừng, trạm xe buýt và bảng thông tin ngoài hiện trường.

Cung cấp hệ thống CMS cho phép quản trị nội dung, audio và cấu hình vận hành một cách linh hoạt.

Thu thập dữ liệu phân tích ẩn danh để đánh giá hành vi sử dụng và cải tiến trải nghiệm người dùng.

## 1.3 Phạm vi & Đối tượng

# 2. KIẾN TRÚC HỆ THỐNG TỔNG QUAN

## 2.1 Sơ đồ kiến trúc tổng quát

Kiến trúc tổng thể được tổ chức thành ba thành phần chính gồm ứng dụng di động dành cho người dùng cuối, hệ thống Backend API và nền tảng quản trị Admin Web CMS.                  

Hình 1. Sơ đồ Kiến trúc Tổng quan

## 2.2 Các thành phần kiến trúc

## 2.3 Luồng hoạt động tổng thể

Hình 2. Luồng Hoạt động App

# 3. GEOFENCE ENGINE & NARRATION ENGINE

## 3.1 Geofence Engine

Geofence Engine đóng vai trò thành phần xử lý trung tâm, liên tục đối chiếu vị trí hiện thời của người dùng với tập hợp POI để xác định thời điểm kích hoạt nội dung thuyết minh.

Hình 3. Geofence Engine Flow

Thông số kỹ thuật Geofence:

## 3.2 Narration Engine

Narration Engine quản lý toàn bộ vòng đời của tiến trình phát âm thanh, bao gồm tiếp nhận sự kiện từ Geofence Engine, lựa chọn nguồn phát TTS hoặc tệp âm thanh, điều phối hàng đợi và ghi nhận nhật ký hoạt động.

Hình 4. Narration Engine Sequence

Trạng thái Narration Engine:

Hình 5. Narration Engine State Diagram

Ưu tiên phát audio:

# 4. HỆ THỐNG APP NGƯỜI DÙNG (MOBILE APP)

## 4.1 Sơ đồ chi tiết Mobile App

Hình 6. Mobile App Architecture

## 4.2 Màn hình & Luồng điều hướng

## 4.3 Chức năng GPS Tracking

Chế độ foreground sử dụng FusedLocationProviderClient trên Android và CLLocationManager trên iOS với chu kỳ cập nhật mặc định 5 giây.         

Chế độ background được triển khai thông qua Foreground Service trên Android và quyền truy cập vị trí mức Always trên iOS nhằm duy trì khả năng theo dõi liên tục.         

Cơ chế tối ưu năng lượng ưu tiên mức độ chính xác cân bằng thay cho High Accuracy, đồng thời tăng chu kỳ lấy mẫu khi phát hiện thiết bị ít hoặc không di chuyển.                           

Hệ thống lọc nhiễu bằng cách loại bỏ các mẫu vị trí có sai số lớn hơn 50 m hoặc thông tin tốc độ không hợp lệ.         

Chức năng định vị và geofence vẫn hoạt động khi không có kết nối mạng, vì cơ chế kích hoạt chỉ phụ thuộc vào dữ liệu GPS trên thiết bị.         

## 4.4 Chức năng Map View

Hiển thị vị trí hiện thời của người dùng trên bản đồ theo thời gian thực thông qua ký hiệu nhận diện trực quan.

Hiển thị toàn bộ POI dưới dạng marker tùy biến, giúp phân biệt nhanh các loại điểm tham quan.

Điểm POI gần nhất được nhấn mạnh thông qua marker kích thước lớn hơn, màu sắc phân biệt và hiệu ứng thu hút sự chú ý.

Tương tác với marker sẽ mở khung xem trước, từ đó cho phép người dùng di chuyển đến màn hình chi tiết của POI.

Bản đồ tự động điều chỉnh tầm nhìn và mức phóng to khi người dùng di chuyển đến khu vực có POI mới liên quan.

Hệ thống có khả năng mở rộng để hỗ trợ tile bản đồ offline trong trường hợp cần lưu đệm dữ liệu bản đồ cục bộ.

## 4.5 Chức năng TTS & Audio

## 4.6 Màn hình Cài đặt & Tùy chỉnh 

# 5. CHIẾN LƯỢC ĐỒNG BỘ & OFFLINE-FIRST 

## 5.1 Mô hình Offline-First

Dữ liệu POI, tài nguyên âm thanh và các thành phần phục vụ hiển thị được lưu đệm cục bộ trên SQLite và hệ thống tệp, qua đó bảo đảm ứng dụng tiếp tục vận hành ổn định ngay cả trong điều kiện mất kết nối mạng.

Hình 7. Offline / Delta Sync Flow

## 5.2 Chi tiết cơ chế đồng bộ

# 6. HỆ THỐNG ADMIN — CMS WEB

## 6.1 Tổng quan Admin System

Hệ thống CMS là ứng dụng web phục vụ công tác quản trị, cho phép quản lý nội dung, theo dõi số liệu phân tích và cấu hình hệ thống mà không cần can thiệp trực tiếp vào mã nguồn.

Hình 8. Admin CMS Structure

## 6.2 Phân quyền người dùng Admin

## 6.3 Module Quản lý POI

Biểu mẫu tạo mới và cập nhật POI bao gồm các trường thông tin sau:

Tên POI (đa ngôn ngữ: VI, EN, KO, ZH...)

Tọa độ: lat/lng (có thể pick trên bản đồ trực tiếp)

Bán kính kích hoạt: 20m – 500m (slider)

Mức ưu tiên: 1 (thấp) – 5 (cao)

Trạng thái: Active / Inactive / Seasonal

Ảnh minh họa: upload PNG/JPG, tự resize

Mô tả văn bản: rich text editor

Script TTS: textarea cho từng ngôn ngữ

File audio: upload mp3/m4a, preview trực tiếp

Link bản đồ: tự động generate Google Maps URL

Hình 9. POI Management Flow

## 6.4 Module Audio Management

Cho phép tải lên tệp âm thanh theo cơ chế kéo thả, đồng thời chuyển mã về định dạng AAC hoặc MP3 128 kbps.

Hỗ trợ nghe thử trực tiếp trong trình duyệt nhằm thuận tiện cho quy trình thẩm định nội dung.

Quản lý phiên bản nội dung âm thanh theo nhiều lần phát hành và hỗ trợ quay lui khi cần thiết.

Liên kết tài nguyên âm thanh với từng POI và ngôn ngữ cụ thể để bảo đảm tính nhất quán trong phân phối nội dung.

Cung cấp chức năng tạo bản xem trước TTS ngay trên giao diện web thông qua các dịch vụ Azure hoặc Google.

Tổng hợp các chỉ số như số lượt phát, thời gian nghe trung bình và điểm dừng nghe để phục vụ phân tích.

Hình 10. AI TTS Audio Generation

## 6.5 Module Analytics Dashboard

Bản đồ nhiệt thể hiện mật độ xuất hiện của người dùng theo khu vực trên cơ sở dữ liệu ẩn danh.

Thống kê 10 POI có tần suất nghe cao nhất trong các cửa sổ thời gian 7 ngày và 30 ngày.

Tính toán thời gian nghe trung bình đối với từng POI.

Phân loại lượt kích hoạt theo cơ chế GPS, QR và thao tác thủ công.

Cung cấp biểu đồ theo dõi tần suất nghe theo giờ, ngày và tuần.

Đo lường tỷ lệ hoàn tất nội dung âm thanh, phản ánh mức độ tiếp nhận của người dùng.

Hỗ trợ xuất báo cáo dữ liệu dưới các định dạng CSV và Excel.

## 6.6 Module Quản lý Tour 

## 6.7 Mô-đun QR Code

Cho phép chọn POI để sinh mã QR dưới dạng PNG hoặc SVG.

Hỗ trợ tạo mẫu in mã QR kèm logo, tên điểm và khung trình bày phù hợp với ấn phẩm truyền thông.

Gắn mã QR với các vị trí vật lý cụ thể, như trạm xe buýt hoặc bảng thông tin tại điểm đến.

Mã QR có thể chứa deep link đến đối tượng POI, cho phép mở ứng dụng và kích hoạt phát nội dung ngay lập tức.                  

Số lượt quét QR được thu thập trong hệ thống phân tích để phục vụ đánh giá mức độ tiếp cận.

Hỗ trợ xuất tệp với kích thước phù hợp cho nhu cầu in ấn khổ A4 và A3.

Hình 11. QR Scan Flow

# 7. THIẾT KẾ CƠ SỞ DỮ LIỆU

## 7.1 ERD Diagram

Hình 12. Database ERD

## 7.2 Mô tả bảng dữ liệu

# 8. THIẾT KẾ REST API

## 8.1 Các endpoint chính

# 9. YÊU CẦU PHI CHỨC NĂNG

## 9.1 Hiệu năng

## 9.2 Bảo mật & Quyền riêng tư

Dữ liệu vị trí được xử lý theo nguyên tắc ẩn danh hóa, không liên kết trực tiếp với danh tính cá nhân.         

Các API quản trị sử dụng cơ chế xác thực JWT Bearer Token.         

Mọi kết nối API bắt buộc phải được thiết lập trên kênh HTTPS.  

Mật khẩu trên hệ thống CMS được bảo vệ bằng cơ chế băm BCrypt kèm salt.         

Dữ liệu GPS phát sinh trong trạng thái offline được lưu tạm thời trên SQLite và đồng bộ lại khi có kết nối.

Hệ thống hướng tới tuân thủ các nguyên tắc GDPR và PDPA, cho phép người dùng từ chối thu thập dữ liệu phân tích.

Cơ sở dữ liệu SQLite trên thiết bị có thể được mã hóa bằng SQLCipher trong các kịch bản yêu cầu mức bảo mật cao hơn.         

Tài khoản Super Admin trên CMS có thể được tăng cường bảo mật bằng cơ chế xác thực hai yếu tố.

## 9.3 Khả năng mở rộng

Hệ thống được kỳ vọng hỗ trợ tối thiểu 500 POI hiển thị đồng thời trên bản đồ.         

Backend cần đáp ứng năng lực xử lý tối thiểu 100 yêu cầu mỗi giây trong điều kiện đồng thời.         

Hệ thống phân tích cần xử lý tối thiểu 10.000 bản ghi sự kiện mỗi ngày.

Kiến trúc cho phép mở rộng sang nhiều khu vực địa lý thông qua việc bổ sung POI mà không cần phát hành lại ứng dụng.

Mô hình dữ liệu và nội dung cho phép mở rộng ngôn ngữ mới mà hạn chế tối đa can thiệp vào mã nguồn.

Tầng TTS được thiết kế theo hướng có thể thay đổi nhà cung cấp dịch vụ, ví dụ Azure hoặc Google Cloud TTS.

## 9.4 Khả dụng & Trải nghiệm (UX/Accessibility)

Ứng dụng duy trì mức độ khả dụng cao trong chế độ offline đối với phạm vi dữ liệu đã được lưu đệm.         

Giao diện cần bảo đảm kích thước chữ và độ tương phản phù hợp với khuyến nghị WCAG AA.

Hệ thống hướng tới khả năng tương thích với các trình đọc màn hình như TalkBack và VoiceOver.         

Giao diện được thiết kế đáp ứng cho nhiều kích thước màn hình, từ điện thoại thông minh đến máy tính bảng.

# 10. KIẾN TRÚC TRIỂN KHAI & MÔI TRƯỜNG *(MỚI)*

## 10.1 Sơ đồ Deployment

Hình 13. Deployment Architecture

## 10.2 Môi trường

## 10.3 Quy trình CI/CD

Mã nguồn được đồng bộ lên kho Git, ví dụ GitHub hoặc Azure DevOps.         

Quy trình xây dựng và kiểm thử được tự động hóa, bao gồm biên dịch và chạy các bộ kiểm thử đơn vị.         

Bản phát hành thử nghiệm được triển khai tự động lên môi trường staging.         

Bản phát hành chính thức yêu cầu bước phê duyệt trước khi triển khai lên môi trường production.         

# 11. KẾ HOẠCH TRIỂN KHAI & LỘ TRÌNH

## 11.1 Phân chia giai đoạn

## 11.2 Stack công nghệ tổng hợp

# 12. RỦI RO & GIẢI PHÁP DỰ PHÒNG

# 13. PHỤ LỤC

## 13.1 Công thức Haversine (Geofence)

Khoảng cách giữa hai điểm GPS trên bề mặt Trái Đất được tính theo công thức Haversine như sau:

a = sin²(Δlat/2) + cos(lat1) × cos(lat2) × sin²(Δlng/2)
c = 2 × atan2(√a, √(1−a))
d = R × c       (R = 6,371,000 m — bán kính Trái Đất)

Ví dụ cài đặt bằng C#:

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

## 13.2 Deep Link Schema

URI Scheme: audiotour://poi/{poi_id}. Universal Link và App Link sử dụng dạng https://audiotour.app/poi/{poi_id}. Ví dụ mã QR: audiotour://poi/khanh-hoi-ben-tau. Luồng xử lý được xác lập theo chuỗi: quét QR, hệ thống chuyển hướng đến ứng dụng nếu đã cài đặt hoặc đến kho ứng dụng nếu chưa có, sau đó ứng dụng tiếp nhận deep link và kích hoạt nội dung audio tương ứng.





         

## 13.3 Quyền hạn (Permissions)



| --- | --- |
| Nền tảng |   .NET MAUI 10.0 (Android / iOS) |
| Thời gian |   Tháng 3, 2026 |
| Môn học |   Đồ án Lập trình C# |



| --- | --- |
|  |  |
| Nền tảng | .NET MAUI 10.0 (Android / iOS) |
| Ngày | Tháng 3, 2026 |
| Môn học | Đồ án Lập trình Mobile |



| --- | --- |
| Hạng mục | Chi tiết |
| Đối tượng nghiên cứu chính | Du khách tham quan các khu di sản và không gian văn hóa tại phường Khánh Hội, TP.HCM. |
| Nền tảng triển khai | .NET MAUI 10.0 cho Android (API 31+) và iOS (14+). |
| Backend | ASP.NET Core Web API ket hop SQLite hoac SQL Server va lop luu tru tep. |
| Admin CMS | Ứng dụng ASP.NET Core MVC phục vụ quản lý POI, audio và phân tích vận hành. |
| Địa lý PoC | Các phường Khánh Hội, Quận 4, TP.HCM (giai đoạn đầu) |



| --- | --- | --- |
| Thành phần | Công nghệ | Mô tả |
| Mobile App | .NET MAUI 10.0 | Ứng dụng khách trên Android và iOS, đảm nhiệm định vị, geofence, phát thuyết minh, bản đồ và quét QR. |
| GPS Service | FusedLocationProvider / MAUI Essentials | Thành phần theo dõi vị trí ở chế độ foreground và background. |
| Geofence Engine | Custom C# + Haversine | Thành phần xác định POI nằm trong bán kính kích hoạt, có bộ lọc debounce và cooldown. |
| Narration Engine | Android TTS / Azure Cognitive | Thành phần điều phối hàng đợi âm thanh, lựa chọn TTS hoặc tệp thu sẵn, đòng thời ngăn kích hoạt lặp. |
| Local DB | SQLite + EF Core | Kho dữ liệu cục bộ lưu thông tin POI offline, nhật ký phát và cấu hình người dùng. |
| Backend API | ASP.NET Core 10 | REST API, hỗ trợ CRUD POI, Analytics, Auth và Sync |
| Database | SQL Server / SQLite | Lưu trữ POI, audio metadata, play logs, users |
| File Storage | Local / Azure Blob / CDN | Lưu file audio .mp3/.m4a, ảnh POI |
| Admin CMS | ASP.NET Core MVC | Quản lý POI, audio, analytics, cài đặt hệ thống |



| --- | --- | --- |
| Tham số | Giá trị mặc định | Mô tả |
| GPS Update Interval | 5 giây (foreground) | Tần suất cập nhật vị trí khi ứng dụng đang hoạt động foreground. |
| GPS Update Interval (BG) | 15 giây (background) | Tần suất cập nhật vị trí trong chế độ background, ưu tiên tiết kiệm năng lượng. |
| GPS Accuracy | Medium (100m) | Độ chính xác GPS — cân bằng pin vs chính xác |
| Min Trigger Radius | 20m | Bán kính tối thiểu để trigger |
| Default POI Radius | 50–200m | Bán kính mặc định của POI (admin cấu hình) |
| Debounce Time | 30 giây | Thời gian chờ tối thiểu sau lần trigger đầu tiên |
| Cooldown Per POI | 10 phút | Không phát lại cùng 1 POI trong vòng 10 phút |
| Max POIs in Range | Top 3 | Chỉ xử lý 3 POI gần nhất/ưu tiên cao nhất |
| Distance Formula | Haversine | Tính khoảng cách theo đường cong bề mặt trái đất |



| --- | --- | --- |
| Ưu tiên | Điều kiện | Hành động |
| 1 (Cao) | QR code scan | Phát nội dung ngay lập tức và ưu tiên cao nhất, có thể ngắt hàng đợi hiện tại. |
| 2 | POI priority cao + đi vào vùng | Đưa mục nội dung vào đầu hàng đợi phát. |
| 3 | POI thường + đi vào vùng | Đưa mục nội dung vào cuối hàng đợi phát. |
| 4 (Thấp) | User nhấn thủ công | Cho phép phát ngay theo yêu cầu thủ công của người dùng. |



| --- | --- | --- |
| Màn hình | Chức năng | Kỹ thuật chính |
| Splash / Onboarding | Giới thiệu ứng dụng và thu thập các quyền truy cập cần thiết cho định vị và âm thanh. | Permissions API (MAUI Essentials) |
| Map View (Home) | Hiển thị bản đồ, vị trí người dùng, tập hợp POI và điểm được nhấn mạnh theo ngữ cảnh. | MAUI Maps / Google Maps SDK + Custom pins |
| POI List | Cung cấp danh sách POI kèm khoảng cách và trạng thái tiêu thụ nội dung. | CollectionView + SQLite query |
| POI Detail | Trình bày ảnh, mô tả, bộ điều khiển âm thanh và liên kết bản đồ của từng POI. | ScrollView + MediaElement / AVPlayer |
| Audio Player Bar | Mini player luôn hiển thị phía dưới, control phát/dừng | Overlay UI + MediaElement binding |
| QR Scanner | Quét mã QR và kích hoạt ngay nội dung thuyết minh tương ứng. | ZXing.Net.MAUI hoặc Camera MAUI |
| Settings (MỚI) | Cho phép tùy chỉnh ngôn ngữ TTS, tốc độ đọc, bán kính kích hoạt và gói dữ liệu offline. | Preferences API + local storage |
| Offline Pack (MỚI) | Tải trước gói dữ liệu POI và audio để sử dụng trong điều kiện không kết nối mạng. | Background download + SQLite sync |



| --- | --- | --- |
| Tính năng | TTS (Text-to-Speech) | Pre-recorded Audio |
| Chất lượng giọng | Trung bình (Android native) / Cao (Azure) | Rất cao — giọng người thật |
| Dung lượng | Không tốn — tạo realtime | ~500KB–2MB / file mp3 |
| Offline | Native TTS hoạt động offline | Cần tải trước |
| Đa ngôn ngữ | Tốt (thay locale) | Cần thu âm từng ngôn ngữ |
| Recommended Use | POI mới, ngôn ngữ phụ, nội dung động | Phù hợp với POI mới, ngôn ngữ bổ sung và nội dung cần cập nhật linh hoạt. |



| --- | --- | --- |
| Module | Tùy chọn | Mô tả |
| GPS & Bán kính | Độ nhạy GPS: High Accuracy / Battery Saver | Cân bằng giữa mức độ chính xác định vị và mục tiêu tiết kiệm năng lượng. |
|  | Bán kính kích hoạt: 20m / 50m / 100m | Phù hợp với các ngữ cảnh di chuyển khác nhau, chẳng hạn đi bộ và đi xe buýt. |
|  | Chu kỳ GPS: 3s / 5s / 10s | Tần suất lấy tọa độ |
| Giọng TTS | Chọn giọng: Nam / Nữ | Tùy TTS engine hỗ trợ |
|  | Tốc độ đọc: 0.75x – 1.5x | Chậm / Bình thường / Nhanh |
|  | Âm lượng tự động | Tự điều chỉnh volume theo môi trường |
| Gói Offline | Danh sách Tour khả dụng | Hiển thị tour có thể tải trước |
|  | Tải / Xóa gói | Download toàn bộ audio + data cho tour chọn |
|  | Dung lượng hiển thị | Mỗi gói hiện rõ kích thước (VD: "Tour Ẩm thực Đêm - 45 MB") |
| Ngôn ngữ | Đổi ngôn ngữ | vi / en / ko / zh — re-sync translations khi đổi |
|  | Dark Mode | Bản đồ Dark Theme cho di chuyển ban đêm |
|  | Thông báo | Bật/tắt notification khi tiến gần POI |



| --- | --- |
| Cơ chế | Mô tả |
| Full Sync | Lần khởi tạo đầu tiên tải về toàn bộ danh sách POI, bản dịch và tài nguyên âm thanh theo ngôn ngữ được chọn. |
| Delta Sync | Gửi mốc thời gian đồng bộ gần nhất để máy chủ chi trả về các đối tượng mới, thay đổi hoặc bị xóa. |
| Offline Mode | Ứng dụng sử dụng dữ liệu SQLite đã lưu đệm; nếu audio chưa có sẵn thì ưu tiên TTS nội bộ. |
| Conflict Resolution | Áp dụng chiến lược server-wins, trong đó CMS là nguồn sự thật dữ liệu duy nhất. |
| Tải gói Tour | User chọn Tour → download trước toàn bộ audio pack → dùng offline trọn vẹn |
| Analytics offline | Log ghi tạm vào SQLite → auto sync lên server khi có mạng trở lại |
| Dung lượng ước tính | ~50MB cho 50 POI (audio + ảnh) |



| --- | --- | --- | --- | --- |
| Quyền | Super Admin | Content Admin | Editor | Viewer |
| Xem Dashboard | Co | Co | Co | Co |
| Quản lý POI | ✅ | ✅ | Co, gioi han quyen sua | Không |
| Upload Audio | ✅ | ✅ | ✅ | ❌ |
| Quản lý Tour | ✅ | ✅ | ❌ | ❌ |
| Analytics | ✅ | ✅ | ❌ | ✅ (Xem) |
| Quản lý User | ✅ | ❌ | ❌ | Chi xem |
| System Config | ✅ | Không | Không | Không |
| Gen QR Code | ✅ | ✅ | ❌ | ❌ |



| --- | --- |
| Chức năng | Mô tả |
| CRUD Tour | Tạo, cập nhật và vô hiệu hoá các lộ trình tham quan theo chủ đề. |
| Gán POI vào Tour | Sắp xếp thứ tự ghé thăm thông qua thao tác kéo thả hoặc lựa chọn có cấu trúc. |
| Preview tuyến | Cho phép xem trước tuyến tham quan trên bản đồ mini của CMS. |
| Kích hoạt / Vô hiệu | Toggle trạng thái active cho từng tour |



| --- | --- | --- |
| Bảng | Trường chính | Mô tả |
| poi | id, lat, lng, radius, priority | Thực thể điểm tham quan gồm toạ độ và các thông số geofence có liên quan. |
| audio_content | poi_id, language, audio_url | Nội dung âm thanh hoặc kịch bản TTS theo từng ngôn ngữ cua POI. |
| analytics_logs | poi_id, user_anon_id, event_time | Nhật ký sự kiện phát để phục vụ thống kê và cơ chế tránh kích hoạt lặp. |
| tours | id, name, is_active | Nhóm các POI thành tuyến tham quan |
| tour_poi_mapping | tour_id, poi_id, order_index | Liên kết POI với Tour theo thứ tự |
| qr_codes | poi_id, qr_data, expires_at | Dữ liệu QR code, thời gian hết hạn |
| user_location_log | user_anon_id, lat, lng | Lịch sử di chuyển ẩn danh cho heat map |
| admin_users | id, email, role | Tài khoản admin CMS với phân quyền |
| mobile_users | device_id, preferred_lang | Thông tin thiết bị ẩn danh |
| sync_log | device_id, sync_time, sync_type | Lịch sử đồng bộ |



| --- | --- | --- |
| Method | Endpoint | Mô tả |
| GET |  /api/v1/pois  | Truy vấn danh sach POI đang ở trạng thái kích hoạt. |
| GET |  /api/v1/pois/{id}  | Truy vấn chi tiết một POI kèm theo nội dung audio liên quan. |
| GET |  /api/v1/pois/nearby?lat=&lng=&radius=  | Tìm kiếm POI nằm trong bán kính chỉ định theo công thức Haversine. |
| POST |  /api/v1/pois  | [Admin] Tạo POI mới |
| PUT |  /api/v1/pois/{id}  | [Admin] Cập nhật POI |
| DELETE |  /api/v1/pois/{id}  | [Admin] Xóa / vô hiệu hóa POI |
| GET |  /api/v1/audio/{poi_id}?lang=vi  | Lấy file audio URL theo ngôn ngữ |
| POST |  /api/v1/audio/upload  | [Admin] Upload file audio |
| POST |  /api/v1/analytics/play  | Ghi log lượt phát |
| POST |  /api/v1/analytics/location  | Ghi log vị trí ẩn danh (batch) |
| GET |  /api/v1/analytics/dashboard  | [Admin] Dữ liệu dashboard |
| GET |  /api/v1/qr/{poi_id}  | Resolve QR deep link |
| POST |  /api/v1/qr/generate/{poi_id}  | [Admin] Tạo QR code PNG |
| GET |  /api/v1/sync?since=timestamp  | Đồng bộ POI (delta sync) |
| GET |  /api/v1/sync/full  | Đồng bộ toàn bộ (full sync) |
| GET |  /api/v1/tours  | Danh sách Tour active |
| GET |  /api/v1/tours/{id}/pois  | POI trong Tour theo thứ tự |



| --- | --- |
| Tiêu chí | Yêu cầu |
| Thời gian khởi động app | Không quá 3 giây trong trường hợp khởi động nguội. |
| Thời gian trigger → phát audio | Không quá 2 giây tính từ khi người dùng đi vào geofence đến lúc phát nội dung. |
| GPS accuracy required | ≤ 20m cho POI radius 50m |
| Pin consumption (background) | ≤ 5% / giờ (background GPS + geofence) |
| API response time | < 500ms (p95) cho endpoint POI list |
| Delta Sync | ≤ 2 giây (Wi-Fi) / ≤ 5 giây (4G) |
| Offline capability | 100% core features hoạt động không cần internet |
| Dung lượng app | < 80MB (base app), gói offline ~50MB/50 POI |



| --- | --- | --- | --- |
| Môi trường | Mục đích | Database | Ghi chú |
| Development | Phát triển cá nhân va gỡ lỗi chức năng. | SQLite local | Hot reload, debug mode |
| Staging | Kiểm thử tích hợp, UAT và đánh giá geofence. | SQL Server (test) | Dữ liệu mẫu, test geofence |
| Production | Vận hành cho người dùng thực tế. | SQL Server (prod) | HTTPS, backup hàng ngày |



| --- | --- | --- | --- |
| Giai đoạn | Thời gian | Nội dung | Deliverable |
| Phase 1 — POC | Tuần 1–2 | Phát triển GPS tracking, Geofence Engine, TTS cơ bản, SQLite local va Map View. | App chạy được trên Android |
| Phase 2 — MVP | Tuần 3–4 | Bổ sung audio file, queue management, POI detail, offline sync va background GPS. | App hoàn chỉnh iOS + Android |
| Phase 3 — Backend | Tuần 5–6 | REST API, Database, File Storage, JWT Auth, Sync API | Backend deployed |
| Phase 4 — Admin | Tuần 7–8 | Admin CMS, POI management, Audio upload, QR code, Tour management | CMS deployed |
| Phase 5 — Analytics | Tuần 9–10 | Analytics dashboard, Heat map, Play logs, Export | Full system ready |
| Phase 6 — Polish | Tuần 11–12 | Hoàn thiện UI/UX, tối ưu hiệu năng, kiểm thử và tài liệu hoá. | Final demo |



| --- | --- | --- |
| Layer | Công nghệ | Gói / Thư viện |
| Mobile Framework | .NET MAUI 10.0 | Microsoft.Maui, CommunityToolkit.Maui |
| GPS | MAUI Essentials + Native | Microsoft.Maui.Essentials (Geolocation) |
| Maps | Google Maps / MAUI Maps | Microsoft.Maui.Controls.Maps |
| Database (local) | SQLite | sqlite-net-pcl, EF Core + SQLite |
| TTS | Native TTS + Azure | Plugin.Maui.Audio, Azure.AI |
| QR Scanner | ZXing.Net.MAUI | ZXing.Net.Maui hoac BarcodeScanning.Maui |
| HTTP Client | HttpClient + Refit | Refit (typed API), Polly (retry) |
| Backend | ASP.NET Core 10 | Entity Framework Core, AutoMapper |
| Admin CMS | ASP.NET Core MVC | Views + Controllers |
| Database (server) | SQL Server / SQLite | EF Core provider |
| File Storage | Local / Azure Blob | Azure.Storage.Blobs + CDN |



| --- | --- | --- | --- |
| # | Rủi ro | Mức độ | Giải pháp |
| 1 | GPS drift / sai số lớn trong tòa nhà | Cao | Lọc các mẫu vị trí có độ chính xác thấp và tăng bán kính kích hoạt trong không gian trong nhà. |
| 2 | iOS giới hạn background GPS | Cao | Sử dụng significant-change API và hướng dẫn người dùng cấp quyền Always khi cần. |
| 3 | Tốn pin quá nhiều khi tracking liên tục | Trung bình | Điều chỉnh chu kỳ lấy mẫu theo trạng thái chuyển động và ưu tiên Balanced accuracy. |
| 4 | TTS chất lượng thấp trên thiết bị cũ | Trung bình | Ưu tiên pre-recorded audio; Azure TTS fallback khi có mạng |
| 5 | POI trigger sai do GPS bounce | Trung bình | Debounce 30s + cooldown 10 phút + hysteresis threshold |
| 6 | Không có mạng → không sync được | Thấp | Offline-first SQLite; sync khi có mạng trở lại |
| 7 | Dung lượng audio file lớn | Thấp | Encode AAC 64kbps cho speech; lazy download theo khu vực |



| --- | --- | --- |
| Permission | Platform | Lý do |
| ACCESS_FINE_LOCATION | Android | Phục vụ định vị có độ chính xác cao trong chế độ foreground. |
| ACCESS_BACKGROUND_LOCATION | Android 10+ | Hỗ trợ theo dõi vị trí khi ứng dụng hoạt động nền trên Android 10 trở lên. |
| FOREGROUND_SERVICE | Android | Background service liên tục |
| NSLocationAlwaysUsageDescription | iOS | GPS background trên iOS |
| NSCameraUsageDescription | iOS | Cấp quyền camera cho tính năng quét mã QR. |
| CAMERA | Android | Camera cho QR scanner |
| INTERNET | Android | Kết nối API, sync dữ liệu |

