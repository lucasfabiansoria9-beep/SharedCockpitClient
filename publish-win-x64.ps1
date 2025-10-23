<#
.SYNOPSIS
  Publica SharedCockpitClient como ejecutable autocontenido para Windows x64.
.DESCRIPTION
  Restaura dependencias y ejecuta 'dotnet publish' con los parámetros necesarios
  (Release por defecto). Deja el .exe en bin/<Config>/net8.0-windows/win-x64/publish.
.PARAMETER Configuration
  Configuración de compilación a usar (Release/Debug). Release es el valor por defecto.
.PARAMETER SkipRestore
  Omite el paso de 'dotnet restore' si ya lo ejecutaste recientemente.
.PARAMETER NoSingleFile
  Evita empaquetar en un único archivo por si necesitas que los binarios queden sueltos.
.EXAMPLE
  ./publish-win-x64.ps1 -Configuration Debug
  Publica la aplicación en modo Debug.
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Release',

    [switch]$SkipRestore,

    [switch]$NoSingleFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw "No se encontró el comando 'dotnet' en el PATH. Instala el .NET SDK 8.0 o abre una terminal de desarrollador de Visual Studio."
}

$projectPath = Join-Path -Path $PSScriptRoot -ChildPath 'SharedCockpitClient.csproj'
if (-not (Test-Path -Path $projectPath)) {
    throw "No se encontró el archivo del proyecto en $projectPath"
}

$originalLocation = Get-Location

try {
    Set-Location -Path $PSScriptRoot

    if (-not $SkipRestore) {
        Write-Host 'Restaurando paquetes NuGet...' -ForegroundColor Cyan
        dotnet restore --nologo $projectPath
    } else {
        Write-Host 'Se omite dotnet restore porque fue solicitado.' -ForegroundColor DarkGray
    }

    $publishArgs = @(
        $projectPath
        '-c', $Configuration
        '-r', 'win-x64'
        '--self-contained', 'true'
        '/p:IncludeNativeLibrariesForSelfExtract=true'
    )

    if (-not $NoSingleFile) {
        $publishArgs += '/p:PublishSingleFile=true'
    }

    Write-Host "Publicando ejecutable ($Configuration, win-x64)..." -ForegroundColor Cyan
    dotnet publish @publishArgs

    $publishDir = Join-Path -Path $PSScriptRoot -ChildPath "bin/$Configuration/net8.0-windows/win-x64/publish"
    $exePath = Join-Path -Path $publishDir -ChildPath 'SharedCockpitClient.exe'

    if (Test-Path -Path $exePath) {
        Write-Host 'Publicación finalizada correctamente.' -ForegroundColor Green
        Write-Host "Ejecutable: $exePath" -ForegroundColor Yellow
    } else {
        Write-Warning "No se encontró el ejecutable en: $exePath"
    }

    $requiredLibs = @('Microsoft.FlightSimulator.SimConnect.dll', 'SimConnect.dll')
    $missingLibs = @()
    foreach ($lib in $requiredLibs) {
        $libPath = Join-Path -Path $publishDir -ChildPath $lib
        if (-not (Test-Path -Path $libPath)) {
            $missingLibs += $lib
        }
    }

    if ($missingLibs.Count -eq 0) {
        Write-Host 'Las DLL de SimConnect están junto al ejecutable.' -ForegroundColor Green
    } else {
        Write-Warning "Faltan las siguientes DLL en la carpeta de publicación: $($missingLibs -join ', ')"
        Write-Warning "Verifica que la carpeta 'libs' exista en el proyecto y vuelve a publicar."
    }

    Write-Host 'Puedes copiar toda la carpeta "publish" al equipo objetivo y ejecutar el .exe.' -ForegroundColor DarkGray
}
finally {
    Set-Location -Path $originalLocation
}