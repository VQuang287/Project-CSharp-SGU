Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$docxPath = Join-Path $PSScriptRoot 'AudioTourApp_PRD_v2_refined.docx'
$backupPath = Join-Path $PSScriptRoot 'AudioTourApp_PRD_v2_refined.backup_before_academic.docx'

if (-not (Test-Path $docxPath)) {
    throw "Khong tim thay file: $docxPath"
}

Copy-Item -LiteralPath $docxPath -Destination $backupPath -Force

$zip = [System.IO.Compression.ZipFile]::Open($docxPath, [System.IO.Compression.ZipArchiveMode]::Update)
$entry = $zip.GetEntry('word/document.xml')
if ($null -eq $entry) {
    $zip.Dispose()
    throw 'Khong tim thay word/document.xml trong file docx.'
}

$reader = New-Object System.IO.StreamReader($entry.Open())
[xml]$xml = $reader.ReadToEnd()
$reader.Close()

$nsUri = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('w', $nsUri)

function Get-ParagraphText($paragraph) {
    $texts = $paragraph.SelectNodes('.//w:t', $ns) | ForEach-Object { $_.'#text' }
    return ($texts -join '')
}

function Set-ParagraphText($paragraph, [string]$newText) {
    $textNodes = $paragraph.SelectNodes('.//w:t', $ns)
    if ($textNodes.Count -eq 0) {
        return $false
    }

    $textNodes[0].InnerText = $newText
    if ($newText.StartsWith(' ') -or $newText.EndsWith(' ')) {
        $textNodes[0].SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    }

    for ($i = 1; $i -lt $textNodes.Count; $i++) {
        $textNodes[$i].InnerText = ''
    }
    return $true
}

function Replace-ParagraphExact([string]$oldText, [string]$newText) {
    $paragraphs = $xml.SelectNodes('//w:p', $ns)
    foreach ($paragraph in $paragraphs) {
        if ((Get-ParagraphText $paragraph).Trim() -eq $oldText.Trim()) {
            [void](Set-ParagraphText $paragraph $newText)
        }
    }
}

function New-TextParagraph([string]$text, [string]$styleVal = $null, [int]$leftTwips = 0, [int]$afterTwips = 80) {
    $p = $xml.CreateElement('w', 'p', $nsUri)
    $pPr = $xml.CreateElement('w', 'pPr', $nsUri)

    if ($styleVal) {
        $pStyle = $xml.CreateElement('w', 'pStyle', $nsUri)
        $pStyle.SetAttribute('val', $nsUri, $styleVal)
        [void]$pPr.AppendChild($pStyle)
    }

    $spacing = $xml.CreateElement('w', 'spacing', $nsUri)
    $spacing.SetAttribute('after', $nsUri, [string]$afterTwips)
    [void]$pPr.AppendChild($spacing)

    if ($leftTwips -gt 0) {
        $ind = $xml.CreateElement('w', 'ind', $nsUri)
        $ind.SetAttribute('left', $nsUri, [string]$leftTwips)
        [void]$pPr.AppendChild($ind)
    }

    [void]$p.AppendChild($pPr)

    $r = $xml.CreateElement('w', 'r', $nsUri)
    $t = $xml.CreateElement('w', 't', $nsUri)
    if ($text.StartsWith(' ') -or $text.EndsWith(' ')) {
        $t.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    }
    $t.InnerText = $text
    [void]$r.AppendChild($t)
    [void]$p.AppendChild($r)
    return ,$p
}

function Set-BodyParagraphText([int]$index, [string]$newText) {
    $bodyParagraphs = $xml.SelectNodes('//w:body/w:p', $ns)
    if ($index -lt 0 -or $index -ge $bodyParagraphs.Count) {
        return
    }
    [void](Set-ParagraphText $bodyParagraphs[$index] $newText)
}

function Set-TableCellParagraphText([int]$tableIndex, [int]$rowIndex, [int]$cellIndex, [int]$paragraphIndex, [string]$newText) {
    $tables = $xml.SelectNodes('//w:body/w:tbl', $ns)
    if ($tableIndex -lt 0 -or $tableIndex -ge $tables.Count) {
        return
    }

    $rows = $tables[$tableIndex].SelectNodes('./w:tr', $ns)
    if ($rowIndex -lt 0 -or $rowIndex -ge $rows.Count) {
        return
    }

    $cells = $rows[$rowIndex].SelectNodes('./w:tc', $ns)
    if ($cellIndex -lt 0 -or $cellIndex -ge $cells.Count) {
        return
    }

    $paragraphsInCell = $cells[$cellIndex].SelectNodes('./w:p', $ns)
    if ($paragraphIndex -lt 0 -or $paragraphIndex -ge $paragraphsInCell.Count) {
        return
    }

    [void](Set-ParagraphText $paragraphsInCell[$paragraphIndex] $newText)
}

$bodyUpdates = @{
    7   = 'Tai lieu dac ta yeu cau san pham'
    12  = 'Tai lieu dac ta yeu cau san pham (PRD)'
    15  = "MỤC LỤC`r`n1. Tong quan du an`r`n1.1 Mo ta san pham`r`n1.2 Muc tieu`r`n1.3 Pham vi va doi tuong`r`n2. Kien truc he thong tong quan`r`n3. Geofence Engine va Narration Engine`r`n4. He thong ung dung nguoi dung`r`n5. Chien luoc dong bo va offline-first`r`n6. He thong admin va CMS web`r`n7. Thiet ke co so du lieu`r`n8. Thiet ke REST API`r`n9. Yeu cau phi chuc nang`r`n10. Kien truc trien khai va moi truong`r`n11. Ke hoach trien khai va lo trinh`r`n12. Rui ro va giai phap du phong`r`n13. Phu luc"
    18  = 'Audio Tour App la ung dung di dong ho tro thuyet minh tu dong cho khach tham quan va du lich. He thong su dung du lieu dinh vi GPS de kich hoat noi dung am thanh, bao gom giong noi tong hop (TTS) hoac tep thu san, khi nguoi dung tiep can cac diem tham quan da duoc xac dinh truoc.'
    19  = 'San pham huong den muc tieu so hoa trai nghiem tham quan thong qua cong nghe dinh vi va am thanh so, qua do bo sung cho mo hinh huong dan vien truyen thong va dap ung nhu cau du lich tu kham pha trong boi canh cac tuyen di san van hoa do thi.'
    21  = 'Cung cap trai nghiem thuyet minh tu dong theo dinh huong offline-first, bao dam cac chuc nang cot loi van kha dung khi khong co ket noi mang.'
    22  = 'Ho tro da ngon ngu, uu tien tieng Viet va tieng Anh, dong thoi co kha nang mo rong sang cac ngon ngu khac thong qua co che TTS.'
    23  = 'Tich hop ma QR nham kich hoat noi dung tai cac diem dung, tram xe buyt va bang thong tin ngoai hien truong.'
    24  = 'Cung cap he thong CMS cho phep quan tri noi dung, audio va cau hinh van hanh mot cach linh hoat.'
    25  = 'Thu thap du lieu phan tich an danh de danh gia hanh vi su dung va cai tien trai nghiem nguoi dung.'
    32  = 'Kien truc tong the duoc to chuc thanh ba thanh phan chinh gom ung dung di dong danh cho nguoi dung cuoi, he thong Backend API va nen tang quan tri Admin Web CMS.'
    44  = 'Geofence Engine dong vai tro thanh phan xu ly trung tam, lien tuc doi chieu vi tri hien thoi cua nguoi dung voi tap hop POI de xac dinh thoi diem kich hoat noi dung thuyet minh.'
    50  = 'Narration Engine quan ly toan bo vong doi cua tien trinh phat am thanh, bao gom tiep nhan su kien tu Geofence Engine, lua chon nguon phat TTS hoac tep am thanh, dieu phoi hang doi va ghi nhan nhat ky hoat dong.'
    67  = 'Che do foreground su dung FusedLocationProviderClient tren Android va CLLocationManager tren iOS voi chu ky cap nhat mac dinh 5 giay.'
    68  = 'Che do background duoc trien khai thong qua Foreground Service tren Android va quyen truy cap vi tri muc Always tren iOS nham duy tri kha nang theo doi lien tuc.'
    69  = 'Co che toi uu nang luong uu tien muc do chinh xac can bang thay cho High Accuracy, dong thoi tang chu ky lay mau khi phat hien thiet bi it hoac khong di chuyen.'
    70  = 'He thong loc nhieu bang cach loai bo cac mau vi tri co sai so lon hon 50 m hoac thong tin toc do khong hop le.'
    71  = 'Chuc nang dinh vi va geofence van hoat dong khi khong co ket noi mang, vi co che kich hoat chi phu thuoc vao du lieu GPS tren thiet bi.'
    73  = 'Hien thi vi tri hien thoi cua nguoi dung tren ban do theo thoi gian thuc thong qua ky hieu nhan dien truc quan.'
    74  = 'Hien thi toan bo POI duoi dang marker tuy bien, giup phan biet nhanh cac loai diem tham quan.'
    75  = 'Diem POI gan nhat duoc nhan manh thong qua marker kich thuoc lon hon, mau sac phan biet va hieu ung thu hut su chu y.'
    76  = 'Tuong tac voi marker se mo khung xem truoc, tu do cho phep nguoi dung di chuyen den man hinh chi tiet cua POI.'
    77  = 'Ban do tu dong dieu chinh tam nhin va muc phong to khi nguoi dung di chuyen den khu vuc co POI moi lien quan.'
    78  = 'He thong co kha nang mo rong de ho tro tile ban do offline trong truong hop can luu dem du lieu ban do cuc bo.'
    87  = 'Du lieu POI, tai nguyen am thanh va cac thanh phan phuc vu hien thi duoc luu dem cuc bo tren SQLite va he thong tep, qua do bao dam ung dung tiep tuc van hanh on dinh ngay ca trong dieu kien mat ket noi mang.'
    96  = 'He thong CMS la ung dung web phuc vu cong tac quan tri, cho phep quan ly noi dung, theo doi so lieu phan tich va cau hinh he thong ma khong can can thiep truc tiep vao ma nguon.'
    102 = 'Bieu mau tao moi va cap nhat POI bao gom cac truong thong tin sau:'
    116 = 'Cho phep tai len tep am thanh theo co che keo tha, dong thoi chuyen ma ve dinh dang AAC hoac MP3 128 kbps.'
    117 = 'Ho tro nghe thu truc tiep trong trinh duyet nham thuan tien cho quy trinh tham dinh noi dung.'
    118 = 'Quan ly phien ban noi dung am thanh theo nhieu lan phat hanh va ho tro quay lui khi can thiet.'
    119 = 'Lien ket tai nguyen am thanh voi tung POI va ngon ngu cu the de bao dam tinh nhat quan trong phan phoi noi dung.'
    120 = 'Cung cap chuc nang tao ban xem truoc TTS ngay tren giao dien web thong qua cac dich vu Azure hoac Google.'
    121 = 'Tong hop cac chi so nhu so luot phat, thoi gian nghe trung binh va diem dung nghe de phuc vu phan tich.'
    125 = 'Ban do nhiet the hien mat do xuat hien cua nguoi dung theo khu vuc tren co so du lieu an danh.'
    126 = 'Thong ke 10 POI co tan suat nghe cao nhat trong cac cua so thoi gian 7 ngay va 30 ngay.'
    127 = 'Tinh toan thoi gian nghe trung binh doi voi tung POI.'
    128 = 'Phan loai luot kich hoat theo co che GPS, QR va thao tac thu cong.'
    129 = 'Cung cap bieu do theo doi tan suat nghe theo gio, ngay va tuan.'
    130 = 'Do luong ty le hoan tat noi dung am thanh, phan anh muc do tiep nhan cua nguoi dung.'
    131 = 'Ho tro xuat bao cao du lieu duoi cac dinh dang CSV va Excel.'
    135 = 'Cho phep chon POI de sinh ma QR duoi dang PNG hoac SVG.'
    136 = 'Ho tro tao mau in ma QR kem logo, ten diem va khung trinh bay phu hop voi an pham truyen thong.'
    137 = 'Gan ma QR voi cac vi tri vat ly cu the, nhu tram xe buyt hoac bang thong tin tai diem den.'
    138 = 'Ma QR co the chua deep link den doi tuong POI, cho phep mo ung dung va kich hoat phat noi dung ngay lap tuc.'
    139 = 'So luot quet QR duoc thu thap trong he thong phan tich de phuc vu danh gia muc do tiep can.'
    140 = 'Ho tro xuat tep voi kich thuoc phu hop cho nhu cau in an kho A4 va A3.'
    162 = 'Du lieu vi tri duoc xu ly theo nguyen tac an danh hoa, khong lien ket truc tiep voi danh tinh ca nhan.'
    163 = 'Cac API quan tri su dung co che xac thuc JWT Bearer Token.'
    164 = 'Moi ket noi API bat buoc phai duoc thiet lap tren kenh HTTPS.'
    165 = 'Mat khau tren he thong CMS duoc bao ve bang co che bam BCrypt kem salt.'
    166 = 'Du lieu GPS phat sinh trong trang thai offline duoc luu tam thoi tren SQLite va dong bo lai khi co ket noi.'
    167 = 'He thong huong toi tuan thu cac nguyen tac GDPR va PDPA, cho phep nguoi dung tu choi thu thap du lieu phan tich.'
    168 = 'Co so du lieu SQLite tren thiet bi co the duoc ma hoa bang SQLCipher trong cac kich ban yeu cau muc bao mat cao hon.'
    169 = 'Tai khoan Super Admin tren CMS co the duoc tang cuong bao mat bang co che xac thuc hai yeu to.'
    171 = 'He thong duoc ky vong ho tro toi thieu 500 POI hien thi dong thoi tren ban do.'
    172 = 'Backend can dap ung nang luc xu ly toi thieu 100 yeu cau moi giay trong dieu kien dong thoi.'
    173 = 'He thong phan tich can xu ly toi thieu 10.000 ban ghi su kien moi ngay.'
    174 = 'Kien truc cho phep mo rong sang nhieu khu vuc dia ly thong qua viec bo sung POI ma khong can phat hanh lai ung dung.'
    175 = 'Mo hinh du lieu va noi dung cho phep mo rong ngon ngu moi ma han che toi da can thiep vao ma nguon.'
    176 = 'Tang TTS duoc thiet ke theo huong co the thay doi nha cung cap dich vu, vi du Azure hoac Google Cloud TTS.'
    178 = 'Ung dung duy tri muc do kha dung cao trong che do offline doi voi pham vi du lieu da duoc luu dem.'
    179 = 'Giao dien can bao dam kich thuoc chu va do tuong phan phu hop voi khuyen nghi WCAG AA.'
    180 = 'He thong huong toi kha nang tuong thich voi cac trinh doc man hinh nhu TalkBack va VoiceOver.'
    181 = 'Giao dien duoc thiet ke dap ung cho nhieu kich thuoc man hinh, tu dien thoai thong minh den may tinh bang.'
    191 = 'Ma nguon duoc dong bo len kho Git, vi du GitHub hoac Azure DevOps.'
    192 = 'Quy trinh xay dung va kiem thu duoc tu dong hoa, bao gom bien dich va chay cac bo kiem thu don vi.'
    193 = 'Ban phat hanh thu nghiem duoc trien khai tu dong len moi truong staging.'
    194 = 'Ban phat hanh chinh thuc yeu cau buoc phe duyet truoc khi trien khai len moi truong production.'
    210 = 'Khoang cach giua hai diem GPS tren be mat Trai Dat duoc tinh theo cong thuc Haversine nhu sau:'
    212 = 'Vi du cai dat bang C#:'
    215 = 'URI Scheme: audiotour://poi/{poi_id}. Universal Link va App Link su dung dang https://audiotour.app/poi/{poi_id}. Vi du ma QR: audiotour://poi/khanh-hoi-ben-tau. Luong xu ly duoc xac lap theo chuoi: quet QR, he thong chuyen huong den ung dung neu da cai dat hoac den kho ung dung neu chua co, sau do ung dung tiep nhan deep link va kich hoat noi dung audio tuong ung.'
}

foreach ($key in $bodyUpdates.Keys) {
    Set-BodyParagraphText -index ([int]$key) -newText $bodyUpdates[$key]
}

$tableCellUpdates = @(
    @{ T = 2; R = 1; C = 0; P = 0; N = 'Doi tuong nghien cuu chinh' },
    @{ T = 2; R = 1; C = 1; P = 0; N = 'Du khach tham quan cac khu di san va khong gian van hoa tai phuong Khanh Hoi, TP.HCM.' },
    @{ T = 2; R = 2; C = 0; P = 0; N = 'Nen tang trien khai' },
    @{ T = 2; R = 2; C = 1; P = 0; N = '.NET MAUI 8.0 cho Android (API 26+) va iOS (14+).' },
    @{ T = 2; R = 3; C = 1; P = 0; N = 'ASP.NET Core Web API ket hop SQLite hoac SQL Server va lop luu tru tep.' },
    @{ T = 2; R = 4; C = 1; P = 0; N = 'Ung dung ASP.NET Core MVC phuc vu quan ly POI, audio va phan tich van hanh.' },
    @{ T = 3; R = 1; C = 2; P = 0; N = 'Ung dung khach tren Android va iOS, dam nhiem dinh vi, geofence, phat thuyet minh, ban do va quet QR.' },
    @{ T = 3; R = 2; C = 2; P = 0; N = 'Thanh phan theo doi vi tri o che do foreground va background.' },
    @{ T = 3; R = 3; C = 2; P = 0; N = 'Thanh phan xac dinh POI nam trong ban kinh kich hoat, co bo loc debounce va cooldown.' },
    @{ T = 3; R = 4; C = 2; P = 0; N = 'Thanh phan dieu phoi hang doi am thanh, lua chon TTS hoac tep thu san, dong thoi ngan kich hoat lap.' },
    @{ T = 3; R = 5; C = 2; P = 0; N = 'Kho du lieu cuc bo luu thong tin POI offline, nhat ky phat va cau hinh nguoi dung.' },
    @{ T = 4; R = 1; C = 2; P = 0; N = 'Tan suat cap nhat vi tri khi ung dung dang hoat dong foreground.' },
    @{ T = 4; R = 2; C = 2; P = 0; N = 'Tan suat cap nhat vi tri trong che do background, uu tien tiet kiem nang luong.' },
    @{ T = 5; R = 1; C = 2; P = 0; N = 'Phat noi dung ngay lap tuc va uu tien cao nhat, co the ngat hang doi hien tai.' },
    @{ T = 5; R = 2; C = 2; P = 0; N = 'Dua muc noi dung vao dau hang doi phat.' },
    @{ T = 5; R = 3; C = 2; P = 0; N = 'Dua muc noi dung vao cuoi hang doi phat.' },
    @{ T = 5; R = 4; C = 2; P = 0; N = 'Cho phep phat ngay theo yeu cau thu cong cua nguoi dung.' },
    @{ T = 6; R = 1; C = 1; P = 0; N = 'Gioi thieu ung dung va thu thap cac quyen truy cap can thiet cho dinh vi va am thanh.' },
    @{ T = 6; R = 2; C = 1; P = 0; N = 'Hien thi ban do, vi tri nguoi dung, tap hop POI va diem duoc nhan manh theo ngu canh.' },
    @{ T = 6; R = 3; C = 1; P = 0; N = 'Cung cap danh sach POI kem khoang cach va trang thai tieu thu noi dung.' },
    @{ T = 6; R = 4; C = 1; P = 0; N = 'Trinh bay anh, mo ta, bo dieu khien am thanh va lien ket ban do cua tung POI.' },
    @{ T = 6; R = 6; C = 1; P = 0; N = 'Quet ma QR va kich hoat ngay noi dung thuyet minh tuong ung.' },
    @{ T = 6; R = 7; C = 1; P = 0; N = 'Cho phep tuy chinh ngon ngu TTS, toc do doc, ban kinh kich hoat va goi du lieu offline.' },
    @{ T = 6; R = 8; C = 1; P = 0; N = 'Tai truoc goi du lieu POI va audio de su dung trong dieu kien khong ket noi mang.' },
    @{ T = 7; R = 5; C = 2; P = 0; N = 'Phu hop voi POI moi, ngon ngu bo sung va noi dung can cap nhat linh hoat.' },
    @{ T = 7; R = 5; C = 3; P = 0; N = 'Phu hop voi POI trong tam, noi dung tieng Viet ban ngu va cac diem nhan thuyet minh quan trong.' },
    @{ T = 8; R = 1; C = 2; P = 0; N = 'Can bang giua muc do chinh xac dinh vi va muc tieu tiet kiem nang luong.' },
    @{ T = 8; R = 2; C = 2; P = 0; N = 'Phu hop voi cac ngu canh di chuyen khac nhau, chang han di bo va di xe buyt.' },
    @{ T = 9; R = 1; C = 1; P = 0; N = 'Lan khoi tao dau tien tai ve toan bo danh sach POI, ban dich va tai nguyen am thanh theo ngon ngu duoc chon.' },
    @{ T = 9; R = 2; C = 1; P = 0; N = 'Gui moc thoi gian dong bo gan nhat de may chu chi tra ve cac doi tuong moi, thay doi hoac bi xoa.' },
    @{ T = 9; R = 3; C = 1; P = 0; N = 'Ung dung su dung du lieu SQLite da luu dem; neu audio chua co san thi uu tien TTS noi bo.' },
    @{ T = 9; R = 4; C = 1; P = 0; N = 'Ap dung chien luoc server-wins, trong do CMS la nguon su that du lieu duy nhat.' },
    @{ T = 10; R = 1; C = 1; P = 0; N = 'Co' },
    @{ T = 10; R = 1; C = 2; P = 0; N = 'Co' },
    @{ T = 10; R = 1; C = 3; P = 0; N = 'Co' },
    @{ T = 10; R = 1; C = 4; P = 0; N = 'Co' },
    @{ T = 10; R = 2; C = 3; P = 0; N = 'Co, gioi han quyen sua' },
    @{ T = 10; R = 2; C = 4; P = 0; N = 'Khong' },
    @{ T = 10; R = 6; C = 4; P = 0; N = 'Chi xem' },
    @{ T = 10; R = 7; C = 2; P = 0; N = 'Khong' },
    @{ T = 10; R = 7; C = 3; P = 0; N = 'Khong' },
    @{ T = 10; R = 7; C = 4; P = 0; N = 'Khong' },
    @{ T = 11; R = 1; C = 1; P = 0; N = 'Tao, cap nhat va vo hieu hoa cac lo trinh tham quan theo chu de.' },
    @{ T = 11; R = 2; C = 1; P = 0; N = 'Sap xep thu tu ghe tham thong qua thao tac keo tha hoac lua chon co cau truc.' },
    @{ T = 11; R = 3; C = 1; P = 0; N = 'Cho phep xem truoc tuyen tham quan tren ban do mini cua CMS.' },
    @{ T = 12; R = 1; C = 2; P = 0; N = 'Thuc the diem tham quan gom toa do va cac thong so geofence co lien quan.' },
    @{ T = 12; R = 2; C = 2; P = 0; N = 'Noi dung am thanh hoac kich ban TTS theo tung ngon ngu cua POI.' },
    @{ T = 12; R = 3; C = 2; P = 0; N = 'Nhat ky su kien phat de phuc vu thong ke va co che tranh kich hoat lap.' },
    @{ T = 13; R = 1; C = 2; P = 0; N = 'Truy van danh sach POI dang o trang thai kich hoat.' },
    @{ T = 13; R = 2; C = 2; P = 0; N = 'Truy van chi tiet mot POI kem theo noi dung audio lien quan.' },
    @{ T = 13; R = 3; C = 2; P = 0; N = 'Tim kiem POI nam trong ban kinh chi dinh theo cong thuc Haversine.' },
    @{ T = 14; R = 1; C = 1; P = 0; N = 'Khong qua 3 giay trong truong hop khoi dong nguon lanh.' },
    @{ T = 14; R = 2; C = 1; P = 0; N = 'Khong qua 2 giay tinh tu khi nguoi dung di vao geofence den luc phat noi dung.' },
    @{ T = 15; R = 1; C = 1; P = 0; N = 'Phat trien ca nhan va go loi chuc nang.' },
    @{ T = 15; R = 2; C = 1; P = 0; N = 'Kiem thu tich hop, UAT va danh gia geofence.' },
    @{ T = 15; R = 3; C = 1; P = 0; N = 'Van hanh cho nguoi dung thuc te.' },
    @{ T = 16; R = 1; C = 2; P = 0; N = 'Phat trien GPS tracking, Geofence Engine, TTS co ban, SQLite local va Map View.' },
    @{ T = 16; R = 2; C = 2; P = 0; N = 'Bo sung audio file, queue management, POI detail, offline sync va background GPS.' },
    @{ T = 16; R = 6; C = 2; P = 0; N = 'Hoan thien UI/UX, toi uu hieu nang, kiem thu va tai lieu hoa.' },
    @{ T = 17; R = 6; C = 2; P = 0; N = 'ZXing.Net.Maui hoac BarcodeScanning.Maui' },
    @{ T = 18; R = 1; C = 3; P = 0; N = 'Loc cac mau vi tri co do chinh xac thap va tang ban kinh kich hoat trong khong gian trong nha.' },
    @{ T = 18; R = 2; C = 3; P = 0; N = 'Su dung significant-change API va huong dan nguoi dung cap quyen Always khi can.' },
    @{ T = 18; R = 3; C = 3; P = 0; N = 'Dieu chinh chu ky lay mau theo trang thai chuyen dong va uu tien Balanced accuracy.' },
    @{ T = 19; R = 1; C = 2; P = 0; N = 'Phuc vu dinh vi co do chinh xac cao trong che do foreground.' },
    @{ T = 19; R = 2; C = 2; P = 0; N = 'Ho tro theo doi vi tri khi ung dung hoat dong nen tren Android 10 tro len.' },
    @{ T = 19; R = 5; C = 2; P = 0; N = 'Cap quyen camera cho tinh nang quet ma QR.' }
)

foreach ($cellUpdate in $tableCellUpdates) {
    Set-TableCellParagraphText -tableIndex $cellUpdate.T -rowIndex $cellUpdate.R -cellIndex $cellUpdate.C -paragraphIndex $cellUpdate.P -newText $cellUpdate.N
}

$entry.Delete()
$newEntry = $zip.CreateEntry('word/document.xml')
$writer = New-Object System.IO.StreamWriter($newEntry.Open())
$xml.Save($writer)
$writer.Close()
$zip.Dispose()

Write-Output "Updated academic version: $docxPath"
Write-Output "Backup created: $backupPath"
