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

function Ensure-ParagraphProperties($paragraph) {
    $pPr = $paragraph.SelectSingleNode('./w:pPr', $ns)
    if ($null -eq $pPr) {
        $pPr = $xml.CreateElement('w', 'pPr', $nsUri)
        if ($paragraph.HasChildNodes) {
            [void]$paragraph.InsertBefore($pPr, $paragraph.FirstChild)
        }
        else {
            [void]$paragraph.AppendChild($pPr)
        }
    }
    return $pPr
}

function Ensure-Centered($paragraph) {
    $pPr = Ensure-ParagraphProperties $paragraph
    $jc = $pPr.SelectSingleNode('./w:jc', $ns)
    if ($null -eq $jc) {
        $jc = $xml.CreateElement('w', 'jc', $nsUri)
        [void]$pPr.AppendChild($jc)
    }
    $jc.SetAttribute('val', $nsUri, 'center')
}

function Ensure-PageBreakBefore($paragraph) {
    $pPr = Ensure-ParagraphProperties $paragraph
    $pageBreak = $pPr.SelectSingleNode('./w:pageBreakBefore', $ns)
    if ($null -eq $pageBreak) {
        $pageBreak = $xml.CreateElement('w', 'pageBreakBefore', $nsUri)
        [void]$pPr.AppendChild($pageBreak)
    }
}

function Set-TocParagraphStyle($paragraph, [int]$leftTwips, [int]$afterTwips) {
    $pPr = Ensure-ParagraphProperties $paragraph

    $tabs = $pPr.SelectSingleNode('./w:tabs', $ns)
    if ($null -eq $tabs) {
        $tabs = $xml.CreateElement('w', 'tabs', $nsUri)
        [void]$pPr.AppendChild($tabs)
    }
    else {
        while ($tabs.HasChildNodes) {
            [void]$tabs.RemoveChild($tabs.FirstChild)
        }
    }

    $tab = $xml.CreateElement('w', 'tab', $nsUri)
    $tab.SetAttribute('val', $nsUri, 'right')
    $tab.SetAttribute('leader', $nsUri, 'dot')
    $tab.SetAttribute('pos', $nsUri, '9000')
    [void]$tabs.AppendChild($tab)

    $ind = $pPr.SelectSingleNode('./w:ind', $ns)
    if ($null -eq $ind) {
        $ind = $xml.CreateElement('w', 'ind', $nsUri)
        [void]$pPr.AppendChild($ind)
    }
    $ind.SetAttribute('left', $nsUri, [string]$leftTwips)

    $spacing = $pPr.SelectSingleNode('./w:spacing', $ns)
    if ($null -eq $spacing) {
        $spacing = $xml.CreateElement('w', 'spacing', $nsUri)
        [void]$pPr.AppendChild($spacing)
    }
    $spacing.SetAttribute('after', $nsUri, [string]$afterTwips)
}

function Clear-ParagraphContent($paragraph) {
    $pPr = $paragraph.SelectSingleNode('./w:pPr', $ns)
    $children = @($paragraph.ChildNodes)
    foreach ($child in $children) {
        if ($null -eq $pPr -or -not [object]::ReferenceEquals($child, $pPr)) {
            [void]$paragraph.RemoveChild($child)
        }
    }
}

function Add-HyperlinkRun($paragraph, [string]$displayText, [string]$anchorName) {
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

function Add-TabRun($paragraph) {
    $run = $xml.CreateElement('w', 'r', $nsUri)
    $tab = $xml.CreateElement('w', 'tab', $nsUri)
    [void]$run.AppendChild($tab)
    [void]$paragraph.AppendChild($run)
}

function Add-PageRefField($paragraph, [string]$bookmarkName) {
    $r1 = $xml.CreateElement('w', 'r', $nsUri)
    $fldBegin = $xml.CreateElement('w', 'fldChar', $nsUri)
    $fldBegin.SetAttribute('fldCharType', $nsUri, 'begin')
    [void]$r1.AppendChild($fldBegin)
    [void]$paragraph.AppendChild($r1)

    $r2 = $xml.CreateElement('w', 'r', $nsUri)
    $instr = $xml.CreateElement('w', 'instrText', $nsUri)
    $instr.SetAttribute('space', 'http://www.w3.org/XML/1998/namespace', 'preserve')
    $instr.InnerText = " PAGEREF $bookmarkName \h "
    [void]$r2.AppendChild($instr)
    [void]$paragraph.AppendChild($r2)

    $r3 = $xml.CreateElement('w', 'r', $nsUri)
    $fldSeparate = $xml.CreateElement('w', 'fldChar', $nsUri)
    $fldSeparate.SetAttribute('fldCharType', $nsUri, 'separate')
    [void]$r3.AppendChild($fldSeparate)
    [void]$paragraph.AppendChild($r3)

    $r4 = $xml.CreateElement('w', 'r', $nsUri)
    $t = $xml.CreateElement('w', 't', $nsUri)
    $t.InnerText = '1'
    [void]$r4.AppendChild($t)
    [void]$paragraph.AppendChild($r4)

    $r5 = $xml.CreateElement('w', 'r', $nsUri)
    $fldEnd = $xml.CreateElement('w', 'fldChar', $nsUri)
    $fldEnd.SetAttribute('fldCharType', $nsUri, 'end')
    [void]$r5.AppendChild($fldEnd)
    [void]$paragraph.AppendChild($r5)
}

$allParagraphs = $xml.SelectNodes('//w:body/w:p', $ns)

$tocTitle = $allParagraphs[15]
Ensure-Centered $tocTitle

$tocItems = @(
    @{ Index = 16; Display = '1. Tổng quan dự án'; Anchor = 'toc_1'; Left = 0; After = 30 },
    @{ Index = 17; Display = '1.1 Mô tả sản phẩm'; Anchor = 'toc_1_1'; Left = 360; After = 20 },
    @{ Index = 18; Display = '1.2 Mục tiêu'; Anchor = 'toc_1_2'; Left = 360; After = 20 },
    @{ Index = 19; Display = '1.3 Phạm vi và đối tượng'; Anchor = 'toc_1_3'; Left = 360; After = 50 },
    @{ Index = 20; Display = '2. Kiến trúc hệ thống tổng quan'; Anchor = 'toc_2'; Left = 0; After = 30 },
    @{ Index = 21; Display = '3. Geofence Engine và Narration Engine'; Anchor = 'toc_3'; Left = 0; After = 30 },
    @{ Index = 22; Display = '4. Hệ thống ứng dụng người dùng'; Anchor = 'toc_4'; Left = 0; After = 30 },
    @{ Index = 23; Display = '5. Chiến lược đồng bộ và offline-first'; Anchor = 'toc_5'; Left = 0; After = 30 },
    @{ Index = 24; Display = '6. Hệ thống admin và CMS web'; Anchor = 'toc_6'; Left = 0; After = 30 },
    @{ Index = 25; Display = '7. Thiết kế cơ sở dữ liệu'; Anchor = 'toc_7'; Left = 0; After = 30 },
    @{ Index = 26; Display = '8. Thiết kế REST API'; Anchor = 'toc_8'; Left = 0; After = 30 },
    @{ Index = 27; Display = '9. Yêu cầu phi chức năng'; Anchor = 'toc_9'; Left = 0; After = 30 },
    @{ Index = 28; Display = '10. Kiến trúc triển khai và môi trường'; Anchor = 'toc_10'; Left = 0; After = 30 },
    @{ Index = 29; Display = '11. Kế hoạch triển khai và lộ trình'; Anchor = 'toc_11'; Left = 0; After = 30 },
    @{ Index = 30; Display = '12. Rủi ro và giải pháp dự phòng'; Anchor = 'toc_12'; Left = 0; After = 30 },
    @{ Index = 31; Display = '13. Phụ lục'; Anchor = 'toc_13'; Left = 0; After = 120 }
)

foreach ($item in $tocItems) {
    $paragraph = $allParagraphs[$item.Index]
    Set-TocParagraphStyle -paragraph $paragraph -leftTwips $item.Left -afterTwips $item.After
    Clear-ParagraphContent $paragraph
    Add-HyperlinkRun -paragraph $paragraph -displayText $item.Display -anchorName $item.Anchor
    Add-TabRun -paragraph $paragraph
    Add-PageRefField -paragraph $paragraph -bookmarkName $item.Anchor
}

foreach ($paragraph in $allParagraphs) {
    $styleNode = $paragraph.SelectSingleNode('./w:pPr/w:pStyle', $ns)
    if ($null -ne $styleNode) {
        $styleVal = $styleNode.GetAttribute('val', $nsUri)
        if ($styleVal -eq 'Heading1') {
            Ensure-PageBreakBefore $paragraph
        }
    }
}

$entry.Delete()
$newEntry = $zip.CreateEntry('word/document.xml')
$writer = New-Object System.IO.StreamWriter($newEntry.Open())
$xml.Save($writer)
$writer.Close()
$zip.Dispose()

Write-Output "Formatted TOC in book style: $docxPath"
