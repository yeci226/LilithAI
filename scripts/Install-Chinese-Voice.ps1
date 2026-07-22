[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$gameDir = $PSScriptRoot
if (-not (Test-Path (Join-Path $gameDir 'Lilith.exe'))) {
    throw 'Extract the voice ZIP beside Lilith.exe, then run this installer again.'
}
if (-not (Test-Path (Join-Path $gameDir 'BepInEx\plugins\LilithAI.dll'))) {
    throw 'BepInEx\plugins\LilithAI.dll is missing. Extract the voice ZIP again.'
}

$drive = [IO.DriveInfo]::new([IO.Path]::GetPathRoot($gameDir))
if ($drive.AvailableFreeSpace -lt 5GB) { throw 'Chinese voice installation needs at least 5 GB of free space.' }

$cache = Join-Path ([IO.Path]::GetTempPath()) 'LilithAI-Chinese-Voice'
New-Item -ItemType Directory -Force $cache | Out-Null
$downloads = @(
    @{
        Name = 'core.zip'
        Url = 'https://github.com/mimimi6666/Lilith-AI-Mod/releases/download/v0.1.1-rc4/core.zip'
        Sha256 = '155DC0BB1A1DDEA003690BCD8A4EDFE949526D46726DAB6FD3AFD4A3BED9FC73'
    },
    @{
        Name = 'voice-runtime.zip'
        Url = 'https://github.com/mimimi6666/Lilith-AI-Mod/releases/download/v0.1.1-rc4/voice-runtime.zip'
        Sha256 = '8AACBCD6B5595E1542D08D0B14B386CB3FB4EB46154AEA9B5FF1DA5484F42201'
    }
)

foreach ($download in $downloads) {
    $path = Join-Path $cache $download.Name
    $valid = (Test-Path $path) -and ((Get-FileHash $path -Algorithm SHA256).Hash -eq $download.Sha256)
    if (-not $valid) {
        Write-Host "Downloading $($download.Name)..."
        & curl.exe -L --fail --retry 2 --output $path $download.Url
        if ($LASTEXITCODE) { throw "$($download.Name) download failed." }
    }
    if ((Get-FileHash $path -Algorithm SHA256).Hash -ne $download.Sha256) {
        throw "$($download.Name) checksum failed. The download may be incomplete."
    }
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$corePath = Join-Path $cache 'core.zip'
$core = [IO.Compression.ZipFile]::OpenRead($corePath)
try {
    foreach ($entry in $core.Entries) {
        if (-not ($entry.FullName.StartsWith('dotnet/') -or
                  $entry.FullName.StartsWith('BepInEx/data/LilithTextInjector/voice/'))) { continue }
        if (-not $entry.Name) { continue }
        $destination = Join-Path $gameDir ($entry.FullName.Replace('/', '\'))
        New-Item -ItemType Directory -Force (Split-Path -Parent $destination) | Out-Null
        [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination, $true)
    }
}
finally {
    $core.Dispose()
}

Write-Host 'Extracting the Chinese voice runtime. This can take a while...'
& tar.exe -xf (Join-Path $cache 'voice-runtime.zip') -C $gameDir
if ($LASTEXITCODE) { throw 'Could not extract the Chinese voice runtime.' }

$hostPath = Join-Path $gameDir 'BepInEx\data\LilithTextInjector\voice-runtime\LilithVoiceHost.exe'
$referencePath = Join-Path $gameDir 'BepInEx\data\LilithTextInjector\voice\calm-reference.wav'
if (-not (Test-Path $hostPath) -or -not (Test-Path $referencePath)) {
    throw 'The voice runtime or reference audio is missing after extraction.'
}

Write-Host ''
Write-Host 'Chinese voice is installed. In the Lilith AI settings tab, set Voice to Chinese.' -ForegroundColor Green
