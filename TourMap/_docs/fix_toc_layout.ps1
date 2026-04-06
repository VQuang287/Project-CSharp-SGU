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

function Set-ParagraphText($paragraph, [string]$newText) {
    $textNodes = $paragraph.SelectNodes('.//w:t', $ns)
    if ($textNodes.Count -eq 0) {
        return
    }

    $textNodes[0].InnerText = $newText
    for ($i = 1; $i -lt $textNodes.Count; $i++) {
        $textNodes[$i].InnerText = ''
    }
}

function New-Paragraph([string]$text, [int]$leftTwips = 0, [int]$afterTwips = 40) {
    $p = $xml.CreateElement('w', 'p', $nsUri)
    $pPr = $xml.CreateElement('w', 'pPr', $nsUri)

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
    $t.InnerText = $text
    [void]$r.AppendChild($t)
    [void]$p.AppendChild($r)

    return $p
}

$bodyParagraphs = $xml.SelectNodes('//w:body/w:p', $ns)
$tocParagraph = $bodyParagraphs[15]
$firstSectionParagraph = $bodyParagraphs[16]

$tocEntries = @(
    @{ Text = '1. Tổng quan dự án'; Indent = 0; After = 30 },
    @{ Text = '1.1 Mô tả sản phẩm'; Indent = 360; After = 20 },
    @{ Text = '1.2 Mục tiêu'; Indent = 360; After = 20 },
    @{ Text = '1.3 Phạm vi và đối tượng'; Indent = 360; After = 50 },
    @{ Text = '2. Kiến trúc hệ thống tổng quan'; Indent = 0; After = 30 },
    @{ Text = '3. Geofence Engine và Narration Engine'; Indent = 0; After = 30 },
    @{ Text = '4. Hệ thống ứng dụng người dùng'; Indent = 0; After = 30 },
    @{ Text = '5. Chiến lược đồng bộ và offline-first'; Indent = 0; After = 30 },
    @{ Text = '6. Hệ thống admin và CMS web'; Indent = 0; After = 30 },
    @{ Text = '7. Thiết kế cơ sở dữ liệu'; Indent = 0; After = 30 },
    @{ Text = '8. Thiết kế REST API'; Indent = 0; After = 30 },
    @{ Text = '9. Yêu cầu phi chức năng'; Indent = 0; After = 30 },
    @{ Text = '10. Kiến trúc triển khai và môi trường'; Indent = 0; After = 30 },
    @{ Text = '11. Kế hoạch triển khai và lộ trình'; Indent = 0; After = 30 },
    @{ Text = '12. Rủi ro và giải pháp dự phòng'; Indent = 0; After = 30 },
    @{ Text = '13. Phụ lục'; Indent = 0; After = 120 }
)

Set-ParagraphText $tocParagraph 'MỤC LỤC'

$existingBetween = @()
$node = $tocParagraph.NextSibling
while ($null -ne $node -and $node -ne $firstSectionParagraph) {
    $next = $node.NextSibling
    if ($node.LocalName -eq 'p') {
        $existingBetween += $node
    }
    $node = $next
}

foreach ($p in $existingBetween) {
    [void]$p.ParentNode.RemoveChild($p)
}

$anchor = $tocParagraph
foreach ($entryInfo in $tocEntries) {
    $newParagraph = New-Paragraph -text $entryInfo.Text -leftTwips $entryInfo.Indent -afterTwips $entryInfo.After
    [void]$anchor.ParentNode.InsertAfter($newParagraph, $anchor)
    $anchor = $newParagraph
}

$entry.Delete()
$newEntry = $zip.CreateEntry('word/document.xml')
$writer = New-Object System.IO.StreamWriter($newEntry.Open())
$xml.Save($writer)
$writer.Close()
$zip.Dispose()

Write-Output "TOC layout fixed: $docxPath"
