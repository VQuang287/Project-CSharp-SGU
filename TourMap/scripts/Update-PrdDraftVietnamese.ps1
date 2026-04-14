[CmdletBinding()]
param(
    [string]$DocumentPath = "_docs\PRD-nháp-2.docx"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceDocx = Join-Path $repoRoot $DocumentPath
$tempDir = Join-Path $repoRoot ".tmp_prd_docx_vn"
$tempZip = Join-Path $repoRoot ".tmp_prd_docx_vn.zip"
$xmlPath = Join-Path $tempDir "word\document.xml"
$wNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

if (-not (Test-Path $sourceDocx)) {
    throw "Không tìm thấy tài liệu: $sourceDocx"
}

if (Test-Path $tempDir) {
    Remove-Item -LiteralPath $tempDir -Recurse -Force
}

if (Test-Path $tempZip) {
    Remove-Item -LiteralPath $tempZip -Force
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
[System.IO.Compression.ZipFile]::ExtractToDirectory($sourceDocx, $tempDir)

$xml = [xml](Get-Content -LiteralPath $xmlPath -Raw)
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace("w", $wNs)

function Get-ParagraphText {
    param([System.Xml.XmlNode]$Paragraph)
    $texts = $Paragraph.SelectNodes(".//w:t", $ns) | ForEach-Object { $_.InnerText }
    return ($texts -join "")
}

function New-TextNode {
    param(
        [string]$Text,
        [bool]$Bold = $false
    )

    $run = $xml.CreateElement("w", "r", $wNs)
    if ($Bold) {
        $rPr = $xml.CreateElement("w", "rPr", $wNs)
        $b = $xml.CreateElement("w", "b", $wNs)
        $rPr.AppendChild($b) | Out-Null
        $run.AppendChild($rPr) | Out-Null
    }

    $t = $xml.CreateElement("w", "t", $wNs)
    if ($Text.StartsWith(" ") -or $Text.EndsWith(" ")) {
        $spaceAttr = $xml.CreateAttribute("xml", "space", "http://www.w3.org/XML/1998/namespace")
        $spaceAttr.Value = "preserve"
        $t.Attributes.Append($spaceAttr) | Out-Null
    }
    $t.InnerText = $Text
    $run.AppendChild($t) | Out-Null
    return $run
}

function New-Paragraph {
    param(
        [string]$Text,
        [string]$Style = "",
        [bool]$Bold = $false
    )

    $p = $xml.CreateElement("w", "p", $wNs)
    if (-not [string]::IsNullOrWhiteSpace($Style)) {
        $pPr = $xml.CreateElement("w", "pPr", $wNs)
        $pStyle = $xml.CreateElement("w", "pStyle", $wNs)
        $val = $xml.CreateAttribute("w", "val", $wNs)
        $val.Value = $Style
        $pStyle.Attributes.Append($val) | Out-Null
        $pPr.AppendChild($pStyle) | Out-Null
        $p.AppendChild($pPr) | Out-Null
    }

    $p.AppendChild((New-TextNode -Text $Text -Bold:$Bold)) | Out-Null
    return $p
}

function New-TableCell {
    param(
        [string]$Text,
        [bool]$Bold = $false
    )

    $tc = $xml.CreateElement("w", "tc", $wNs)
    $tcPr = $xml.CreateElement("w", "tcPr", $wNs)
    $tcW = $xml.CreateElement("w", "tcW", $wNs)
    $wAttr = $xml.CreateAttribute("w", "w", $wNs)
    $wAttr.Value = "0"
    $tcW.Attributes.Append($wAttr) | Out-Null
    $typeAttr = $xml.CreateAttribute("w", "type", $wNs)
    $typeAttr.Value = "auto"
    $tcW.Attributes.Append($typeAttr) | Out-Null
    $tcPr.AppendChild($tcW) | Out-Null
    $tc.AppendChild($tcPr) | Out-Null
    $tc.AppendChild((New-Paragraph -Text $Text -Bold:$Bold)) | Out-Null
    return $tc
}

function New-Table {
    param(
        [string[]]$Headers,
        [object[]]$Rows
    )

    $tbl = $xml.CreateElement("w", "tbl", $wNs)
    $tblPr = $xml.CreateElement("w", "tblPr", $wNs)
    $tblStyle = $xml.CreateElement("w", "tblStyle", $wNs)
    $styleAttr = $xml.CreateAttribute("w", "val", $wNs)
    $styleAttr.Value = "TableGrid"
    $tblStyle.Attributes.Append($styleAttr) | Out-Null
    $tblPr.AppendChild($tblStyle) | Out-Null
    $tbl.AppendChild($tblPr) | Out-Null

    $headerRow = $xml.CreateElement("w", "tr", $wNs)
    foreach ($header in $Headers) {
        $headerRow.AppendChild((New-TableCell -Text $header -Bold:$true)) | Out-Null
    }
    $tbl.AppendChild($headerRow) | Out-Null

    foreach ($row in $Rows) {
        $tr = $xml.CreateElement("w", "tr", $wNs)
        foreach ($cellText in $row) {
            $tr.AppendChild((New-TableCell -Text ([string]$cellText))) | Out-Null
        }
        $tbl.AppendChild($tr) | Out-Null
    }

    return $tbl
}

function TryFind-ParagraphByPrefix {
    param([string]$Prefix)
    foreach ($paragraph in $xml.SelectNodes("//w:body/w:p", $ns)) {
        if ((Get-ParagraphText $paragraph).StartsWith($Prefix)) {
            return $paragraph
        }
    }
    return $null
}

function Find-ParagraphByRegex {
    param([string]$Pattern)
    foreach ($paragraph in $xml.SelectNodes("//w:body/w:p", $ns)) {
        if ((Get-ParagraphText $paragraph) -match $Pattern) {
            return $paragraph
        }
    }
    throw "Không tìm thấy đoạn phù hợp với mẫu: $Pattern"
}

function Insert-NodesBefore {
    param(
        [System.Xml.XmlNode]$Anchor,
        [System.Collections.Generic.List[System.Xml.XmlNode]]$Nodes
    )
    foreach ($node in $Nodes) {
        $imported = $xml.ImportNode($node, $true)
        $Anchor.ParentNode.InsertBefore($imported, $Anchor) | Out-Null
    }
}

function Remove-BlockIfExists {
    param(
        [string]$StartPrefix,
        [string]$EndPattern = "",
        [string[]]$EndPrefixes = @()
    )

    $startNode = TryFind-ParagraphByPrefix -Prefix $StartPrefix
    if ($null -eq $startNode) {
        return
    }

    $current = $startNode
    while ($null -ne $current) {
        if ($current.LocalName -eq "p") {
            $currentText = Get-ParagraphText $current
            if ((-not [string]::IsNullOrWhiteSpace($EndPattern)) -and ($currentText -match $EndPattern)) {
                break
            }
            foreach ($endPrefix in $EndPrefixes) {
                if ($currentText.StartsWith($endPrefix)) {
                    return
                }
            }
        }

        $next = $current.NextSibling
        $current.ParentNode.RemoveChild($current) | Out-Null
        $current = $next
    }
}

$modules = @(
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Xác thực quản trị viên"; Purpose = "Đăng nhập, đăng xuất và kiểm soát phiên làm việc của quản trị viên."; Files = "AccountController.cs; AdminUser.cs; AdminDbContext.cs"; Status = "Đã triển khai";
        Classes = @(
            @("AccountController", "Xử lý đăng nhập, đăng xuất và xác thực cookie", "Làm việc với AdminDbContext và PasswordHasher"),
            @("AdminUser", "Lưu tài khoản quản trị, vai trò và trạng thái khóa", "Được AccountController truy vấn"),
            @("AdminDbContext", "Truy cập bảng người dùng quản trị", "Cấp dữ liệu cho controller")
        );
        Diagram = @("AccountController -> AdminDbContext -> AdminUser", "AccountController -> PasswordHasher<AdminUser>", "AccountController -> CookieAuthentication");
        Actors = "Quản trị viên"; Input = "Tên đăng nhập, mật khẩu"; Output = "Phiên đăng nhập hợp lệ"; Conditions = "Tài khoản hoạt động và mật khẩu đúng";
        Flow = @("Quản trị viên -> AccountController.Login", "AccountController -> AdminDbContext: tìm AdminUser", "AccountController -> PasswordHasher: xác thực mật khẩu", "AccountController -> CookieAuth: tạo phiên", "Hệ thống -> Quản trị viên: chuyển vào dashboard")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Bảng điều khiển tổng quan"; Purpose = "Hiển thị KPI hệ thống và các chỉ số vận hành chính."; Files = "HomeController.cs; AdminDashboardViewModel.cs; AdminDbContext.cs"; Status = "Đã triển khai";
        Classes = @(
            @("HomeController", "Tổng hợp dữ liệu dashboard", "Đọc dữ liệu từ AdminDbContext"),
            @("AdminDashboardViewModel", "Đóng gói KPI để hiển thị", "Nhận dữ liệu từ HomeController"),
            @("PoiPlaybackItem", "Lưu POI nổi bật theo lượt nghe", "Nằm trong ViewModel")
        );
        Diagram = @("HomeController -> AdminDbContext", "HomeController -> AdminDashboardViewModel", "AdminDashboardViewModel -> PoiPlaybackItem");
        Actors = "Quản trị viên"; Input = "Yêu cầu mở trang tổng quan"; Output = "Dashboard KPI và top POI"; Conditions = "Quản trị viên đã đăng nhập";
        Flow = @("Quản trị viên -> HomeController.Index", "HomeController -> AdminDbContext: đếm POI, tour, user, QR, lượt phát", "HomeController -> AdminDashboardViewModel: đóng gói dữ liệu", "HomeController -> View: hiển thị dashboard")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quản lý điểm tham quan (POI)"; Purpose = "Tạo, sửa, xóa và cập nhật nội dung POI."; Files = "PoisController.cs; Poi.cs; AdminDbContext.cs"; Status = "Đã triển khai";
        Classes = @(
            @("PoisController", "Thực hiện CRUD POI và quản lý trạng thái hiển thị", "Làm việc với AdminDbContext"),
            @("Poi", "Mô hình dữ liệu điểm tham quan", "Được lưu trong SQLite và đồng bộ sang mobile"),
            @("AdminDbContext", "Truy cập dữ liệu POI", "Cấp dữ liệu cho controller")
        );
        Diagram = @("Admin View -> PoisController -> AdminDbContext -> Poi", "PoisController -> IWebHostEnvironment -> wwwroot/uploads", "PoisController -> AITranslationService (tùy chọn)");
        Actors = "Quản trị viên"; Input = "Thông tin POI, ảnh, âm thanh"; Output = "POI mới hoặc bản cập nhật POI"; Conditions = "Dữ liệu hợp lệ";
        Flow = @("Quản trị viên -> PoisController.Create/Edit", "PoisController -> tải ảnh và âm thanh lên wwwroot/uploads", "PoisController -> AdminDbContext: thêm hoặc cập nhật POI", "Hệ thống -> Quản trị viên: quay lại danh sách POI")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Dịch thuật AI và tạo TTS"; Purpose = "Tự động dịch mô tả POI và sinh âm thanh đa ngôn ngữ."; Files = "AITranslationService.cs; PoisController.cs; Poi.cs"; Status = "Đã triển khai";
        Classes = @(
            @("AITranslationService", "Gọi AI để dịch và sinh file TTS", "Được PoisController sử dụng"),
            @("PoisController", "Kích hoạt luồng dịch/tạo TTS khi cần", "Nhận kết quả từ AITranslationService"),
            @("Poi", "Lưu mô tả và URL âm thanh theo từng ngôn ngữ", "Được cập nhật sau khi xử lý AI")
        );
        Diagram = @("PoisController -> AITranslationService -> văn bản đã dịch", "AITranslationService -> wwwroot/uploads/audio", "AITranslationService -> các trường ngôn ngữ của Poi");
        Actors = "Quản trị viên"; Input = "Mô tả tiếng Việt và tùy chọn Auto AI"; Output = "Bản dịch và file âm thanh"; Conditions = "POI có nội dung mô tả";
        Flow = @("Quản trị viên -> PoisController(Create/Edit, autoAI=true)", "PoisController -> AITranslationService: gửi nội dung mô tả", "AITranslationService -> sinh TTS cho từng ngôn ngữ", "PoisController -> Poi: lưu Description* và AudioUrl*", "AdminDbContext -> SQLite: lưu thay đổi")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quản lý tour"; Purpose = "Tạo tour và gán POI theo thứ tự tham quan."; Files = "ToursController.cs; Tour.cs; TourPoiMapping.cs; TourEditViewModel.cs"; Status = "Đã triển khai";
        Classes = @(
            @("ToursController", "CRUD tour và quản lý danh sách POI trong tour", "Dùng AdminDbContext"),
            @("Tour", "Lưu thông tin tour", "Được lưu trong bảng Tours"),
            @("TourPoiMapping", "Liên kết POI với tour theo thứ tự", "Nối Tour và Poi")
        );
        Diagram = @("Admin View -> ToursController -> TourEditViewModel", "ToursController -> AdminDbContext -> Tour", "ToursController -> AdminDbContext -> TourPoiMapping");
        Actors = "Quản trị viên"; Input = "Tên tour, mô tả, danh sách POI"; Output = "Tour và thứ tự POI"; Conditions = "Dữ liệu hợp lệ";
        Flow = @("Quản trị viên -> ToursController.Create/Edit", "ToursController -> AdminDbContext: lưu Tour", "ToursController -> AdminDbContext: thêm/xóa TourPoiMapping", "Hệ thống -> Quản trị viên: cập nhật danh sách tour")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quản lý người dùng di động"; Purpose = "Xem, lọc, thay đổi vai trò và xử lý tài khoản mobile."; Files = "UsersController.cs; MobileUser.cs; PlaybackHistory.cs"; Status = "Đã triển khai";
        Classes = @(
            @("UsersController", "Quản lý tài khoản người dùng trên CMS", "Đọc/ghi MobileUser"),
            @("MobileUser", "Lưu thông tin tài khoản người dùng", "Được CMS và Auth API sử dụng"),
            @("PlaybackHistory", "Lưu lịch sử nghe để hỗ trợ thống kê", "Được dùng khi xem chi tiết user")
        );
        Diagram = @("UsersController -> AdminDbContext -> MobileUser", "UsersController -> PlaybackHistory", "UsersController -> PasswordHasher<MobileUser>");
        Actors = "Quản trị viên"; Input = "Từ khóa tìm kiếm, hành động trên user"; Output = "Danh sách user và kết quả xử lý"; Conditions = "Quản trị viên có quyền phù hợp";
        Flow = @("Quản trị viên -> UsersController.Index/Details", "UsersController -> AdminDbContext: truy vấn MobileUser", "Quản trị viên -> UsersController.ChangeRole/ResetPassword/Delete", "AdminDbContext -> SQLite: lưu thay đổi", "Hệ thống -> Quản trị viên: thông báo kết quả")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quản lý mã QR"; Purpose = "Sinh QR deep-link cho POI và lưu lịch sử tạo mã."; Files = "QrController.cs; QrCodeEntry.cs; Poi.cs"; Status = "Đã triển khai";
        Classes = @(
            @("QrController", "Hiển thị danh sách và tạo mã QR", "Làm việc với AdminDbContext"),
            @("QrCodeEntry", "Lưu deep-link, ảnh QR và thời gian tạo", "Được CMS sử dụng"),
            @("Poi", "Nguồn dữ liệu để sinh deep-link", "Liên kết với QR")
        );
        Diagram = @("Admin View -> QrController -> AdminDbContext", "QrController -> Poi", "QrController -> QrCodeEntry");
        Actors = "Quản trị viên"; Input = "POI cần tạo QR"; Output = "Ảnh QR và deep-link"; Conditions = "POI tồn tại";
        Flow = @("Quản trị viên -> QrController.Generate", "QrController -> AdminDbContext: tải POI", "QrController -> tạo deep-link và qrImageUrl", "QrController -> AdminDbContext: lưu QrCodeEntry", "Hệ thống -> Quản trị viên: hiển thị QR trong danh sách")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Phân tích và báo cáo"; Purpose = "Tổng hợp dữ liệu nghe audio, vị trí và xuất báo cáo CSV."; Files = "AnalyticsController.cs; AnalyticsDashboardViewModel.cs; PlaybackHistory.cs; UserLocationLog.cs"; Status = "Đã triển khai";
        Classes = @(
            @("AnalyticsController", "Tổng hợp metric và xuất báo cáo", "Đọc PlaybackHistory và UserLocationLog"),
            @("AnalyticsDashboardViewModel", "Đóng gói dữ liệu biểu đồ và heatmap", "Nhận dữ liệu từ controller"),
            @("UserLocationLog", "Lưu điểm vị trí để phục vụ heatmap", "Dùng cho phân tích hành vi")
        );
        Diagram = @("AnalyticsController -> AdminDbContext", "AnalyticsController -> AnalyticsDashboardViewModel", "AnalyticsDashboardViewModel -> PlaybackHistory/UserLocationLog");
        Actors = "Quản trị viên"; Input = "Yêu cầu xem analytics hoặc xuất báo cáo"; Output = "Dashboard phân tích hoặc file CSV"; Conditions = "Quản trị viên đã đăng nhập";
        Flow = @("Quản trị viên -> AnalyticsController.Index/ExportCsv", "AnalyticsController -> AdminDbContext: truy vấn PlaybackHistory và UserLocationLog", "AnalyticsController -> AnalyticsDashboardViewModel: tổng hợp dữ liệu", "Hệ thống -> Quản trị viên: hiển thị biểu đồ hoặc trả file CSV")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Khởi động và đa ngôn ngữ"; Purpose = "Chọn ngôn ngữ ban đầu và cập nhật giao diện theo ngôn ngữ."; Files = "SplashPage.cs; LocalizationService.cs; AppShell.xaml.cs"; Status = "Đã triển khai";
        Classes = @(
            @("SplashPage", "Màn hình chọn ngôn ngữ ban đầu", "Cập nhật LocalizationService"),
            @("LocalizationService", "Quản lý ngôn ngữ và sự kiện thay đổi", "Được toàn bộ ứng dụng sử dụng"),
            @("AppShell", "Cập nhật nhãn tab và route theo ngôn ngữ", "Nhận sự kiện LanguageChanged")
        );
        Diagram = @("SplashPage -> LocalizationService", "LocalizationService -> LanguageChanged event", "AppShell/Pages -> LocalizationService");
        Actors = "Người dùng"; Input = "Ngôn ngữ được chọn"; Output = "Giao diện đã đổi ngôn ngữ"; Conditions = "Ứng dụng đang chạy";
        Flow = @("Người dùng -> SplashPage: chọn ngôn ngữ", "SplashPage -> LocalizationService: đặt CurrentLanguage", "LocalizationService -> pages/tabs: phát sự kiện LanguageChanged", "Hệ thống -> Người dùng: cập nhật giao diện")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Xác thực người dùng"; Purpose = "Đăng nhập, đăng ký, guest mode và quản lý phiên trên mobile."; Files = "AuthService.cs; LoginPage.cs; RegisterPage.cs; AuthApiController.cs"; Status = "Đã triển khai";
        Classes = @(
            @("LoginPage", "Thu thập thông tin đăng nhập hoặc guest mode", "Gọi AuthService"),
            @("RegisterPage", "Đăng ký tài khoản mới", "Gọi AuthService"),
            @("AuthService", "Quản lý API xác thực và SecureStorage", "Làm việc với AuthApiController")
        );
        Diagram = @("LoginPage/RegisterPage -> AuthService", "AuthService -> AuthApiController -> AdminDbContext -> MobileUser", "AuthService -> SecureStorage");
        Actors = "Người dùng"; Input = "Email, mật khẩu hoặc guest login"; Output = "Phiên đăng nhập và hồ sơ người dùng"; Conditions = "Kết nối API xác thực hoạt động";
        Flow = @("Người dùng -> LoginPage/RegisterPage", "UI -> AuthService", "AuthService -> /api/v1/auth", "AuthApiController -> MobileUser: xác thực hoặc tạo mới", "AuthService -> SecureStorage: lưu token và profile", "Hệ thống -> AppShell: vào ứng dụng chính")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Bản đồ và GPS"; Purpose = "Hiển thị bản đồ, POI và vị trí hiện tại của người dùng."; Files = "MapPage.xaml.cs; TourRuntimeService.cs; GpsTrackingService_Android.cs"; Status = "Đã triển khai";
        Classes = @(
            @("MapPage", "Render bản đồ và marker POI", "Nghe TourRuntimeService"),
            @("TourRuntimeService", "Điều phối dữ liệu GPS và runtime tour", "Làm việc với GeofenceEngine"),
            @("GpsTrackingService_Android", "Lấy tọa độ GPS liên tục", "Cấp vị trí cho runtime")
        );
        Diagram = @("MapPage -> TourRuntimeService", "TourRuntimeService -> GpsTrackingService_Android", "MapPage -> Mapsui MapControl");
        Actors = "Người dùng"; Input = "Mở bản đồ và cấp quyền vị trí"; Output = "Bản đồ, marker POI và vị trí hiện tại"; Conditions = "GPS và quyền truy cập sẵn sàng";
        Flow = @("Người dùng -> MapPage", "MapPage -> TourRuntimeService.InitializeAsync", "TourRuntimeService -> GPS service: lấy tọa độ", "MapPage -> Mapsui: vẽ marker POI và current location", "Người dùng -> chạm marker -> mở preview hoặc chi tiết")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Khám phá POI và chi tiết"; Purpose = "Tìm kiếm, lọc và xem chi tiết điểm tham quan."; Files = "PoiListPage.xaml.cs; PoiDetailPage.cs; DatabaseService.cs; Poi.cs"; Status = "Đã triển khai";
        Classes = @(
            @("PoiListPage", "Hiển thị danh sách, tìm kiếm và lọc POI", "Đọc dữ liệu từ DatabaseService"),
            @("PoiDetailPage", "Hiển thị mô tả và điều khiển phát audio", "Đọc dữ liệu một POI"),
            @("DatabaseService", "Truy vấn SQLite cục bộ", "Cấp dữ liệu cho list/detail")
        );
        Diagram = @("PoiListPage -> DatabaseService -> Poi", "PoiListPage -> PoiDetailPage", "PoiDetailPage -> DatabaseService -> Poi");
        Actors = "Người dùng"; Input = "Từ khóa tìm kiếm, bộ lọc, lựa chọn POI"; Output = "Danh sách lọc và trang chi tiết"; Conditions = "Cơ sở dữ liệu cục bộ đã có dữ liệu";
        Flow = @("Người dùng -> PoiListPage", "PoiListPage -> DatabaseService: tải POI", "Người dùng -> tìm kiếm hoặc lọc", "PoiListPage -> CollectionView: cập nhật danh sách", "Người dùng -> chọn POI", "Ứng dụng -> PoiDetailPage: hiển thị thông tin chi tiết")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Geofence và phát âm thanh"; Purpose = "Tự động xác định POI gần nhất và phát audio thuyết minh."; Files = "GeofenceEngine.cs; NarrationEngine.cs; AudioPlayerService.cs; TourRuntimeService.cs"; Status = "Đã triển khai";
        Classes = @(
            @("GeofenceEngine", "Tính khoảng cách Haversine và kiểm tra bán kính", "Được TourRuntimeService gọi"),
            @("NarrationEngine", "Điều phối trạng thái phát âm thanh", "Làm việc với AudioPlayerService"),
            @("AudioPlayerService", "Phát và dừng âm thanh cục bộ", "Thực thi playback")
        );
        Diagram = @("TourRuntimeService -> GeofenceEngine", "TourRuntimeService -> NarrationEngine -> AudioPlayerService", "NarrationEngine -> DatabaseService/local audio");
        Actors = "Người dùng đang di chuyển"; Input = "Tọa độ GPS mới"; Output = "Âm thanh POI được phát"; Conditions = "Thỏa bán kính, debounce và cooldown";
        Flow = @("Cập nhật GPS -> TourRuntimeService", "TourRuntimeService -> GeofenceEngine: tìm POI gần nhất", "GeofenceEngine -> kiểm tra radius/debounce/cooldown", "TourRuntimeService -> NarrationEngine.OnPOITriggeredAsync", "NarrationEngine -> AudioPlayerService: phát MP3/TTS", "Hệ thống -> ghi playback history")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Quét mã QR"; Purpose = "Quét mã QR để mở nhanh POI và audio guide."; Files = "QrScannerPage.cs; DatabaseService.cs; NarrationEngine.cs; Api QrController.cs"; Status = "Đã triển khai";
        Classes = @(
            @("QrScannerPage", "Điều khiển camera và xử lý kết quả quét", "Đọc dữ liệu POI"),
            @("DatabaseService", "Tìm POI theo ID sau khi quét", "Trả dữ liệu cho QR page"),
            @("NarrationEngine", "Phát audio từ POI đã quét", "Được gọi từ QR page")
        );
        Diagram = @("QrScannerPage -> CameraView", "QrScannerPage -> DatabaseService -> Poi", "QrScannerPage -> NarrationEngine -> PoiDetailPage");
        Actors = "Người dùng"; Input = "Mã QR"; Output = "Trang POI và âm thanh"; Conditions = "Camera được cấp quyền và QR hợp lệ";
        Flow = @("Người dùng -> QrScannerPage", "CameraView -> QrScannerPage.OnQrDetected", "QrScannerPage -> ParsePoiId", "QrScannerPage -> DatabaseService: tìm POI", "Người dùng -> bấm 'Mở và phát thuyết minh'", "QrScannerPage -> NarrationEngine + PoiDetailPage")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Đồng bộ offline và dữ liệu cục bộ"; Purpose = "Đồng bộ dữ liệu POI và quản lý gói nội dung offline."; Files = "SyncService.cs; DatabaseService.cs; OfflinePacksPage.cs; Api SyncController.cs"; Status = "Đã triển khai (gói offline hiện còn mô phỏng)";
        Classes = @(
            @("SyncService", "Gọi API để đồng bộ dữ liệu", "Làm việc với SyncController API"),
            @("DatabaseService", "Lưu và đọc SQLite cục bộ", "Làm lớp cache dữ liệu"),
            @("OfflinePacksPage", "Quản lý giao diện tải/xóa gói offline", "Làm việc với AppDataDirectory")
        );
        Diagram = @("SyncService -> SyncController API", "SyncService -> DatabaseService -> SQLite", "OfflinePacksPage -> AppDataDirectory/audio");
        Actors = "Người dùng / hệ thống"; Input = "Yêu cầu đồng bộ hoặc tải offline"; Output = "Dữ liệu local và file âm thanh"; Conditions = "Có mạng để đồng bộ, có bộ nhớ để lưu";
        Flow = @("Khởi động app hoặc người dùng -> SyncService.SyncPoisFromServerAsync", "SyncService -> /api/v1/pois/sync/pois", "SyncService -> DatabaseService: ghi SQLite", "OfflinePacksPage -> tạo/xóa file âm thanh trong AppData", "UI -> cập nhật trạng thái lưu trữ và lần đồng bộ cuối")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Hồ sơ và cài đặt"; Purpose = "Quản lý thông tin người dùng, đổi mật khẩu, đổi ngôn ngữ và xóa cache."; Files = "ProfilePage.cs; SettingsPage.cs; AuthService.cs; LocalizationService.cs"; Status = "Đã triển khai (một số UI như favorites còn là placeholder)";
        Classes = @(
            @("ProfilePage", "Hiển thị hồ sơ, đổi mật khẩu và đăng xuất", "Dùng AuthService"),
            @("SettingsPage", "Đổi ngôn ngữ và xóa cache", "Dùng LocalizationService và FileSystem"),
            @("AuthService", "Cấp thông tin người dùng hiện tại và thao tác phiên", "Phục vụ ProfilePage")
        );
        Diagram = @("ProfilePage -> AuthService", "SettingsPage -> LocalizationService", "SettingsPage -> FileSystem/AppData", "AuthService -> /api/v1/auth/change-password");
        Actors = "Người dùng"; Input = "Thao tác hồ sơ hoặc cài đặt"; Output = "Thông tin cập nhật, phiên mới hoặc giao diện mới"; Conditions = "Người dùng đã đăng nhập đối với tác vụ hồ sơ";
        Flow = @("Người dùng -> ProfilePage/SettingsPage", "ProfilePage -> AuthService: lấy CurrentUser / đổi mật khẩu / đăng xuất", "SettingsPage -> LocalizationService: đổi ngôn ngữ", "SettingsPage -> FileSystem: xóa cache", "Hệ thống -> Người dùng: cập nhật giao diện và phiên")
    }
)

$overviewNodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$overviewNodes.Add((New-Paragraph -Text "1.1. Mục tiêu và giá trị hệ thống" -Bold:$true))
$overviewNodes.Add((New-Paragraph -Text "Hệ thống TourMap được xây dựng theo mô hình hai nền tảng gồm Admin CMS và Mobile App, trong đó Admin CMS phục vụ quản trị còn Mobile App phục vụ trải nghiệm du lịch tại điểm." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Mục tiêu nghiệp vụ chính là số hóa POI, chuẩn hóa dữ liệu tour, hỗ trợ audio guide đa ngôn ngữ, theo dõi hành vi nghe audio và cải thiện khả năng quản trị dữ liệu thực địa." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Giá trị kỹ thuật hiện tại là kết hợp GPS, geofence, TTS, SQLite cục bộ và REST API, giúp ứng dụng vẫn hoạt động khi mạng yếu đồng thời cho phép quản trị dữ liệu tập trung." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "1.2. Thành phần hệ thống và phạm vi hiện tại" -Bold:$true))
$overviewNodes.Add((New-Table -Headers @("Thành phần", "Vai trò", "Dữ liệu chính", "Công nghệ / nền tảng") -Rows @(
    @("Admin CMS", "Quản trị POI, tour, user, QR và analytics", "SQLite server, media uploads, dashboard metrics", "ASP.NET Core MVC, Entity Framework Core"),
    @("Mobile App", "Bản đồ, GPS, audio guide, QR, offline, profile", "SQLite local, SecureStorage, AppData audio", ".NET MAUI, Mapsui, BarcodeScanning"),
    @("REST API", "Xác thực, đồng bộ dữ liệu và ghi nhận lịch sử", "JWT, POI DTO, playback history", "ASP.NET Core Web API"),
    @("AI / Media", "Dịch đa ngôn ngữ và sinh âm thanh TTS", "Description* và AudioUrl* trên POI", "AITranslationService + file uploads")
)))
$overviewNodes.Add((New-Paragraph -Text "1.3. Kiến trúc tổng thể (mức logic)" -Bold:$true))
$overviewNodes.Add((New-Paragraph -Text "Admin CMS <-> SQLite server (POI, Tour, User, QR, Analytics)" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Mobile App -> REST API (/auth, /pois, /tours, /qr, /pois/sync)" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Mobile App -> SQLite local / SecureStorage / AppDataDirectory audio" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "GPS -> TourRuntimeService -> GeofenceEngine -> NarrationEngine -> AudioPlayerService" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "PlaybackHistory + UserLocationLog -> AnalyticsController -> Dashboard / CSV / Heatmap" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "1.4. Tác nhân và luồng giá trị" -Bold:$true))
$overviewNodes.Add((New-Table -Headers @("Tác nhân", "Mục tiêu", "Chức năng sử dụng") -Rows @(
    @("Quản trị viên", "Quản trị nội dung và theo dõi vận hành", "Dashboard, POI, Tour, User, QR, Analytics"),
    @("Khách du lịch / người dùng", "Tìm điểm đến, nghe thuyết minh, quét QR và dùng bản đồ", "Map, POI detail, QR, profile, settings"),
    @("Thiết bị / GPS", "Cấp tọa độ và dữ liệu local", "Geofence, offline data, app storage"),
    @("Backend API", "Xác thực và đồng bộ dữ liệu", "Auth, sync POI, tours, QR resolve, log history")
)))

$section2Nodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$section2Nodes.Add((New-Paragraph -Text "2.1. Bảng tổng hợp module hiện tại" -Bold:$true))
$summaryRows = @()
foreach ($module in $modules) {
    $summaryRows += ,@($module.Platform, $module.Title, $module.Purpose, $module.Files, $module.Status)
}
$section2Nodes.Add((New-Table -Headers @("Nền tảng", "Module", "Mô tả chi tiết", "Lớp / tệp chính", "Trạng thái") -Rows $summaryRows))
$section2Nodes.Add((New-Paragraph -Text "2.2. Thiết kế lớp chi tiết theo module" -Bold:$true))

$moduleIndex = 0
foreach ($module in $modules) {
    $moduleIndex++
    $section2Nodes.Add((New-Paragraph -Text ("2.2.{0}. {1} - {2}" -f $moduleIndex, $module.Platform, $module.Title) -Bold:$true))
    $section2Nodes.Add((New-Table -Headers @("Thành phần lớp", "Vai trò", "Quan hệ chính") -Rows $module.Classes))
    $section2Nodes.Add((New-Paragraph -Text "Sơ đồ lớp (dạng văn bản):" -Bold:$true))
    foreach ($line in $module.Diagram) {
        $section2Nodes.Add((New-Paragraph -Text $line -Style "ListParagraph"))
    }
}

$section3Nodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$section3Nodes.Add((New-Paragraph -Text "3.5. Luồng xử lý chi tiết theo module" -Bold:$true))

$workflowIndex = 0
foreach ($module in $modules) {
    $workflowIndex++
    $section3Nodes.Add((New-Paragraph -Text ("3.5.{0}. Luồng xử lý - {1} - {2}" -f $workflowIndex, $module.Platform, $module.Title) -Bold:$true))
    $section3Nodes.Add((New-Table -Headers @("Tác nhân", "Đầu vào", "Đầu ra", "Điều kiện / ghi chú") -Rows @(
        @($module.Actors, $module.Input, $module.Output, $module.Conditions)
    )))
    $section3Nodes.Add((New-Paragraph -Text "Lưu đồ xử lý (dạng văn bản):" -Bold:$true))
    foreach ($line in $module.Flow) {
        $section3Nodes.Add((New-Paragraph -Text $line -Style "ListParagraph"))
    }
}

Remove-BlockIfExists -StartPrefix "3.5." -EndPattern '^4\..*$' -EndPrefixes @("2.1.")
Remove-BlockIfExists -StartPrefix "1.1." -EndPattern '^2\..*Class Module$'
Remove-BlockIfExists -StartPrefix "2.1." -EndPattern '^3\..*Workflows\)$'

$section2Heading = Find-ParagraphByRegex -Pattern '^2\..*Class Module$'
$section3Heading = Find-ParagraphByRegex -Pattern '^3\..*Workflows\)$'
$section4Heading = Find-ParagraphByRegex -Pattern '^4\..*$'

Insert-NodesBefore -Anchor $section2Heading -Nodes $overviewNodes
Insert-NodesBefore -Anchor $section3Heading -Nodes $section2Nodes
Insert-NodesBefore -Anchor $section4Heading -Nodes $section3Nodes

$xml.Save($xmlPath)

[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $tempZip)
Copy-Item -LiteralPath $tempZip -Destination $sourceDocx -Force

Remove-Item -LiteralPath $tempZip -Force
Remove-Item -LiteralPath $tempDir -Recurse -Force

Write-Host "Đã cập nhật tài liệu: $sourceDocx"
