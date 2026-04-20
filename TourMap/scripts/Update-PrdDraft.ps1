[CmdletBinding()]
param(
    [string]$DocumentPath = "_docs\PRD-nháp.docx"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sourceDocx = Join-Path $repoRoot $DocumentPath
$tempDir = Join-Path $repoRoot ".tmp_prd_docx_edit"
$tempZip = Join-Path $repoRoot ".tmp_prd_docx_edit.zip"
$xmlPath = Join-Path $tempDir "word\document.xml"
$wNs = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"

if (-not (Test-Path $sourceDocx)) {
    $docsDir = Join-Path $repoRoot "_docs"
    $candidate = Get-ChildItem -LiteralPath $docsDir -Filter "PRD*.docx" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if ($null -eq $candidate) {
        throw "Document not found: $sourceDocx"
    }

    $sourceDocx = $candidate.FullName
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

    $tblW = $xml.CreateElement("w", "tblW", $wNs)
    $tblWValue = $xml.CreateAttribute("w", "w", $wNs)
    $tblWValue.Value = "0"
    $tblW.Attributes.Append($tblWValue) | Out-Null
    $tblWType = $xml.CreateAttribute("w", "type", $wNs)
    $tblWType.Value = "auto"
    $tblW.Attributes.Append($tblWType) | Out-Null
    $tblPr.AppendChild($tblW) | Out-Null

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

function Find-ParagraphByPrefix {
    param([string]$Prefix)

    $paragraphs = $xml.SelectNodes("//w:body/w:p", $ns)
    foreach ($paragraph in $paragraphs) {
        if ((Get-ParagraphText $paragraph).StartsWith($Prefix)) {
            return $paragraph
        }
    }

    throw "Could not find paragraph starting with: $Prefix"
}

function TryFind-ParagraphByPrefix {
    param([string]$Prefix)

    $paragraphs = $xml.SelectNodes("//w:body/w:p", $ns)
    foreach ($paragraph in $paragraphs) {
        if ((Get-ParagraphText $paragraph).StartsWith($Prefix)) {
            return $paragraph
        }
    }

    return $null
}

function Find-ParagraphByContains {
    param([string]$Fragment)

    $paragraphs = $xml.SelectNodes("//w:body/w:p", $ns)
    foreach ($paragraph in $paragraphs) {
        if ((Get-ParagraphText $paragraph).Contains($Fragment)) {
            return $paragraph
        }
    }

    throw "Could not find paragraph containing: $Fragment"
}

function Find-ParagraphByRegex {
    param([string]$Pattern)

    $paragraphs = $xml.SelectNodes("//w:body/w:p", $ns)
    foreach ($paragraph in $paragraphs) {
        if ((Get-ParagraphText $paragraph) -match $Pattern) {
            return $paragraph
        }
    }

    throw "Could not find paragraph matching regex: $Pattern"
}

function Insert-NodesAfter {
    param(
        [System.Xml.XmlNode]$Anchor,
        [System.Collections.Generic.List[System.Xml.XmlNode]]$Nodes
    )

    $current = $Anchor
    foreach ($node in $Nodes) {
        $imported = $xml.ImportNode($node, $true)
        $current.ParentNode.InsertAfter($imported, $current) | Out-Null
        $current = $imported
    }
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
        Platform = "Admin CMS"; Title = "Xac thuc Admin"; Purpose = "Dang nhap/dang xuat admin, khoa tam thoi tai khoan va cap cookie auth."; Files = "AccountController.cs; AdminUser.cs; AdminDbContext.cs"; Status = "Implemented"
        Classes = @(
            @("AccountController", "Xu ly login, logout, xac thuc cookie", "Dung AdminDbContext va PasswordHasher"),
            @("AdminUser", "Luu tai khoan admin, role, lockout, failed count", "Duoc AccountController va Program bootstrap su dung"),
            @("AdminDbContext", "Truy cap bang AdminUsers", "Cap du lieu cho controller")
        )
        Diagram = @("AccountController -> AdminDbContext -> AdminUser", "AccountController -> PasswordHasher<AdminUser>", "AccountController -> CookieAuthentication")
        Actors = "Admin"; Input = "Username, password"; Output = "Session dang nhap hop le"; Conditions = "Tai khoan active, mat khau dung, khong bi lockout"
        Flow = @("Admin -> AccountController.Login", "AccountController -> AdminDbContext: tim AdminUser", "AccountController -> PasswordHasher: xac thuc mat khau", "AccountController -> CookieAuth: tao session", "He thong -> Admin: chuyen vao dashboard")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Dashboard tong quan"; Purpose = "Tong hop KPI he thong, POI noi bat, tong user, tong QR, tong luot nghe."; Files = "HomeController.cs; AdminDashboardViewModel.cs; AdminDbContext.cs"; Status = "Implemented"
        Classes = @(
            @("HomeController", "Tong hop du lieu dashboard", "Doc du lieu tu AdminDbContext"),
            @("AdminDashboardViewModel", "Dong goi KPI de render view", "Nhan du lieu tu HomeController"),
            @("PoiPlaybackItem", "Luu top POI theo luot nghe", "Nam trong ViewModel")
        )
        Diagram = @("HomeController -> AdminDbContext", "HomeController -> AdminDashboardViewModel", "AdminDashboardViewModel -> PoiPlaybackItem")
        Actors = "Admin"; Input = "Yeu cau mo trang dashboard"; Output = "Dashboard KPI va top POI"; Conditions = "Admin da dang nhap"
        Flow = @("Admin -> HomeController.Index", "HomeController -> AdminDbContext: dem POI, Tour, User, QR, Plays", "HomeController -> AdminDashboardViewModel: dong goi du lieu", "HomeController -> View: render dashboard")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quan ly POI"; Purpose = "CRUD diem tham quan, upload media, bat/tat hien thi POI."; Files = "PoisController.cs; Poi.cs; AdminDbContext.cs"; Status = "Implemented"
        Classes = @(
            @("PoisController", "Create, edit, delete, list, toggle visibility", "Lam viec voi AdminDbContext va AITranslationService"),
            @("Poi", "Model POI, toa do, mo ta, media, ngon ngu", "Duoc luu trong SQLite va dong bo sang mobile"),
            @("AdminDbContext", "DbSet<Poi> va truy van POI", "Cap du lieu cho controller")
        )
        Diagram = @("Admin View -> PoisController -> AdminDbContext -> Poi", "PoisController -> IWebHostEnvironment -> wwwroot/uploads", "PoisController -> AITranslationService (optional)")
        Actors = "Admin"; Input = "Thong tin POI, anh, audio, trang thai"; Output = "POI moi hoac POI da cap nhat"; Conditions = "ModelState hop le"
        Flow = @("Admin -> PoisController.Create/Edit", "PoisController -> upload image/audio vao wwwroot/uploads", "PoisController -> AdminDbContext: insert/update Poi", "He thong -> Admin: quay lai danh sach POI")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "AI translation va TTS"; Purpose = "Dich mo ta POI sang nhieu ngon ngu va tao audio TTS da ngon ngu."; Files = "AITranslationService.cs; PoisController.cs; Poi.cs"; Status = "Implemented"
        Classes = @(
            @("AITranslationService", "Goi AI de dich va tao file TTS", "Duoc PoisController goi"),
            @("PoisController", "Kich hoat autoAI khi tao/sua POI", "Nhan ket qua tu AITranslationService"),
            @("Poi", "Luu cac truong DescriptionEn/Zh/Ko/Ja/Fr va AudioUrl*", "Duoc cap nhat sau khi sinh AI")
        )
        Diagram = @("PoisController -> AITranslationService -> translated text", "AITranslationService -> wwwroot/uploads/audio", "AITranslationService -> Poi language fields")
        Actors = "Admin"; Input = "Mo ta tieng Viet va tuy chon autoAI"; Output = "Ban dich va file audio TTS"; Conditions = "POI co noi dung mo ta va bat autoAI"
        Flow = @("Admin -> PoisController(Create/Edit, autoAI=true)", "PoisController -> AITranslationService: dich noi dung", "AITranslationService -> sinh TTS tung ngon ngu", "PoisController -> Poi: luu Description* va AudioUrl*", "AdminDbContext -> SQLite: save changes")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quan ly Tour"; Purpose = "Tao tour, gan POI vao tour va sap xep thu tu tham quan."; Files = "ToursController.cs; Tour.cs; TourPoiMapping.cs; TourEditViewModel.cs"; Status = "Implemented"
        Classes = @(
            @("ToursController", "CRUD Tour va quan ly danh sach POI trong Tour", "Dung AdminDbContext va TourEditViewModel"),
            @("Tour", "Thong tin tour, mo ta, trang thai active", "Duoc luu trong bang Tours"),
            @("TourPoiMapping", "Map nhieu POI vao mot Tour theo thu tu", "Lien ket Tour va Poi"),
            @("TourEditViewModel", "Dong goi input create/edit tour", "Cho view admin")
        )
        Diagram = @("Admin View -> ToursController -> TourEditViewModel", "ToursController -> AdminDbContext -> Tour", "ToursController -> AdminDbContext -> TourPoiMapping")
        Actors = "Admin"; Input = "Ten tour, mo ta, danh sach POI"; Output = "Tour va thu tu POI"; Conditions = "ModelState hop le"
        Flow = @("Admin -> ToursController.Create/Edit", "ToursController -> AdminDbContext: save Tour", "ToursController -> AdminDbContext: remove/add TourPoiMapping", "He thong -> Admin: cap nhat danh sach tour")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quan ly User mobile"; Purpose = "Xem user, tim kiem, loc role, doi role, reset password, xoa user."; Files = "UsersController.cs; MobileUser.cs; PlaybackHistory.cs"; Status = "Implemented"
        Classes = @(
            @("UsersController", "Quan ly tai khoan mobile tren CMS", "Doc/ghi MobileUser va PlaybackHistory"),
            @("MobileUser", "Thong tin user, role, email, display name, token", "Duoc CMS va API Auth su dung"),
            @("PlaybackHistory", "Dem so lan nghe de hien thi chi tiet user", "Duoc UsersController thong ke")
        )
        Diagram = @("UsersController -> AdminDbContext -> MobileUser", "UsersController -> PlaybackHistory", "UsersController -> PasswordHasher<MobileUser>")
        Actors = "Admin"; Input = "Search, role, user action"; Output = "Danh sach user va ket qua cap nhat"; Conditions = "Admin co quyen Administrator"
        Flow = @("Admin -> UsersController.Index/Details", "UsersController -> AdminDbContext: truy van MobileUser", "Admin -> UsersController.ChangeRole/ResetPassword/Delete", "AdminDbContext -> SQLite: save changes", "He thong -> Admin: thong bao ket qua")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Quan ly QR"; Purpose = "Sinh QR deep-link cho tung POI va luu lich su QR da tao."; Files = "QrController.cs; QrCodeEntry.cs; Poi.cs"; Status = "Implemented"
        Classes = @(
            @("QrController", "Hien thi danh sach QR va tao QR cho POI", "Dung AdminDbContext"),
            @("QrCodeEntry", "Luu deep-link, URL anh QR, thoi gian tao", "Duoc CMS va Analytics tham chieu"),
            @("Poi", "Nguon du lieu de sinh deep-link audiotour://poi/{id}", "Lien ket voi QR")
        )
        Diagram = @("Admin View -> QrController -> AdminDbContext", "QrController -> Poi", "QrController -> QrCodeEntry")
        Actors = "Admin"; Input = "Chon POI can tao QR"; Output = "QR image URL va deep-link"; Conditions = "POI ton tai"
        Flow = @("Admin -> QrController.Generate", "QrController -> AdminDbContext: load Poi", "QrController -> tao deep-link va qrImageUrl", "QrController -> AdminDbContext: save QrCodeEntry", "He thong -> Admin: hien thi QR trong danh sach")
    },
    [pscustomobject]@{
        Platform = "Admin CMS"; Title = "Analytics va bao cao"; Purpose = "Thong ke playback, trigger type, heatmap, export CSV."; Files = "AnalyticsController.cs; AnalyticsDashboardViewModel.cs; PlaybackHistory.cs; UserLocationLog.cs"; Status = "Implemented"
        Classes = @(
            @("AnalyticsController", "Tong hop metric va export CSV", "Doc PlaybackHistory va UserLocationLog"),
            @("AnalyticsDashboardViewModel", "Dong goi KPI, top POI, heatmap, chart data", "Render tren view analytics"),
            @("PlaybackHistory", "Luu lich su nghe audio", "La nguon du lieu chinh cho analytics"),
            @("UserLocationLog", "Luu diem GPS de ve heatmap", "Duoc tong hop theo cum")
        )
        Diagram = @("AnalyticsController -> AdminDbContext", "AnalyticsController -> AnalyticsDashboardViewModel", "AnalyticsDashboardViewModel -> PlaybackHistory/UserLocationLog")
        Actors = "Admin"; Input = "Yeu cau xem analytics hoac export"; Output = "Dashboard phan tich hoac file CSV"; Conditions = "Admin da dang nhap"
        Flow = @("Admin -> AnalyticsController.Index/ExportCsv", "AnalyticsController -> AdminDbContext: query PlaybackHistory, UserLocationLog", "AnalyticsController -> AnalyticsDashboardViewModel: tong hop metric", "He thong -> Admin: render chart/heatmap hoac tra file CSV")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Onboarding va localization"; Purpose = "Chon ngon ngu ban dau, doi ngon ngu runtime va cap nhat shell labels."; Files = "SplashPage.cs; LocalizationService.cs; AppShell.xaml.cs"; Status = "Implemented"
        Classes = @(
            @("SplashPage", "Man hinh chon ngon ngu va dieu huong vao app", "Cap nhat LocalizationService"),
            @("LocalizationService", "Quan ly dictionary ngon ngu va event LanguageChanged", "Duoc toan app nghe su kien"),
            @("AppShell", "Cap nhat ten tab va route theo ngon ngu", "Nhan su kien tu LocalizationService")
        )
        Diagram = @("SplashPage -> LocalizationService", "LocalizationService -> LanguageChanged event", "AppShell/Pages -> LocalizationService")
        Actors = "Nguoi dung"; Input = "Ngon ngu duoc chon"; Output = "UI toan app doi ngon ngu"; Conditions = "App dang mo"
        Flow = @("Nguoi dung -> SplashPage: chon language", "SplashPage -> LocalizationService: set CurrentLanguage", "LocalizationService -> pages/tabs: phat su kien LanguageChanged", "He thong -> Nguoi dung: UI duoc cap nhat theo ngon ngu moi")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Xac thuc nguoi dung"; Purpose = "Dang nhap, dang ky, guest mode, refresh token va quan ly session mobile."; Files = "AuthService.cs; LoginPage.cs; RegisterPage.cs; AuthApiController.cs"; Status = "Implemented"
        Classes = @(
            @("LoginPage", "Thu form dang nhap va guest mode", "Goi AuthService"),
            @("RegisterPage", "Dang ky tai khoan moi", "Goi AuthService"),
            @("AuthService", "Quan ly API auth, SecureStorage, refresh token", "Lam viec voi AuthApiController"),
            @("AuthApiController", "REST API login/register/profile/change password", "Doc/ghi MobileUser")
        )
        Diagram = @("LoginPage/RegisterPage -> AuthService", "AuthService -> AuthApiController -> AdminDbContext -> MobileUser", "AuthService -> SecureStorage")
        Actors = "Nguoi dung"; Input = "Email/password hoac guest login"; Output = "JWT, refresh token, user profile"; Conditions = "Ket noi server auth hoat dong"
        Flow = @("Nguoi dung -> LoginPage/RegisterPage", "UI -> AuthService", "AuthService -> /api/v1/auth", "AuthApiController -> MobileUser: xac thuc/tao moi", "AuthService -> SecureStorage: luu token va profile", "He thong -> AppShell: vao ung dung chinh")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Ban do va GPS"; Purpose = "Hien thi Mapsui, marker POI, vi tri hien tai va preview POI tren ban do."; Files = "MapPage.xaml.cs; TourRuntimeService.cs; GpsTrackingService_Android.cs"; Status = "Implemented"
        Classes = @(
            @("MapPage", "Render map, marker POI, preview card, GPS badge", "Nghe TourRuntimeService va NarrationEngine"),
            @("TourRuntimeService", "Khoi tao runtime, lay POI va GPS", "Lam viec voi SyncService va GeofenceEngine"),
            @("GpsTrackingService_Android", "Lay toa do lien tuc tren Android", "Cap vi tri cho runtime")
        )
        Diagram = @("MapPage -> TourRuntimeService", "TourRuntimeService -> GpsTrackingService_Android", "MapPage -> Mapsui MapControl")
        Actors = "Nguoi dung"; Input = "Mo man hinh map, cap quyen GPS"; Output = "Ban do, marker, vi tri hien tai"; Conditions = "GPS va permission san sang"
        Flow = @("Nguoi dung -> MapPage", "MapPage -> TourRuntimeService.InitializeAsync", "TourRuntimeService -> GPS service: lay toa do", "MapPage -> Mapsui: ve marker POI va current location", "Nguoi dung -> tap marker -> mo preview/detail")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Kham pha POI va chi tiet"; Purpose = "Tim kiem, loc, xem danh sach POI va mo chi tiet POI."; Files = "PoiListPage.xaml.cs; PoiDetailPage.cs; DatabaseService.cs; Poi.cs"; Status = "Implemented"
        Classes = @(
            @("PoiListPage", "Hien thi danh sach, search, filter, navigate", "Doc POI tu DatabaseService"),
            @("PoiDetailPage", "Hien thi mo ta, thong tin, map link, playback controls", "Doc 1 POI tu DatabaseService"),
            @("DatabaseService", "Truy van SQLite local", "Cap du lieu cho list/detail"),
            @("Poi", "Model du lieu diem tham quan", "Dung xuyen suot app")
        )
        Diagram = @("PoiListPage -> DatabaseService -> Poi", "PoiListPage -> PoiDetailPage", "PoiDetailPage -> DatabaseService -> Poi")
        Actors = "Nguoi dung"; Input = "Tu khoa tim kiem, filter, chon POI"; Output = "Danh sach loc va man hinh chi tiet"; Conditions = "Local database da co du lieu"
        Flow = @("Nguoi dung -> PoiListPage", "PoiListPage -> DatabaseService: load POI", "Nguoi dung -> search/filter", "PoiListPage -> CollectionView: cap nhat danh sach", "Nguoi dung -> chon POI", "App -> PoiDetailPage: hien thi thong tin chi tiet")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Geofence va phat audio"; Purpose = "Tinh khoang cach, xac dinh POI gan nhat va phat audio/TTS tu dong."; Files = "GeofenceEngine.cs; NarrationEngine.cs; AudioPlayerService.cs; TourRuntimeService.cs"; Status = "Implemented"
        Classes = @(
            @("GeofenceEngine", "Tinh Haversine, tim POI trong ban kinh", "Duoc TourRuntimeService goi"),
            @("NarrationEngine", "State machine Idle/Playing/Cooldown va fallback audio", "Lam viec voi AudioPlayerService"),
            @("AudioPlayerService", "Phat/stop audio local", "Thuc thi playback"),
            @("TourRuntimeService", "Phoi hop GPS, geofence va narration", "Phat su kien location/audio")
        )
        Diagram = @("TourRuntimeService -> GeofenceEngine", "TourRuntimeService -> NarrationEngine -> AudioPlayerService", "NarrationEngine -> DatabaseService/local audio")
        Actors = "Nguoi dung di chuyen"; Input = "Toa do GPS moi"; Output = "Audio POI duoc phat"; Conditions = "Dat ban kinh, khong debounce/cooldown"
        Flow = @("GPS update -> TourRuntimeService", "TourRuntimeService -> GeofenceEngine: tim nearest POI", "GeofenceEngine -> kiem tra radius/debounce/cooldown", "TourRuntimeService -> NarrationEngine.OnPOITriggeredAsync", "NarrationEngine -> AudioPlayerService: phat MP3/TTS", "He thong -> ghi playback history")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Quet QR"; Purpose = "Quet ma QR, resolve POI va mo nhanh noi dung audio tour."; Files = "QrScannerPage.cs; DatabaseService.cs; NarrationEngine.cs; Api QrController.cs"; Status = "Implemented"
        Classes = @(
            @("QrScannerPage", "Khoi tao camera, xu ly detection, reset scanner", "Doc POI tu DatabaseService"),
            @("DatabaseService", "Tim POI theo ID sau khi scan", "Tra model cho QR page"),
            @("NarrationEngine", "Phat audio khi user bam mo va phat", "Nhan POI tu QR page"),
            @("QrController API", "Cung cap deep-link/QR data o server", "Ho tro tao va resolve QR")
        )
        Diagram = @("QrScannerPage -> CameraView", "QrScannerPage -> DatabaseService -> Poi", "QrScannerPage -> NarrationEngine -> PoiDetailPage")
        Actors = "Nguoi dung"; Input = "Ma QR"; Output = "POI detail va audio"; Conditions = "Camera permission duoc cap, QR hop le"
        Flow = @("Nguoi dung -> QrScannerPage", "CameraView -> QrScannerPage.OnQrDetected", "QrScannerPage -> ParsePoiId", "QrScannerPage -> DatabaseService: tim POI", "Nguoi dung -> bam 'Mo va phat thuyet minh'", "QrScannerPage -> NarrationEngine + PoiDetailPage")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Offline sync va local data"; Purpose = "Dong bo POI tu server, luu SQLite local, quan ly goi tai offline."; Files = "SyncService.cs; DatabaseService.cs; OfflinePacksPage.cs; Api SyncController.cs"; Status = "Implemented (offline packs currently mocked)"
        Classes = @(
            @("SyncService", "Goi API sync POI va log playback history", "Dung HttpClientFactory"),
            @("DatabaseService", "Luu/lay POI tu SQLite local", "Lam local cache cho app"),
            @("OfflinePacksPage", "UI tai/xoa goi offline va hien thi dung luong", "Dung AppDataDirectory"),
            @("SyncController API", "Tra danh sach POI va nhan playback history", "La backend cho SyncService")
        )
        Diagram = @("SyncService -> SyncController API", "SyncService -> DatabaseService -> SQLite", "OfflinePacksPage -> AppDataDirectory/audio")
        Actors = "Nguoi dung / he thong"; Input = "Yeu cau sync hoac tai offline"; Output = "Local database va file audio local"; Conditions = "Co network de sync; co storage de tai"
        Flow = @("App start/nguoi dung -> SyncService.SyncPoisFromServerAsync", "SyncService -> /api/v1/pois/sync/pois", "SyncService -> DatabaseService: ghi SQLite", "OfflinePacksPage -> tao/xoa file audio trong AppData", "UI -> cap nhat trang thai storage va last sync")
    },
    [pscustomobject]@{
        Platform = "Mobile App"; Title = "Profile va settings"; Purpose = "Quan ly thong tin user, doi mat khau, dang xuat, doi ngon ngu va xoa cache."; Files = "ProfilePage.cs; SettingsPage.cs; AuthService.cs; LocalizationService.cs"; Status = "Implemented (mot so UI nhu favorites la placeholder)"
        Classes = @(
            @("ProfilePage", "Hien thi thong tin user, change password, logout", "Dung AuthService va LocalizationService"),
            @("SettingsPage", "Doi ngon ngu, xem cache, clear cache", "Dung LocalizationService va FileSystem"),
            @("AuthService", "Logout, change password, current user", "Cap du lieu cho ProfilePage"),
            @("LocalizationService", "Doi ngon ngu toan app", "Cap nhat Settings va pages")
        )
        Diagram = @("ProfilePage -> AuthService", "SettingsPage -> LocalizationService", "SettingsPage -> FileSystem/AppData", "AuthService -> /api/v1/auth/change-password")
        Actors = "Nguoi dung"; Input = "Tac vu profile/settings"; Output = "Thong tin cap nhat, session moi, ngon ngu moi"; Conditions = "User da dang nhap cho profile; settings co the dung moi luc"
        Flow = @("Nguoi dung -> ProfilePage/SettingsPage", "ProfilePage -> AuthService: lay CurrentUser / change password / logout", "SettingsPage -> LocalizationService: doi language", "SettingsPage -> FileSystem: clear cache", "He thong -> Nguoi dung: cap nhat UI va session")
    }
)

$overviewNodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$overviewNodes.Add((New-Paragraph -Text "1.1. Muc tieu va gia tri he thong" -Bold:$true))
$overviewNodes.Add((New-Paragraph -Text "He thong TourMap duoc xay dung theo mo hinh 2 nen tang gom Admin CMS va Mobile App, trong do Admin CMS phuc vu quan tri va Mobile App phuc vu trai nghiem du lich tai diem." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Muc tieu nghiep vu chinh la so hoa POI, chuan hoa du lieu tour, ho tro audio guide da ngon ngu, theo doi hanh vi nghe audio va cai thien kha nang quan tri du lieu thuc dia." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Gia tri ky thuat hien tai la ket hop GPS + geofence + TTS + SQLite local + REST API, giup app van hoat dong duoc khi mang yeu va cho phep admin quan tri du lieu tap trung." -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "1.2. Thanh phan he thong va pham vi hien tai" -Bold:$true))
$overviewNodes.Add((New-Table -Headers @("Thanh phan", "Vai tro", "Du lieu chinh", "Cong nghe/nen tang") -Rows @(
    @("Admin CMS", "Quan tri POI, Tour, User, QR, Analytics", "SQLite server, media uploads, dashboard metrics", "ASP.NET Core MVC, Entity Framework Core, Swashbuckle"),
    @("Mobile App", "Ban do, GPS, audio guide, QR, offline, profile", "SQLite local, SecureStorage, AppData audio", ".NET MAUI, Mapsui, BarcodeScanning"),
    @("REST API", "Cap auth, sync POI, tours, QR, analytics logging", "JWT, refresh token, POI DTO, playback history", "ASP.NET Core Web API"),
    @("AI/Media layer", "Dich da ngon ngu va sinh TTS", "Description* va AudioUrl* tren POI", "AITranslationService + file uploads")
)))
$overviewNodes.Add((New-Paragraph -Text "1.3. Kien truc tong the (muc logic)" -Bold:$true))
$overviewNodes.Add((New-Paragraph -Text "Admin CMS <-> SQLite server (POI, Tour, User, QR, Analytics)" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Mobile App -> REST API (/auth, /pois, /tours, /qr, /pois/sync)" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "Mobile App -> SQLite local / SecureStorage / AppDataDirectory audio" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "GPS -> TourRuntimeService -> GeofenceEngine -> NarrationEngine -> AudioPlayerService" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "PlaybackHistory + UserLocationLog -> AnalyticsController -> Dashboard/CSV/Heatmap" -Style "ListParagraph"))
$overviewNodes.Add((New-Paragraph -Text "1.4. Tac nhan va luong gia tri" -Bold:$true))
$overviewNodes.Add((New-Table -Headers @("Tac nhan", "Muc tieu", "Chuc nang su dung") -Rows @(
    @("Admin", "Quan tri noi dung va theo doi van hanh", "Dashboard, POI, Tour, User, QR, Analytics"),
    @("Khach du lich / nguoi dung", "Tim diem den, nghe thuyet minh, quet QR, su dung ban do", "Map, POI detail, QR, profile, settings"),
    @("He thong GPS / thiet bi", "Cap toa do va bo nho local", "Geofence, offline data, app storage"),
    @("Backend API", "Dong bo va xac thuc", "Auth, sync POI, tours, QR resolve, log history")
)))

$section2Nodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$section2Nodes.Add((New-Paragraph -Text "2.1. Bang tong hop module hien tai" -Bold:$true))
$summaryRows = @()
foreach ($module in $modules) {
    $summaryRows += ,@($module.Platform, $module.Title, $module.Purpose, $module.Files, $module.Status)
}
$section2Nodes.Add((New-Table -Headers @("Nen tang", "Module", "Mo ta chi tiet", "Main classes/files", "Trang thai") -Rows $summaryRows))
$section2Nodes.Add((New-Paragraph -Text "2.2. Class module design chi tiet" -Bold:$true))

$moduleIndex = 0
foreach ($module in $modules) {
    $moduleIndex++
    $section2Nodes.Add((New-Paragraph -Text ("2.2.{0}. {1} - {2}" -f $moduleIndex, $module.Platform, $module.Title) -Bold:$true))
    $section2Nodes.Add((New-Table -Headers @("Thanh phan lop", "Vai tro", "Quan he chinh") -Rows $module.Classes))
    $section2Nodes.Add((New-Paragraph -Text "Class diagram (text):" -Bold:$true))
    foreach ($line in $module.Diagram) {
        $section2Nodes.Add((New-Paragraph -Text $line -Style "ListParagraph"))
    }
}

$section3Nodes = New-Object 'System.Collections.Generic.List[System.Xml.XmlNode]'
$section3Nodes.Add((New-Paragraph -Text "3.5. Workflow chi tiet theo module" -Bold:$true))

$workflowIndex = 0
foreach ($module in $modules) {
    $workflowIndex++
    $section3Nodes.Add((New-Paragraph -Text ("3.5.{0}. Workflow - {1} - {2}" -f $workflowIndex, $module.Platform, $module.Title) -Bold:$true))
    $section3Nodes.Add((New-Table -Headers @("Tac nhan", "Dau vao", "Dau ra", "Dieu kien/ghi chu") -Rows @(
        @($module.Actors, $module.Input, $module.Output, $module.Conditions)
    )))
    $section3Nodes.Add((New-Paragraph -Text "Process flow chart (text):" -Bold:$true))
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

if (Test-Path $tempZip) {
    Remove-Item -LiteralPath $tempZip -Force
}

[System.IO.Compression.ZipFile]::CreateFromDirectory($tempDir, $tempZip)
Copy-Item -LiteralPath $tempZip -Destination $sourceDocx -Force

Remove-Item -LiteralPath $tempZip -Force
Remove-Item -LiteralPath $tempDir -Recurse -Force

Write-Host "Updated PRD draft: $sourceDocx"
