# Configura Git en esta PC para el repositorio de Crónicas de México.
# Ejecutar una vez por PC después de clonar:  powershell -ExecutionPolicy Bypass -File Tools\setup-git.ps1
# La configuración vive en .git/config, por eso no viaja con el repositorio.

$ErrorActionPreference = 'Stop'
$UnityVersion = '6000.0.85f1'
$Merge = "C:/Program Files/Unity/Hub/Editor/$UnityVersion/Editor/Data/Tools/UnityYAMLMerge.exe"

Set-Location (Split-Path $PSScriptRoot -Parent)

git lfs install --local
if ($LASTEXITCODE -ne 0) { throw 'Git LFS no está instalado.' }

if (Test-Path $Merge) {
    git config --local merge.unityyamlmerge.name 'Unity SmartMerge (UnityYAMLMerge)'
    git config --local merge.unityyamlmerge.driver "`"$Merge`" merge -h -p --force %O %B %A %A"
    git config --local merge.unityyamlmerge.recursive binary
    Write-Host "UnityYAMLMerge configurado: $Merge"
} else {
    Write-Warning "No se encontró UnityYAMLMerge en $Merge. Instala Unity $UnityVersion desde Unity Hub y vuelve a ejecutar."
}

git config --local core.autocrlf false
git config --local pull.rebase true
Write-Host 'Listo.'
