param(
    [string]$ModsDir = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\294100'
)
# Builds Assemblies/VigilForgottenBond.dll.
# Needs: the .NET SDK (https://dotnet.microsoft.com/download), and VEF + VPE subscribed on Steam.
# RimWorld and Harmony references are downloaded from NuGet automatically.
$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot '..\Source\VigilForgottenBond\VigilForgottenBond.csproj'
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK not installed: get it from https://dotnet.microsoft.com/download' }

function Find-Dll([string]$fileName, [string]$workshopId) {
    $modRoot = Join-Path $ModsDir $workshopId
    if (!(Test-Path $modRoot)) { throw "Workshop mod $workshopId not found under $ModsDir. Subscribe to it, or pass -ModsDir." }
    $hits = @(Get-ChildItem -Path $modRoot -Filter $fileName -Recurse -File | Select-Object -ExpandProperty FullName)
    $v16 = @($hits | Where-Object { $_ -match '\\1\.6\\' })
    if ($v16.Count -gt 0) { return $v16[0] }
    if ($hits.Count -gt 0) { return $hits[0] }
    throw "Could not find $fileName inside $modRoot."
}
if (!$env:VEF_DLL -or !(Test-Path $env:VEF_DLL)) { $env:VEF_DLL = Find-Dll 'VEF.dll' '2023507013' }
if (!$env:VPE_DLL -or !(Test-Path $env:VPE_DLL)) { $env:VPE_DLL = Find-Dll 'VanillaPsycastsExpanded.dll' '2842502659' }
Write-Host "VEF: $env:VEF_DLL"
Write-Host "VPE: $env:VPE_DLL"

& dotnet build $project -c Release -v minimal
if ($LASTEXITCODE -ne 0) { throw 'Compilation failed; see compiler errors above.' }
Write-Host 'Built Assemblies\VigilForgottenBond.dll. Smoke-test on a NEW save (see Docs/DEV_TESTS.md).'
