param(
    [ValidateSet('validate', 'web', 'android', 'all')]
    [string]$Target = 'all',
    [string]$Version = '0.1.0',
    [int]$AndroidCode = 1,
    [string]$AppId = 'com.vtcacademy.nihongolife'
)

$ErrorActionPreference = 'Stop'
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.3.12f1\Editor\Unity.exe'
$project = Join-Path $PSScriptRoot 'NihongoLife'
$logRoot = Join-Path $PSScriptRoot 'Build\Logs'
New-Item -ItemType Directory -Force -Path $logRoot | Out-Null

if (-not (Test-Path -LiteralPath $unity)) { throw "Unity 6000.3.12f1 was not found at $unity" }
if ($Version -notmatch '^\d+\.\d+\.\d+([-.][0-9A-Za-z.-]+)?$') { throw 'Version must be semantic (for example 1.0.0).' }
if ($AndroidCode -lt 1) { throw 'AndroidCode must be positive.' }
if ($AppId -notmatch '^[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+$') { throw 'AppId must be a reverse-DNS identifier.' }

$env:NIHONGO_VERSION = $Version
$env:NIHONGO_ANDROID_CODE = $AndroidCode.ToString()
$env:NIHONGO_APP_ID = $AppId
$env:NIHONGO_BUILD_ROOT = Join-Path $PSScriptRoot 'Build\Release'

function Invoke-UnityBuild([string]$method, [string]$logName) {
    & $unity -batchmode -quit -projectPath $project -executeMethod $method -logFile (Join-Path $logRoot $logName)
    if ($LASTEXITCODE -ne 0) { throw "Unity failed ($method). See Build\Logs\$logName" }
}

switch ($Target) {
    'validate' { Invoke-UnityBuild 'NihongoLife.Editor.ReleaseBuild.ValidateRelease' 'validate.log' }
    'web' { Invoke-UnityBuild 'NihongoLife.Editor.ReleaseBuild.BuildWebGL' 'webgl.log' }
    'android' { Invoke-UnityBuild 'NihongoLife.Editor.ReleaseBuild.BuildAndroid' 'android.log' }
    'all' {
        Invoke-UnityBuild 'NihongoLife.Editor.ReleaseBuild.BuildWebGL' 'webgl.log'
        Invoke-UnityBuild 'NihongoLife.Editor.ReleaseBuild.BuildAndroid' 'android.log'
    }
}
