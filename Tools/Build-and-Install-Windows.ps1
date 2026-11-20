param(
    [string]$RimWorldDir = 'C:\Program Files (x86)\Steam\steamapps\common\RimWorld',
    [string]$WorkshopModsDir = 'C:\Program Files (x86)\Steam\steamapps\workshop\content\294100'
)
$ErrorActionPreference = 'Stop'
$sourceRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$builder = Join-Path $PSScriptRoot 'Build-Windows.ps1'
& $builder -ModsDir $WorkshopModsDir
$dll = Join-Path $sourceRoot 'Assemblies\VigilForgottenBond.dll'
if (!(Test-Path $dll)) { throw "The compiler did not produce $dll. No files have been installed." }
$target = Join-Path $RimWorldDir 'Mods\VIGIL_ForgottenBond'
if (Test-Path $target) { Write-Warning "Replacing existing VIGIL mod directory at $target"; Remove-Item $target -Recurse -Force }
New-Item -ItemType Directory -Force $target | Out-Null
foreach ($dir in @('About','Defs','Textures','Assemblies','Languages','Patches')) {
    $path = Join-Path $sourceRoot $dir
    if (Test-Path $path) { Copy-Item $path (Join-Path $target $dir) -Recurse -Force }
}
Write-Host "Installed compiled VIGIL alpha to: $target"
Write-Host 'Disable any old VIGIL weapon-preview mod: it reuses the same defNames.'
Write-Host 'Load RimWorld 1.6 with Harmony, Royalty, Biotech, VEF, VPE, VIGIL enabled (in that order).'
