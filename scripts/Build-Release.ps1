[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pluginSource = Join-Path $root 'src\bin\Release\net6.0\LilithAI.dll'
$pluginCode = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\Plugin.cs')
$version = [regex]::Match($pluginCode, 'public const string Version = "([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the plugin version.' }

$coreArchive = Join-Path $root 'release-assets\packages\core.zip'
$voiceArchive = Join-Path $root 'release-assets\packages\voice-runtime.zip'
if (-not (Test-Path $coreArchive) -or -not (Test-Path $voiceArchive)) {
    throw 'release-assets\packages\core.zip and voice-runtime.zip are required.'
}

dotnet build (Join-Path $root 'LilithAI.sln') -c Release --no-restore
if ($LASTEXITCODE) { throw 'Release build failed.' }

Add-Type -AssemblyName System.IO.Compression.FileSystem
function Expand-SelectedArchive([string]$archivePath, [string]$destination, [string[]]$prefixes, [string[]]$files = @()) {
    $archive = [IO.Compression.ZipFile]::OpenRead($archivePath)
    try {
        foreach ($entry in $archive.Entries) {
            $entryName = $entry.FullName.Replace('\', '/')
            $selected = $files -contains $entryName
            foreach ($prefix in $prefixes) {
                if ($entryName.StartsWith($prefix, [StringComparison]::Ordinal)) { $selected = $true; break }
            }
            if (-not $selected -or -not $entry.Name) { continue }
            $path = Join-Path $destination ($entryName.Replace('/', '\'))
            New-Item -ItemType Directory -Force (Split-Path -Parent $path) | Out-Null
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $path, $true)
        }
    }
    finally {
        $archive.Dispose()
    }
}

function Copy-PackageFiles([string]$destination) {
    New-Item -ItemType Directory -Force (Join-Path $destination 'BepInEx\plugins') | Out-Null
    Copy-Item $pluginSource (Join-Path $destination 'BepInEx\plugins\LilithAI.dll')
    Get-ChildItem $root -Filter 'README*.md' | Copy-Item -Destination $destination
    Copy-Item (Join-Path $root 'docs') $destination -Recurse
}

$output = Join-Path $root 'release-assets\output'
$stage = Join-Path $root 'release-assets\stage'
New-Item -ItemType Directory -Force $output, $stage | Out-Null
Get-ChildItem $output -Filter "LilithAI-v$version-*.zip" | Remove-Item -Force
$textRoot = Join-Path $stage 'text'
$voiceRoot = Join-Path $stage 'text+voice'
foreach ($path in $textRoot, $voiceRoot) {
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Recurse -Force }
    New-Item -ItemType Directory -Force $path | Out-Null
}

$basePrefixes = @('BepInEx/core/', 'BepInEx/patchers/', 'BepInEx/unity-libs/', 'dotnet/')
$baseFiles = @('.doorstop_version', 'doorstop_config.ini', 'winhttp.dll')
Expand-SelectedArchive $coreArchive $textRoot $basePrefixes $baseFiles
Copy-PackageFiles $textRoot

Copy-Item (Join-Path $textRoot '*') $voiceRoot -Recurse
Expand-SelectedArchive $coreArchive $voiceRoot @('BepInEx/data/LilithTextInjector/voice/')
Expand-SelectedArchive $voiceArchive $voiceRoot @('')

$textZip = Join-Path $output "LilithAI-v$version-text.zip"
$voiceZip = Join-Path $output "LilithAI-v$version-text+voice.zip"
foreach ($zip in $textZip, $voiceZip) {
    if (Test-Path $zip) { Remove-Item -LiteralPath $zip -Force }
}
Compress-Archive -Path (Join-Path $textRoot '*') -DestinationPath $textZip -CompressionLevel Optimal
Compress-Archive -Path (Join-Path $voiceRoot '*') -DestinationPath $voiceZip -CompressionLevel Optimal

$required = @(
    'winhttp.dll',
    'BepInEx/core/BepInEx.Core.dll',
    'BepInEx/plugins/LilithAI.dll',
    'README.md', 'README.zh-CN.md', 'README.en.md', 'README.ja.md',
    'docs/images/hero.png', 'docs/images/chat.png', 'docs/images/settings.png'
)
$textArchive = [IO.Compression.ZipFile]::OpenRead($textZip)
$voicePackage = [IO.Compression.ZipFile]::OpenRead($voiceZip)
try {
    $textNames = $textArchive.Entries.FullName.Replace('\', '/')
    $voiceNames = $voicePackage.Entries.FullName.Replace('\', '/')
    foreach ($name in $required) {
        if ($textNames -notcontains $name -or $voiceNames -notcontains $name) { throw "Package is missing $name." }
    }
    if ($textNames -contains 'BepInEx/data/LilithTextInjector/voice-runtime/LilithVoiceHost.exe') {
        throw 'Text package unexpectedly contains the voice runtime.'
    }
    if ($voiceNames -notcontains 'BepInEx/data/LilithTextInjector/voice-runtime/LilithVoiceHost.exe' -or
        $voiceNames -notcontains 'BepInEx/data/LilithTextInjector/voice/calm-reference.wav') {
        throw 'Voice package is incomplete.'
    }
}
finally {
    $textArchive.Dispose()
    $voicePackage.Dispose()
}

if ((Get-Item $voiceZip).Length -gt 2GB) { throw 'Voice package exceeds the GitHub 2 GiB asset limit.' }
$hashes = Get-FileHash $textZip, $voiceZip -Algorithm SHA256
$hashes | ForEach-Object { "$($_.Hash)  $([IO.Path]::GetFileName($_.Path))" } |
    Set-Content -Encoding ASCII (Join-Path $output 'SHA256SUMS.txt')
$hashes | Select-Object Path, Hash
