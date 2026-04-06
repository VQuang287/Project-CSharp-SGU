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
[xml]$xml = $reader.ReadToEnd()
$reader.Close()

$nsUri = 'http://schemas.openxmlformats.org/wordprocessingml/2006/main'
$ns = New-Object System.Xml.XmlNamespaceManager($xml.NameTable)
$ns.AddNamespace('w', $nsUri)

function Get-ParagraphText($paragraph) {
    (($paragraph.SelectNodes('.//w:t', $ns) | ForEach-Object { $_.'#text' }) -join '')
}

function Ensure-Bookmark($paragraph, [string]$bookmarkName, [int]$bookmarkId) {
    $existing = $paragraph.SelectSingleNode("./w:bookmarkStart[@w:name='$bookmarkName']", $ns)
    if ($null -ne $existing) {
        return
    }

    $bookmarkStart = $xml.CreateElement('w', 'bookmarkStart', $nsUri)
    $bookmarkStart.SetAttribute('id', $nsUri, [string]$bookmarkId)
    $bookmarkStart.SetAttribute('name', $nsUri, $bookmarkName)

    $bookmarkEnd = $xml.CreateElement('w', 'bookmarkEnd', $nsUri)
    $bookmarkEnd.SetAttribute('id', $nsUri, [string]$bookmarkId)

    $firstChild = $paragraph.FirstChild
    if ($null -ne $firstChild) {
        [void]$paragraph.InsertBefore($bookmarkStart, $firstChild)
        [void]$paragraph.AppendChild($bookmarkEnd)
    }
    else {
        [void]$paragraph.AppendChild($bookmarkStart)
        [void]$paragraph.AppendChild($bookmarkEnd)
    }
}

function Replace-WithHyperlink($paragraph, [string]$displayText, [string]$anchorName) {
    $pPr = $paragraph.SelectSingleNode('./w:pPr', $ns)
    $children = @($paragraph.ChildNodes)
    foreach ($child in $children) {
        if ($null -eq $pPr -or -not [object]::ReferenceEquals($child, $pPr)) {
            [void]$paragraph.RemoveChild($child)
        }
    }

    $hyperlink = $xml.CreateElement('w', 'hyperlink', $nsUri)
    $hyperlink.SetAttribute('anchor', $nsUri, $anchorName)
    $hyperlink.SetAttribute('history', $nsUri, '1')

    $run = $xml.CreateElement('w', 'r', $nsUri)
    $rPr = $xml.CreateElement('w', 'rPr', $nsUri)

    $rStyle = $xml.CreateElement('w', 'rStyle', $nsUri)
    $rStyle.SetAttribute('val', $nsUri, 'Hyperlink')
    [void]$rPr.AppendChild($rStyle)

    [void]$run.AppendChild($rPr)

    $text = $xml.CreateElement('w', 't', $nsUri)
    $text.InnerText = $displayText
    [void]$run.AppendChild($text)

    [void]$hyperlink.AppendChild($run)
    [void]$paragraph.AppendChild($hyperlink)
}

$tocParagraphs = $xml.SelectNodes('//w:body/w:p[position()>=16 and position()<=31]', $ns)
$headingParagraphs = $xml.SelectNodes('//w:body/w:p[position()>=32 and position()<=47]', $ns)

$items = @(
    @{ TocIndex = 0; HeadingText = '1. TỔNG QUAN DỰ ÁN'; Bookmark = 'toc_1_tong_quan'; Display = '1. Tổng quan dự án' },
    @{ TocIndex = 1; HeadingText = '1.1 Mô tả sản phẩm'; Bookmark = 'toc_1_1_mo_ta'; Display = '1.1 Mô tả sản phẩm' },
    @{ TocIndex = 2; HeadingText = '1.2 Mục tiêu'; Bookmark = 'toc_1_2_muc_tieu'; Display = '1.2 Mục tiêu' },
    @{ TocIndex = 3; HeadingText = '1.3 Phạm vi & Đối tượng'; Bookmark = 'toc_1_3_pham_vi'; Display = '1.3 Phạm vi và đối tượng' },
    @{ TocIndex = 4; HeadingText = '2. KIẾN TRÚC HỆ THỐNG TỔNG QUAN'; Bookmark = 'toc_2_kien_truc'; Display = '2. Kiến trúc hệ thống tổng quan' },
    @{ TocIndex = 5; HeadingText = '3. GEOFENCE ENGINE & NARRATION ENGINE'; Bookmark = 'toc_3_engine'; Display = '3. Geofence Engine và Narration Engine' },
    @{ TocIndex = 6; HeadingText = '4. HỆ THỐNG APP NGƯỜI DÙNG (MOBILE APP)'; Bookmark = 'toc_4_mobile'; Display = '4. Hệ thống ứng dụng người dùng' },
    @{ TocIndex = 7; HeadingText = '5. CHIẾN LƯỢC ĐỒNG BỘ & OFFLINE-FIRST *(MỚI)*'; Bookmark = 'toc_5_offline'; Display = '5. Chiến lược đồng bộ và offline-first' },
    @{ TocIndex = 8; HeadingText = '6. HỆ THỐNG ADMIN — CMS WEB'; Bookmark = 'toc_6_admin'; Display = '6. Hệ thống admin và CMS web' },
    @{ TocIndex = 9; HeadingText = '7. THIẾT KẾ CƠ SỞ DỮ LIỆU'; Bookmark = 'toc_7_db'; Display = '7. Thiết kế cơ sở dữ liệu' },
    @{ TocIndex = 10; HeadingText = '8. THIẾT KẾ REST API'; Bookmark = 'toc_8_api'; Display = '8. Thiết kế REST API' },
    @{ TocIndex = 11; HeadingText = '9. YÊU CẦU PHI CHỨC NĂNG'; Bookmark = 'toc_9_nfr'; Display = '9. Yêu cầu phi chức năng' },
    @{ TocIndex = 12; HeadingText = '10. KIẾN TRÚC TRIỂN KHAI & MÔI TRƯỜNG *(MỚI)*'; Bookmark = 'toc_10_deploy'; Display = '10. Kiến trúc triển khai và môi trường' },
    @{ TocIndex = 13; HeadingText = '11. KẾ HOẠCH TRIỂN KHAI & LỘ TRÌNH'; Bookmark = 'toc_11_roadmap'; Display = '11. Kế hoạch triển khai và lộ trình' },
    @{ TocIndex = 14; HeadingText = '12. RỦI RO & GIẢI PHÁP DỰ PHÒNG'; Bookmark = 'toc_12_risk'; Display = '12. Rủi ro và giải pháp dự phòng' },
    @{ TocIndex = 15; HeadingText = '13. PHỤ LỤC'; Bookmark = 'toc_13_appendix'; Display = '13. Phụ lục' }
)

$allParagraphs = $xml.SelectNodes('//w:body/w:p', $ns)
$bookmarkId = 500

foreach ($item in $items) {
    $tocParagraph = $tocParagraphs[$item.TocIndex]
    $headingParagraph = $null

    foreach ($paragraph in $allParagraphs) {
        if ((Get-ParagraphText $paragraph).Trim() -eq $item.HeadingText) {
            $headingParagraph = $paragraph
            break
        }
    }

    if ($null -eq $tocParagraph -or $null -eq $headingParagraph) {
        continue
    }

    Ensure-Bookmark -paragraph $headingParagraph -bookmarkName $item.Bookmark -bookmarkId $bookmarkId
    Replace-WithHyperlink -paragraph $tocParagraph -displayText $item.Display -anchorName $item.Bookmark
    $bookmarkId++
}

$entry.Delete()
$newEntry = $zip.CreateEntry('word/document.xml')
$writer = New-Object System.IO.StreamWriter($newEntry.Open())
$xml.Save($writer)
$writer.Close()
$zip.Dispose()

Write-Output "Added clickable TOC links: $docxPath"
