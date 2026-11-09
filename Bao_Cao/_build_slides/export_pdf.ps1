param([string]$Pptx, [string]$Pdf)
# Opens the deck in PowerPoint, makes every embedded video start by itself when its slide shows (pptxgenjs cannot write the
# slide timing; PowerPoint writes it), saves the deck, then saves it as PDF (what the audience sees, fonts included).
$app = New-Object -ComObject PowerPoint.Application
try {
    $deck = $app.Presentations.Open($Pptx, $false, $false, $false)
    $videos = 0
    foreach ($slide in $deck.Slides) {
        foreach ($shape in $slide.Shapes) {
            if ($shape.Type -eq 16 -and $shape.MediaType -eq 3) {  # msoMedia, ppMediaTypeMovie
                $play = $shape.AnimationSettings.PlaySettings
                $play.PlayOnEntry = -1
                $play.PauseAnimation = 0
                $play.HideWhileNotPlaying = 0
                $videos++
            }
        }
    }
    $deck.Save()
    $deck.SaveAs($Pdf, 32)
    "slides: " + $deck.Slides.Count + ", videos that play by themselves: " + $videos
    $deck.Close()
} finally {
    $app.Quit()
}
