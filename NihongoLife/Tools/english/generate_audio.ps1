param(
    [string]$ProjectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
)

$ErrorActionPreference = 'Stop'
$manifestPath = Join-Path $PSScriptRoot 'audio_manifest.json'
$outputRoot = Join-Path $ProjectRoot 'LocalContent\English\audio'
$resolvedProject = [IO.Path]::GetFullPath($ProjectRoot)
$resolvedOutput = [IO.Path]::GetFullPath($outputRoot)
if (-not $resolvedOutput.StartsWith($resolvedProject, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Refusing to write outside project: $resolvedOutput"
}

Add-Type -AssemblyName System.Speech
$items = Get-Content -LiteralPath $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json
New-Item -ItemType Directory -Path $resolvedOutput -Force | Out-Null
$voice = [System.Speech.Synthesis.SpeechSynthesizer]::new()
$voice.Rate = -2
$voice.Volume = 100
try {
    foreach ($item in $items) {
        $target = Join-Path $resolvedOutput $item.file
        $voice.SetOutputToWaveFile($target)
        $voice.Speak([string]$item.text)
        $voice.SetOutputToNull()
        Write-Output $target
    }
}
finally {
    $voice.Dispose()
}

$license = @"
NihongoLife English Foundations offline audio
Generated locally from original project scripts with the operating-system speech synthesizer.
Not copied from Cambridge, British Council, VOA, or another textbook/audio course.
Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss K')
"@
Set-Content -LiteralPath (Join-Path $resolvedOutput 'SOURCE.txt') -Value $license -Encoding utf8
