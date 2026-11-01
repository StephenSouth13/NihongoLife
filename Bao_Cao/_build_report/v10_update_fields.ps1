param([string]$Docx, [string]$Pdf)
# Opens the report in Word, fills the table of contents and the lists of figures and tables, saves, exports the PDF.
$word = New-Object -ComObject Word.Application
$word.Visible = $false
$word.DisplayAlerts = 0
try {
    $d = $word.Documents.Open($Docx, $false, $false, $false)
    $d.Repaginate()
    foreach ($round in 1..2) {
        foreach ($t in $d.TablesOfContents) { $t.Update() }
        foreach ($t in $d.TablesOfFigures) { $t.Update() }
        $d.Repaginate()
    }
    "pages: " + $d.ComputeStatistics(2)
    "tocs: " + $d.TablesOfContents.Count + " lists: " + $d.TablesOfFigures.Count + " fields: " + $d.Fields.Count
    $d.Save()
    $d.ExportAsFixedFormat($Pdf, 17)
    $d.Close(0)
} finally {
    $word.Quit()
}
