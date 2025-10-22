<#
.SYNOPSIS
  Publica SharedCockpitClient como ejecutable autocontenido para Windows x64.
.DESCRIPTION
  Restaura dependencias y ejecuta 'dotnet publish' usando la configuración indicada
  (Release por defecto). Deja el .exe en bin/<Config>/net8.0-windows/win-x64/publish.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$projectPath = Join-Path $PSScriptRoot 'SharedCockpitClient.csproj'
if (-not (Test-Path $projectPath)) {
    throw "No se encontró el archivo del proyecto en $projectPath"
}

Write-Host "Restaurando paquetes NuGet..." -ForegroundColor Cyan
& dotnet restore $projectPath

Write-Host "Publicando ejecutable ($Configuration)..." -ForegroundColor Cyan
& dotnet publish $projectPath -c $Configuration

$publishDir = Join-Path $PSScriptRoot "bin/$Configuration/net8.0-windows/win-x64/publish"
if (Test-Path $publishDir) {
    Write-Host "✔ Publicación finalizada. Ejecutable en:" -ForegroundColor Green
    Write-Host "  $publishDir" -ForegroundColor Yellow
    Write-Host "Recuerda copiar también la carpeta 'libs' si publicas sin un solo archivo." -ForegroundColor DarkGray
} else {
    Write-Warning "No se encontró la carpeta de publicación esperada: $publishDir"
}
