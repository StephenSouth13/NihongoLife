param([string]$Docx, [string]$Pdf)
# Opens the report in Word, updates the table of contents and the lists of figures and tables, keeps the empty
# paragraph that ends each list from opening a page of its own, saves, and exports the PDF.
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $doc = $word.Documents.Open($Docx, $false, $false)
    $doc.Repaginate()
    foreach ($toc in $doc.TablesOfContents) { $toc.Update() }
    foreach ($tof in $doc.TablesOfFigures) { $tof.Update() }
    $doc.Fields.Update() | Out-Null
    # The lists of figures and tables are long: a little less space between lines keeps each on whole pages.
    foreach ($tof in $doc.TablesOfFigures) { $tof.Range.ParagraphFormat.SpaceBefore = 0; $tof.Range.ParagraphFormat.SpaceAfter = 1 }
    $paras = $doc.Paragraphs
    $n = $paras.Count
    $shrunk = 0
    for ($i = 1; $i -lt $n; $i++) {
        $p = $paras.Item($i)
        if ($p.Range.Text.Trim().Length -gt 0) { continue }
        $next = $paras.Item($i + 1)
        if ($next.PageBreakBefore -ne 0) {
            $p.Range.Font.Size = 1
            $p.SpaceBefore = 0; $p.SpaceAfter = 0
            $p.LineSpacingRule = 4; $p.LineSpacing = 1
            $shrunk++
        }
    }
    "shrunk: $shrunk"
    $doc.Repaginate()
    foreach ($toc in $doc.TablesOfContents) { $toc.UpdatePageNumbers() }
    foreach ($tof in $doc.TablesOfFigures) { $tof.UpdatePageNumbers() }
    $doc.Save()
    $doc.ExportAsFixedFormat($Pdf, 17)
    "pages: " + $doc.ComputeStatistics(2)
    $doc.Close($false)
} finally {
    $word.Quit()
    [System.Runtime.InteropServices.Marshal]::ReleaseComObject($word) | Out-Null
}
