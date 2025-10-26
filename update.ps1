# ==========================================================
# SharedCockpitClient - Auto Update & Build Script v2.0
# ==========================================================
# Autor: Lucas Soria
# Descripción:
#   - Actualiza proyecto local desde GitHub
#   - Compila en modo Release
#   - Incrementa versión automáticamente
#   - Hace commit, push y crea tag de versión
# ==========================================================

$ErrorActionPreference = "Stop"
$projectName = "SharedCockpitClient"
$repoUrl = "https://github.com/lucasfabiansoria9-beep/SharedCockpitClient.git"
$publishDir = "bin\Release\net8.0-windows\win-x64\publish"
$versionFile = "version.txt"
$branch = "main"

Write-Host ""
Write-Host "=== Iniciando actualización automática de $projectName ===" -ForegroundColor Cyan

# Obtener versión actual
if (Test-Path $versionFile) {
    $currentVersion = Get-Content $versionFile
} else {
    $currentVersion = "0.9.0"
}

# Incrementar versión (X.Y.Z → X.Y.(Z+1))
$split = $currentVersion -split "\."
$major = [int]$split[0]
$minor = [int]$split[1]
$patch = [int]$split[2] + 1
$newVersion = "$major.$minor.$patch"

Write-Host ("Versión anterior: " + $currentVersion + " → Nueva versión: " + $newVersion) -ForegroundColor Yellow

# Sincronizar con GitHub
Write-Host ""
Write-Host "Actualizando repositorio local desde GitHub ($branch)..." -ForegroundColor Cyan
git fetch origin $branch
git pull origin $branch

# Compilar proyecto
Write-Host ""
Write-Host "Compilando proyecto $projectName (Release | win-x64)..." -ForegroundColor Yellow
dotnet publish "$projectName.csproj" -c Release -r win-x64 --self-contained false

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "ERROR: Error durante la compilación. Revisión necesaria." -ForegroundColor Red
    exit 1
}

# Actualizar versión
Write-Host ""
Write-Host "Compilación exitosa. Generando versión $newVersion..." -ForegroundColor Green
Set-Content -Path $versionFile -Value $newVersion

# Commit y push
Write-Host ""
Write-Host "Subiendo cambios a GitHub..." -ForegroundColor Cyan
git add .
git commit -m ("Build " + $newVersion + " - actualización automática")
git push origin $branch

# Crear y subir tag
$tagName = "v$newVersion"
git tag -a $tagName -m ("Release " + $projectName + " " + $newVersion + " (auto-build)")
git push origin $tagName

# Reporte final
Write-Host ""
Write-Host "=== Proyecto actualizado y compilado exitosamente ===" -ForegroundColor Green
Write-Host ("Publicación: " + $publishDir) -ForegroundColor DarkGray
Write-Host ("Versión actual: " + $newVersion) -ForegroundColor Yellow
Write-Host ("Repositorio: " + $repoUrl) -ForegroundColor Cyan
Write-Host ("Log: commit y tag subidos a rama " + $branch + " como " + $tagName) -ForegroundColor Gray
Write-Host "=====================================================" -ForegroundColor DarkGray
Write-Host "Proceso completado correctamente - Proyecto sincronizado" -ForegroundColor Green
Write-Host "=====================================================" -ForegroundColor DarkGray
