param([string]$Docx, [string]$Pdf)
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($Docx, $false, $false)
    # Update every field twice (page numbers settle after the TOC grows), then each TOC explicitly.
    for ($i = 0; $i -lt 2; $i++) {
        $doc.Fields.Update() | Out-Null
        foreach ($toc in $doc.TablesOfContents) { $toc.Update() | Out-Null }
        foreach ($tof in $doc.TablesOfFigures) { $tof.Update() | Out-Null }
    }
    $doc.Save()
    $doc.ExportAsFixedFormat($Pdf, 17, $false, 0, 0, 1, 1, 0, $true, $true, 1, $true, $true, $false)
    "pages=" + $doc.ComputeStatistics(2)
    $doc.Close($false)
} finally {
    $word.Quit()
}
