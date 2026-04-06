Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$docxPath = Join-Path $PSScriptRoot 'AudioTourApp_PRD_v2_refined.docx'

if (-not (Test-Path $docxPath)) {
    throw "Khong tim thay file: $docxPath"
}

$zip = [System.IO.Compression.ZipFile]::Open($docxPath, [System.IO.Compression.ZipArchiveMode]::Update)
$entry = $zip.GetEntry('word/document.xml')
if ($null -eq $entry) {
    $zip.Dispose()
    throw 'Khong tim thay word/document.xml.'
}

$reader = New-Object System.IO.StreamReader($entry.Open())
$text = $reader.ReadToEnd()
$reader.Close()

$replacements = [ordered]@{
    '.NET MAUI 8.0 (Android / iOS)' = '.NET MAUI 10.0 (Android / iOS)'
    '.NET MAUI 8.0 cho Android (API 26+) va iOS (14+).' = '.NET MAUI 10.0 cho Android (API 31+) và iOS (14+).'
    '.NET MAUI 8.0' = '.NET MAUI 10.0'
    'ASP.NET Core 8' = 'ASP.NET Core 10'

    'Nen tang trien khai' = 'Nền tảng triển khai'
    'Ung dung khach tren Android va iOS, dam nhiem dinh vi, geofence, phat thuyet minh, ban do va quet QR.' = 'Ứng dụng khách trên Android và iOS, đảm nhiệm định vị, geofence, phát thuyết minh, bản đồ và quét QR.'
    'REST API — CRUD POI, Analytics, Auth, Sync' = 'REST API, hỗ trợ CRUD POI, Analytics, Auth và Sync'
    'Mat khau tren he thong CMS duoc bao ve bang co che bam BCrypt kem salt.' = 'Mật khẩu trên hệ thống CMS được bảo vệ bằng cơ chế băm BCrypt kèm salt.'
    'Cao' = 'Cao'
    'Khong' = 'Không'
    '✅ (Xem)' = '✅ (Xem)'
    'Cho phep tai len tep am thanh theo co che keo tha, dong thoi chuyen ma ve dinh dang AAC hoac MP3 128 kbps.' = 'Cho phép tải lên tệp âm thanh theo cơ chế kéo thả, đồng thời chuyển mã về định dạng AAC hoặc MP3 128 kbps.'
    'Ho tro nghe thu truc tiep trong trinh duyet nham thuan tien cho quy trinh tham dinh noi dung.' = 'Hỗ trợ nghe thử trực tiếp trong trình duyệt nhằm thuận tiện cho quy trình thẩm định nội dung.'
    'Quan ly phien ban noi dung am thanh theo nhieu lan phat hanh va ho tro quay lui khi can thiet.' = 'Quản lý phiên bản nội dung âm thanh theo nhiều lần phát hành và hỗ trợ quay lui khi cần thiết.'
    'Lien ket tai nguyen am thanh voi tung POI va ngon ngu cu the de bao dam tinh nhat quan trong phan phoi noi dung.' = 'Liên kết tài nguyên âm thanh với từng POI và ngôn ngữ cụ thể để bảo đảm tính nhất quán trong phân phối nội dung.'
    'Cung cap chuc nang tao ban xem truoc TTS ngay tren giao dien web thong qua cac dich vu Azure hoac Google.' = 'Cung cấp chức năng tạo bản xem trước TTS ngay trên giao diện web thông qua các dịch vụ Azure hoặc Google.'
    'Tong hop cac chi so nhu so luot phat, thoi gian nghe trung binh va diem dung nghe de phuc vu phan tich.' = 'Tổng hợp các chỉ số như số lượt phát, thời gian nghe trung bình và điểm dừng nghe để phục vụ phân tích.'
    'Ban do nhiet the hien mat do xuat hien cua nguoi dung theo khu vuc tren co so du lieu an danh.' = 'Bản đồ nhiệt thể hiện mật độ xuất hiện của người dùng theo khu vực trên cơ sở dữ liệu ẩn danh.'
    'Thong ke 10 POI co tan suat nghe cao nhat trong cac cua so thoi gian 7 ngay va 30 ngay.' = 'Thống kê 10 POI có tần suất nghe cao nhất trong các cửa sổ thời gian 7 ngày và 30 ngày.'
    'Tinh toan thoi gian nghe trung binh doi voi tung POI.' = 'Tính toán thời gian nghe trung bình đối với từng POI.'
    'Phan loai luot kich hoat theo co che GPS, QR va thao tac thu cong.' = 'Phân loại lượt kích hoạt theo cơ chế GPS, QR và thao tác thủ công.'
    'Cung cap bieu do theo doi tan suat nghe theo gio, ngay va tuan.' = 'Cung cấp biểu đồ theo dõi tần suất nghe theo giờ, ngày và tuần.'
    'Do luong ty le hoan tat noi dung am thanh, phan anh muc do tiep nhan cua nguoi dung.' = 'Đo lường tỷ lệ hoàn tất nội dung âm thanh, phản ánh mức độ tiếp nhận của người dùng.'
    'Ho tro xuat bao cao du lieu duoi cac dinh dang CSV va Excel.' = 'Hỗ trợ xuất báo cáo dữ liệu dưới các định dạng CSV và Excel.'
    'Cho phep chon POI de sinh ma QR duoi dang PNG hoac SVG.' = 'Cho phép chọn POI để sinh mã QR dưới dạng PNG hoặc SVG.'
    'Ho tro tao mau in ma QR kem logo, ten diem va khung trinh bay phu hop voi an pham truyen thong.' = 'Hỗ trợ tạo mẫu in mã QR kèm logo, tên điểm và khung trình bày phù hợp với ấn phẩm truyền thông.'
    'Gan ma QR voi cac vi tri vat ly cu the, nhu tram xe buyt hoac bang thong tin tai diem den.' = 'Gắn mã QR với các vị trí vật lý cụ thể, như trạm xe buýt hoặc bảng thông tin tại điểm đến.'
    'Ma QR co the chua deep link den doi tuong POI, cho phep mo ung dung va kich hoat phat noi dung ngay lap tuc.' = 'Mã QR có thể chứa deep link đến đối tượng POI, cho phép mở ứng dụng và kích hoạt phát nội dung ngay lập tức.'
    'So luot quet QR duoc thu thap trong he thong phan tich de phuc vu danh gia muc do tiep can.' = 'Số lượt quét QR được thu thập trong hệ thống phân tích để phục vụ đánh giá mức độ tiếp cận.'
    'Ho tro xuat tep voi kich thuoc phu hop cho nhu cau in an kho A4 va A3.' = 'Hỗ trợ xuất tệp với kích thước phù hợp cho nhu cầu in ấn khổ A4 và A3.'
    'Du lieu vi tri duoc xu ly theo nguyen tac an danh hoa, khong lien ket truc tiep voi danh tinh ca nhan.' = 'Dữ liệu vị trí được xử lý theo nguyên tắc ẩn danh hóa, không liên kết trực tiếp với danh tính cá nhân.'
    'Cac API quan tri su dung co che xac thuc JWT Bearer Token.' = 'Các API quản trị sử dụng cơ chế xác thực JWT Bearer Token.'
    'Du lieu GPS phat sinh trong trang thai offline duoc luu tam thoi tren SQLite va dong bo lai khi co ket noi.' = 'Dữ liệu GPS phát sinh trong trạng thái offline được lưu tạm thời trên SQLite và đồng bộ lại khi có kết nối.'
    'He thong huong toi tuan thu cac nguyen tac GDPR va PDPA, cho phep nguoi dung tu choi thu thap du lieu phan tich.' = 'Hệ thống hướng tới tuân thủ các nguyên tắc GDPR và PDPA, cho phép người dùng từ chối thu thập dữ liệu phân tích.'
    'Co so du lieu SQLite tren thiet bi co the duoc ma hoa bang SQLCipher trong cac kich ban yeu cau muc bao mat cao hon.' = 'Cơ sở dữ liệu SQLite trên thiết bị có thể được mã hóa bằng SQLCipher trong các kịch bản yêu cầu mức bảo mật cao hơn.'
    'Tai khoan Super Admin tren CMS co the duoc tang cuong bao mat bang co che xac thuc hai yeu to.' = 'Tài khoản Super Admin trên CMS có thể được tăng cường bảo mật bằng cơ chế xác thực hai yếu tố.'
    'He thong duoc ky vong ho tro toi thieu 500 POI hien thi dong thoi tren ban do.' = 'Hệ thống được kỳ vọng hỗ trợ tối thiểu 500 POI hiển thị đồng thời trên bản đồ.'
    'Backend can dap ung nang luc xu ly toi thieu 100 yeu cau moi giay trong dieu kien dong thoi.' = 'Backend cần đáp ứng năng lực xử lý tối thiểu 100 yêu cầu mỗi giây trong điều kiện đồng thời.'
    'He thong phan tich can xu ly toi thieu 10.000 ban ghi su kien moi ngay.' = 'Hệ thống phân tích cần xử lý tối thiểu 10.000 bản ghi sự kiện mỗi ngày.'
    'Kien truc cho phep mo rong sang nhieu khu vuc dia ly thong qua viec bo sung POI ma khong can phat hanh lai ung dung.' = 'Kiến trúc cho phép mở rộng sang nhiều khu vực địa lý thông qua việc bổ sung POI mà không cần phát hành lại ứng dụng.'
    'Mo hinh du lieu va noi dung cho phep mo rong ngon ngu moi ma han che toi da can thiep vao ma nguon.' = 'Mô hình dữ liệu và nội dung cho phép mở rộng ngôn ngữ mới mà hạn chế tối đa can thiệp vào mã nguồn.'
    'Tang TTS duoc thiet ke theo huong co the thay doi nha cung cap dich vu, vi du Azure hoac Google Cloud TTS.' = 'Tầng TTS được thiết kế theo hướng có thể thay đổi nhà cung cấp dịch vụ, ví dụ Azure hoặc Google Cloud TTS.'
    'Ung dung duy tri muc do kha dung cao trong che do offline doi voi pham vi du lieu da duoc luu dem.' = 'Ứng dụng duy trì mức độ khả dụng cao trong chế độ offline đối với phạm vi dữ liệu đã được lưu đệm.'
    'Giao dien can bao dam kich thuoc chu va do tuong phan phu hop voi khuyen nghi WCAG AA.' = 'Giao diện cần bảo đảm kích thước chữ và độ tương phản phù hợp với khuyến nghị WCAG AA.'
    'He thong huong toi kha nang tuong thich voi cac trinh doc man hinh nhu TalkBack va VoiceOver.' = 'Hệ thống hướng tới khả năng tương thích với các trình đọc màn hình như TalkBack và VoiceOver.'
    'Giao dien duoc thiet ke dap ung cho nhieu kich thuoc man hinh, tu dien thoai thong minh den may tinh bang.' = 'Giao diện được thiết kế đáp ứng cho nhiều kích thước màn hình, từ điện thoại thông minh đến máy tính bảng.'
    'Ma nguon duoc dong bo len kho Git, vi du GitHub hoac Azure DevOps.' = 'Mã nguồn được đồng bộ lên kho Git, ví dụ GitHub hoặc Azure DevOps.'
    'Quy trinh xay dung va kiem thu duoc tu dong hoa, bao gom bien dich va chay cac bo kiem thu don vi.' = 'Quy trình xây dựng và kiểm thử được tự động hóa, bao gồm biên dịch và chạy các bộ kiểm thử đơn vị.'
    'Ban phat hanh thu nghiem duoc trien khai tu dong len moi truong staging.' = 'Bản phát hành thử nghiệm được triển khai tự động lên môi trường staging.'
    'Ban phat hanh chinh thuc yeu cau buoc phe duyet truoc khi trien khai len moi truong production.' = 'Bản phát hành chính thức yêu cầu bước phê duyệt trước khi triển khai lên môi trường production.'
    'Bieu mau tao moi va cap nhat POI bao gom cac truong thong tin sau:' = 'Biểu mẫu tạo mới và cập nhật POI bao gồm các trường thông tin sau:'
    'Khoang cach giua hai diem GPS tren be mat Trai Dat duoc tinh theo cong thuc Haversine nhu sau:' = 'Khoảng cách giữa hai điểm GPS trên bề mặt Trái Đất được tính theo công thức Haversine như sau:'
    'Vi du cai dat bang C#:' = 'Ví dụ cài đặt bằng C#:'
    'URI Scheme: audiotour://poi/{poi_id}. Universal Link và App Link sử dụng dạng https://audiotour.app/poi/{poi_id}. Ví dụ mã QR: audiotour://poi/khanh-hoi-ben-tau. Luồng xử lý được xác lập theo chuỗi: quét QR, hệ thống chuyển hướng đến ứng dụng nếu đã cài đặt hoặc đến kho ứng dụng nếu chưa có, sau đó ứng dụng tiếp nhận deep link và kích hoạt nội dung audio tương ứng.' = 'URI Scheme: audiotour://poi/{poi_id}. Universal Link và App Link sử dụng dạng https://audiotour.app/poi/{poi_id}. Ví dụ mã QR: audiotour://poi/khanh-hoi-ben-tau. Luồng xử lý được xác lập theo chuỗi: quét QR, hệ thống chuyển hướng đến ứng dụng nếu đã cài đặt hoặc đến kho ứng dụng nếu chưa có, sau đó ứng dụng tiếp nhận deep link và kích hoạt nội dung audio tương ứng.'
}

foreach ($key in $replacements.Keys) {
    $text = $text.Replace($key, $replacements[$key])
}

$entry.Delete()
$newEntry = $zip.CreateEntry('word/document.xml')
$writer = New-Object System.IO.StreamWriter($newEntry.Open())
$writer.Write($text)
$writer.Close()
$zip.Dispose()

Write-Output "Updated versions and Vietnamese text in: $docxPath"
