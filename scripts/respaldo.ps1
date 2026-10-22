# Copia de seguridad de la base SQLite de SPIn.
# Uso:  .\scripts\respaldo.ps1          (Docker: detiene el contenedor un momento)
#       .\scripts\respaldo.ps1 -Local   (ejecución con dotnet run: copia datos\spin.db)
param([switch]$Local)
$ErrorActionPreference = "Stop"
$raiz = Split-Path $PSScriptRoot -Parent
$destino = Join-Path $raiz "respaldos"
New-Item -ItemType Directory -Force -Path $destino | Out-Null
$marca = Get-Date -Format "yyyyMMdd-HHmmss"
$archivo = Join-Path $destino "spin-$marca.db"
Push-Location $raiz
try {
    if ($Local) {
        Copy-Item "datos\spin.db" $archivo
        foreach ($s in "-wal", "-shm") { if (Test-Path "datos\spin.db$s") { Copy-Item "datos\spin.db$s" "$archivo$s" } }
    } else {
        docker compose stop spin
        try {
            docker compose cp spin:/app/data/spin.db $archivo
            if ($LASTEXITCODE -ne 0) { throw "No se pudo copiar la base desde el contenedor." }
        } finally { docker compose start spin }
    }
    Write-Host "Respaldo creado: $archivo"
} finally { Pop-Location }
