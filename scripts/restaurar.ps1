# Restaura un respaldo creado con respaldo.ps1 (reemplaza la base actual).
# Uso:  .\scripts\restaurar.ps1 respaldos\spin-20261021-180000.db [-Local]
param([Parameter(Mandatory = $true)][string]$Archivo, [switch]$Local)
$ErrorActionPreference = "Stop"
$raiz = Split-Path $PSScriptRoot -Parent
$Archivo = Resolve-Path $Archivo
$ok = Read-Host "Se reemplazará la base actual por $Archivo. Escriba SI para continuar"
if ($ok -ne "SI") { Write-Host "Cancelado."; exit 1 }
Push-Location $raiz
try {
    if ($Local) {
        foreach ($s in "-wal", "-shm") { Remove-Item "datos\spin.db$s" -ErrorAction SilentlyContinue }
        Copy-Item $Archivo "datos\spin.db" -Force
    } else {
        docker compose stop spin
        try {
            docker compose cp $Archivo spin:/app/data/spin.db
            if ($LASTEXITCODE -ne 0) { throw "No se pudo copiar el respaldo al contenedor." }
        } finally { docker compose start spin }
    }
    Write-Host "Base restaurada. Vuelva a ingresar a la aplicación."
} finally { Pop-Location }
