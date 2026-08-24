[CmdletBinding()]
param(
    [string]$GameDir
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\LilithAI.csproj'

if (-not $GameDir) {
    $projectCode = Get-Content -Raw -Encoding UTF8 $project
    $GameDir = [regex]::Match($projectCode, '<GameDir[^>]*>([^<]+)</GameDir>').Groups[1].Value
}
if (-not $GameDir) { throw 'Could not resolve GameDir.' }

$requiredAssemblies = @(
    (Join-Path $GameDir 'BepInEx\core\BepInEx.Core.dll'),
    (Join-Path $GameDir 'BepInEx\core\BepInEx.Unity.IL2CPP.dll'),
    (Join-Path $GameDir 'BepInEx\core\Il2CppInterop.Runtime.dll'),
    (Join-Path $GameDir 'BepInEx\interop\Assembly-CSharp.dll'),
    (Join-Path $GameDir 'BepInEx\interop\UnityEngine.UI.dll'),
    (Join-Path $GameDir 'BepInEx\interop\Unity.TextMeshPro.dll')
)
foreach ($assembly in $requiredAssemblies) {
    if (-not (Test-Path -LiteralPath $assembly -PathType Leaf)) {
        throw "Required local interop assembly is missing: $assembly"
    }
}

dotnet build $project -c Release -p:GameDir=$GameDir
if ($LASTEXITCODE) { throw 'Plugin interop build failed.' }

$output = Join-Path $root 'src\bin\Release\net6.0\LilithAI.dll'
if (-not (Test-Path -LiteralPath $output -PathType Leaf)) { throw "Plugin output is missing: $output" }
Get-FileHash -LiteralPath $output -Algorithm SHA256 | Select-Object Path, Hash
