param([string]$In, [string]$Out, [string]$Pdf)
# Loads Roboto for this Windows session only (nothing is installed), lets PowerPoint embed it in the deck, exports the PDF.
Add-Type -Namespace Win -Name Gdi -MemberDefinition @'
[DllImport("gdi32.dll", CharSet=CharSet.Unicode)] public static extern int AddFontResourceW(string f);
[DllImport("gdi32.dll", CharSet=CharSet.Unicode)] public static extern bool RemoveFontResourceW(string f);
[DllImport("user32.dll", CharSet=CharSet.Auto)] public static extern IntPtr SendMessageTimeout(IntPtr h, uint m, IntPtr w, IntPtr l, uint f, uint t, out IntPtr r);
'@
$fonts = Get-ChildItem "D:\VTC_Academy\NihongoLife\NihongoLife\Bao_Cao\Fonts_Roboto\*.ttf"
foreach ($f in $fonts) { [void][Win.Gdi]::AddFontResourceW($f.FullName) }
$r = [IntPtr]::Zero
[void][Win.Gdi]::SendMessageTimeout([IntPtr]0xffff, 0x001D, [IntPtr]::Zero, [IntPtr]::Zero, 2, 2000, [ref]$r)
$pp = New-Object -ComObject PowerPoint.Application
try {
    $d = $pp.Presentations.Open($In, -1, 0, 0)
    "slides: " + $d.Slides.Count
    $d.SaveAs($Out, 24, -1)           # ppSaveAsOpenXMLPresentation, embed TrueType fonts
    $d.SaveAs($Pdf, 32)               # ppSaveAsPDF
    $d.Close()
} finally {
    $pp.Quit()
    foreach ($f in $fonts) { [void][Win.Gdi]::RemoveFontResourceW($f.FullName) }
    [void][Win.Gdi]::SendMessageTimeout([IntPtr]0xffff, 0x001D, [IntPtr]::Zero, [IntPtr]::Zero, 2, 2000, [ref]$r)
}
