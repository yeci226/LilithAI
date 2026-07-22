[CmdletBinding()]
param([string]$GameDir)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pluginSource = Join-Path $root 'src\bin\Release\net6.0\LilithAI.dll'
$pluginCode = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\Plugin.cs')
$version = [regex]::Match($pluginCode, 'public const string Version = "([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the plugin version.' }

if (-not $GameDir) {
    $projectCode = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\LilithAI.csproj')
    $GameDir = [regex]::Match($projectCode, '<GameDir[^>]*>([^<]+)</GameDir>').Groups[1].Value
}
if (-not (Test-Path (Join-Path $GameDir 'Lilith.exe'))) { throw "Lilith.exe was not found in $GameDir." }

$coreArchive = Join-Path $root 'release-assets\packages\core.zip'
$chineseArchive = Join-Path $root 'release-assets\packages\voice-runtime.zip'
$japaneseRoot = Join-Path $GameDir 'BepInEx\data\LilithTextInjector\voice-runtime'
$irodoriRoot = Join-Path $japaneseRoot 'Irodori-TTS-Server'
$uvPythonRoot = Join-Path $japaneseRoot '.uv-python'
$hfCacheRoot = Join-Path $japaneseRoot '.hf-cache'
foreach ($path in $coreArchive, $chineseArchive, $irodoriRoot, $uvPythonRoot, $hfCacheRoot) {
    if (-not (Test-Path $path)) { throw "Required release source is missing: $path" }
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
    New-Item -ItemType Directory -Force (Join-Path $destination 'BepInEx\config') | Out-Null
    Copy-Item (Join-Path $root 'release-assets\BepInEx.cfg') (Join-Path $destination 'BepInEx\config\BepInEx.cfg')
    Get-ChildItem $root -Filter 'README*.md' | Copy-Item -Destination $destination
    Copy-Item (Join-Path $root 'docs') $destination -Recurse
}

function Get-ZipItems([string]$sourceRoot, [string]$archivePrefix, [scriptblock]$include = { $true }) {
    $sourceRoot = $sourceRoot.TrimEnd('\')
    foreach ($file in Get-ChildItem $sourceRoot -Recurse -File -Force) {
        $relative = $file.FullName.Substring($sourceRoot.Length).TrimStart('\').Replace('\', '/')
        if (& $include $relative) {
            [pscustomobject]@{
                Source = $file.FullName
                Entry = "$archivePrefix/$relative"
                Length = $file.Length
            }
        }
    }
}

function New-Zip([string]$path, [object[]]$items) {
    if (Test-Path $path) { Remove-Item -LiteralPath $path -Force }
    $stream = [IO.File]::Open($path, [IO.FileMode]::CreateNew)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create)
    try {
        foreach ($item in $items) {
            [IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
                $archive, $item.Source, $item.Entry, [IO.Compression.CompressionLevel]::Optimal) | Out-Null
        }
    }
    finally {
        $archive.Dispose()
        $stream.Dispose()
    }
}

function Get-ZipNames([string]$path) {
    $archive = [IO.Compression.ZipFile]::OpenRead($path)
    try { return @($archive.Entries.FullName.Replace('\', '/')) }
    finally { $archive.Dispose() }
}

$output = Join-Path $root 'release-assets\output'
$stage = Join-Path $root 'release-assets\stage'
$baseRoot = Join-Path $stage 'base'
New-Item -ItemType Directory -Force $output, $stage | Out-Null
Get-ChildItem $output -Filter "LilithAI-v$version-*.zip" | Remove-Item -Force
if (Test-Path $baseRoot) { Remove-Item -LiteralPath $baseRoot -Recurse -Force }
New-Item -ItemType Directory -Force $baseRoot | Out-Null

$basePrefixes = @('BepInEx/core/', 'BepInEx/patchers/', 'BepInEx/unity-libs/', 'BepInEx/data/LilithTextInjector/voice/', 'dotnet/')
$baseFiles = @('.doorstop_version', 'doorstop_config.ini', 'winhttp.dll')
Expand-SelectedArchive $coreArchive $baseRoot $basePrefixes $baseFiles
Copy-PackageFiles $baseRoot

$baseZip = Join-Path $output "LilithAI-v$version-base.zip"
$chineseZip = Join-Path $output "LilithAI-v$version-voice-chinese.zip"
$japaneseRuntime1Zip = Join-Path $output "LilithAI-v$version-voice-japanese-runtime-1.zip"
$japaneseRuntime2Zip = Join-Path $output "LilithAI-v$version-voice-japanese-runtime-2.zip"
$japaneseModelZip = Join-Path $output "LilithAI-v$version-voice-japanese-model.zip"

Compress-Archive -Path (Join-Path $baseRoot '*') -DestinationPath $baseZip -CompressionLevel Optimal
Copy-Item $chineseArchive $chineseZip

$irodoriItems = @(Get-ZipItems $irodoriRoot 'BepInEx/data/LilithTextInjector/voice-runtime/Irodori-TTS-Server' {
    param($relative)
    -not $relative.StartsWith('.git/') -and
    -not $relative.StartsWith('.venv/Lib/site-packages/torch/lib/')
})
$uvPythonItems = @(Get-ZipItems $uvPythonRoot 'BepInEx/data/LilithTextInjector/voice-runtime/.uv-python')
$supportModelItems = @(Get-ZipItems $hfCacheRoot 'BepInEx/data/LilithTextInjector/voice-runtime/.hf-cache' {
    param($relative)
    -not $relative.StartsWith('hub/models--Aratako--Irodori-TTS-500M-v3/')
})
$torchLibRoot = Join-Path $irodoriRoot '.venv\Lib\site-packages\torch\lib'
$torchItems = @(Get-ZipItems $torchLibRoot 'BepInEx/data/LilithTextInjector/voice-runtime/Irodori-TTS-Server/.venv/Lib/site-packages/torch/lib')
$runtimeItems = @($irodoriItems + $uvPythonItems + $supportModelItems + $torchItems)
$runtime1 = [Collections.Generic.List[object]]::new()
$runtime2 = [Collections.Generic.List[object]]::new()
$runtime1Size = 0L
$runtime2Size = 0L
foreach ($item in $runtimeItems | Sort-Object Length -Descending) {
    if ($runtime1Size -le $runtime2Size) { $runtime1.Add($item); $runtime1Size += $item.Length }
    else { $runtime2.Add($item); $runtime2Size += $item.Length }
}
New-Zip $japaneseRuntime1Zip $runtime1.ToArray()
New-Zip $japaneseRuntime2Zip $runtime2.ToArray()

$mainModelRoot = Join-Path $hfCacheRoot 'hub\models--Aratako--Irodori-TTS-500M-v3'
$mainModelItems = @(Get-ZipItems $mainModelRoot 'BepInEx/data/LilithTextInjector/voice-runtime/.hf-cache/hub/models--Aratako--Irodori-TTS-500M-v3')
New-Zip $japaneseModelZip $mainModelItems

$required = @(
    'winhttp.dll',
    'BepInEx/core/BepInEx.Core.dll',
    'BepInEx/plugins/LilithAI.dll',
    'BepInEx/data/LilithTextInjector/voice/calm-reference.wav',
    'BepInEx/data/LilithTextInjector/voice/jp/calm-reference.wav',
    'README.md', 'README.zh-CN.md', 'README.en.md', 'README.ja.md',
    'docs/images/hero.png', 'docs/images/chat.png', 'docs/images/settings.png'
)
$baseNames = Get-ZipNames $baseZip
foreach ($name in $required) {
    if ($baseNames -notcontains $name) { throw "Base package is missing $name." }
}
if ($baseNames -contains 'BepInEx/data/LilithTextInjector/voice-runtime/LilithVoiceHost.exe') {
    throw 'Base package unexpectedly contains a voice runtime.'
}

$chineseNames = Get-ZipNames $chineseZip
if ($chineseNames -notcontains 'BepInEx/data/LilithTextInjector/voice-runtime/LilithVoiceHost.exe') {
    throw 'Chinese voice package is incomplete.'
}
$japaneseNames = @((Get-ZipNames $japaneseRuntime1Zip) + (Get-ZipNames $japaneseRuntime2Zip) +
    (Get-ZipNames $japaneseModelZip))
foreach ($name in @(
    'BepInEx/data/LilithTextInjector/voice-runtime/Irodori-TTS-Server/src/irodori_openai_tts/__init__.py',
    'BepInEx/data/LilithTextInjector/voice-runtime/Irodori-TTS-Server/.venv/Lib/site-packages/torch/lib/torch_cuda.dll'
)) {
    if ($japaneseNames -notcontains $name) { throw "Japanese voice packages are missing $name." }
}
if (-not ($japaneseNames | Where-Object { $_ -like 'BepInEx/data/LilithTextInjector/voice-runtime/.uv-python/*/python.exe' })) {
    throw 'Japanese voice packages are missing bundled Python.'
}
if (-not ($japaneseNames | Where-Object { $_ -like 'BepInEx/data/LilithTextInjector/voice-runtime/.hf-cache/hub/models--Aratako--Irodori-TTS-500M-v3/snapshots/*/model.safetensors' })) {
    throw 'Japanese voice packages are missing the Irodori model.'
}

$packages = @($baseZip, $chineseZip, $japaneseRuntime1Zip, $japaneseRuntime2Zip, $japaneseModelZip)
foreach ($package in $packages) {
    if ((Get-Item $package).Length -gt 2GB) { throw "$package exceeds the GitHub 2 GiB asset limit." }
}
$hashes = Get-FileHash $packages -Algorithm SHA256
$hashes | ForEach-Object { "$($_.Hash)  $([IO.Path]::GetFileName($_.Path))" } |
    Set-Content -Encoding ASCII (Join-Path $output 'SHA256SUMS.txt')
$hashes | Select-Object Path, Hash
