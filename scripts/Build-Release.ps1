[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$pluginSource = Join-Path $root 'src\bin\Release\net6.0\LilithAI.dll'
$pluginCode = Get-Content -Raw -Encoding UTF8 (Join-Path $root 'src\Plugin.cs')
$version = [regex]::Match($pluginCode, 'public const string Version = "([^"]+)"').Groups[1].Value
if (-not $version) { throw 'Could not read the plugin version.' }

dotnet build (Join-Path $root 'LilithAI.sln') -c Release --no-restore
if ($LASTEXITCODE) { throw 'Release build failed.' }

$output = Join-Path $root 'release-assets\output'
$stage = Join-Path $root 'release-assets\stage'
New-Item -ItemType Directory -Force $output, $stage | Out-Null

foreach ($edition in 'text', 'voice') {
    $editionRoot = Join-Path $stage $edition
    if (Test-Path $editionRoot) { Remove-Item -LiteralPath $editionRoot -Recurse -Force }
    New-Item -ItemType Directory -Force (Join-Path $editionRoot 'BepInEx\plugins') | Out-Null
    Copy-Item $pluginSource (Join-Path $editionRoot 'BepInEx\plugins\LilithAI.dll')
    Copy-Item (Join-Path $root 'README.md') (Join-Path $editionRoot 'README.md')

    if ($edition -eq 'voice') {
        Copy-Item (Join-Path $PSScriptRoot 'Install-Chinese-Voice.ps1') $editionRoot
        Copy-Item (Join-Path $PSScriptRoot 'Install-Chinese-Voice.cmd') $editionRoot
    }

    $zipPath = Join-Path $output "LilithAI-v$version-$edition.zip"
    if (Test-Path $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
    Compress-Archive -Path (Join-Path $editionRoot '*') -DestinationPath $zipPath -CompressionLevel Optimal
}

Add-Type -AssemblyName System.IO.Compression.FileSystem
$textZip = Join-Path $output "LilithAI-v$version-text.zip"
$voiceZip = Join-Path $output "LilithAI-v$version-voice.zip"
$textEntries = [IO.Compression.ZipFile]::OpenRead($textZip)
$voiceEntries = [IO.Compression.ZipFile]::OpenRead($voiceZip)
try {
    $textNames = $textEntries.Entries.FullName.Replace('\', '/')
    $voiceNames = $voiceEntries.Entries.FullName.Replace('\', '/')
    if ($textNames -notcontains 'BepInEx/plugins/LilithAI.dll') { throw 'Text package is missing LilithAI.dll.' }
    if ($textNames -contains 'Install-Chinese-Voice.ps1') { throw 'Text package unexpectedly contains the voice installer.' }
    if ($voiceNames -notcontains 'BepInEx/plugins/LilithAI.dll' -or
        $voiceNames -notcontains 'Install-Chinese-Voice.ps1' -or
        $voiceNames -notcontains 'Install-Chinese-Voice.cmd') {
        throw 'Voice package is incomplete.'
    }
}
finally {
    $textEntries.Dispose()
    $voiceEntries.Dispose()
}

$hashes = Get-FileHash $textZip, $voiceZip -Algorithm SHA256
$hashes | ForEach-Object { "$($_.Hash)  $([IO.Path]::GetFileName($_.Path))" } |
    Set-Content -Encoding ASCII (Join-Path $output 'SHA256SUMS.txt')
$hashes | Select-Object Path, Hash
