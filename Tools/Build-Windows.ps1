param(
    [Parameter(Mandatory=$false)][string]$RimWorldDir = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [Parameter(Mandatory=$false)][string]$ModsDir = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\294100'
)
$ErrorActionPreference = 'Stop'
$managed = Join-Path $RimWorldDir 'RimWorldWin64_Data\Managed'
$project = Join-Path $PSScriptRoot '..\Source\VigilForgottenBond\VigilForgottenBond.csproj'
if (!(Test-Path (Join-Path $managed 'Assembly-CSharp.dll'))) { throw "RimWorld Assembly-CSharp.dll missing at $managed" }
if (!(Get-Command dotnet -ErrorAction SilentlyContinue)) { throw 'dotnet SDK not installed; install a compatible .NET SDK plus net472 targeting pack.' }
$env:RIMWORLD_MANAGED = $managed
function Find-Dll([string]$pattern, [string]$modHint) {
    if (!(Test-Path $ModsDir)) { throw "Workshop directory missing: $ModsDir" }
    $matches = @(Get-ChildItem -Path $ModsDir -Filter $pattern -Recurse -File -ErrorAction SilentlyContinue |
        Where-Object { $_.FullName -match $modHint } | Select-Object -ExpandProperty FullName)
    if ($matches.Count -lt 1) { throw "Cannot find $pattern (matching $modHint). Set DLL env variables manually." }
    $chosen = @($matches | Where-Object { $_ -match '\\1\.6\\' })
    if ($chosen.Count -gt 0) { return $chosen[0] }
    return $matches[0]
}
if (!$env:HARMONY_DLL -or !(Test-Path $env:HARMONY_DLL)) { $env:HARMONY_DLL = Find-Dll '0Harmony.dll' '2009463077|Harmony' }
if (!$env:VEF_DLL -or !(Test-Path $env:VEF_DLL)) { $env:VEF_DLL = Find-Dll 'VEF.dll' '2023507013|VanillaExpandedFramework' }
if (!$env:VPE_DLL -or !(Test-Path $env:VPE_DLL)) { $env:VPE_DLL = Find-Dll 'VanillaPsycastsExpanded.dll' '2842502659|VanillaPsycastsExpanded' }
Write-Host "RimWorld: $managed"
Write-Host "Harmony: $env:HARMONY_DLL"
Write-Host "VEF: $env:VEF_DLL"
Write-Host "VPE: $env:VPE_DLL"
& dotnet build $project -c Release -v minimal
if ($LASTEXITCODE -ne 0) { throw "Compilation failed; see compiler errors above. Nothing installable was created." }
Write-Host 'Compiled! Verify DLL in Assemblies/ and smoke-test on a NEW save.'
