<#
.SYNOPSIS
    Crea una migración de Entity Framework Core en Data/Migrations.

.DESCRIPTION
    Usa el SDK de .NET 10 si está instalado. Si no, usa un contenedor temporal de Docker
    con el SDK (no instala nada en su equipo).

.EXAMPLE
    .\scripts\migracion.ps1 -Nombre Entrega01
#>
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z][A-Za-z0-9_]*$')]
    [string]$Nombre
)

$ErrorActionPreference = 'Stop'
$raiz = Split-Path -Parent $PSScriptRoot
Push-Location $raiz
try {
    $sdk = Get-Command dotnet -ErrorAction SilentlyContinue
    $tieneSdk10 = $false
    if ($sdk) { $tieneSdk10 = [bool](dotnet --list-sdks | Select-String -Pattern '^10\.') }

    if ($tieneSdk10) {
        Write-Host "Usando el SDK de .NET 10 instalado..." -ForegroundColor Cyan
        dotnet tool restore
        if ($LASTEXITCODE -ne 0) { throw "No se pudo restaurar la herramienta dotnet-ef." }
        dotnet ef migrations add $Nombre --output-dir Data/Migrations
        if ($LASTEXITCODE -ne 0) { throw "No se pudo crear la migración." }
    }
    elseif (Get-Command docker -ErrorAction SilentlyContinue) {
        Write-Host "SDK de .NET 10 no encontrado: usando Docker..." -ForegroundColor Cyan
        $comando = "dotnet tool restore && dotnet ef migrations add $Nombre --output-dir Data/Migrations; rc=`$?; rm -rf bin obj; exit `$rc"
        docker run --rm -v "${raiz}:/src" -w /src mcr.microsoft.com/dotnet/sdk:10.0 sh -c $comando
        if ($LASTEXITCODE -ne 0) { throw "No se pudo crear la migración con Docker." }
    }
    else {
        throw "Instale el SDK de .NET 10 (https://dotnet.microsoft.com/download/dotnet/10.0) o Docker Desktop."
    }

    Write-Host "Migración '$Nombre' creada en Data/Migrations." -ForegroundColor Green
}
finally {
    Pop-Location
}
