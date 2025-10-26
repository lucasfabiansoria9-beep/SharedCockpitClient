# ==============================================
# 🧹 Limpieza segura del proyecto SharedCockpitClient
# Autor: Lucas Soria + Leonardo
# Fecha: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
# ==============================================

Write-Host ""
Write-Host "🧭 Iniciando limpieza segura del proyecto SharedCockpitClient..." -ForegroundColor Cyan

# --- Configuración ---
$ProjectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogFile = Join-Path $ProjectRoot "clean_log.txt"
$BackupFolder = Join-Path $ProjectRoot "backup_before_clean_$(Get-Date -Format yyyyMMdd_HHmmss)"

# --- Crear backup opcional ---
Write-Host "📦 Creando copia de seguridad de archivos importantes..." -ForegroundColor Yellow
New-Item -ItemType Directory -Force -Path $BackupFolder | Out-Null

# Archivos y carpetas esenciales a respaldar
$EssentialItems = @(
    "Program.cs",
    "SharedCockpitClient.csproj",
    "MainForm.cs",
    "MainForm.Designer.cs",
    "MainForm.resx",
    "ConnectionManager.cs",
    "NetworkConnection.cs",
    "SimConnectHandler.cs",
    "update.ps1",
    "publish-win-x64.ps1",
    "git-push.ps1",
    "version.txt",
    "SharedCockpitClient.slnx"
)

foreach ($item in $EssentialItems) {
    $src = Join-Path $ProjectRoot $item
    if (Test-Path $src) {
        Copy-Item $src -Destination $BackupFolder -Force
    }
}

# --- Limpieza de binarios y temporales ---
$PathsToClean = @(
    "bin",
    "obj",
    "*.pdb",
    "*.deps.json",
    "*.runtimeconfig.json",
    "*.log",
    "*.bak",
    "*.tmp",
    "*.old"
)

Write-Host ""
Write-Host "🧹 Eliminando archivos temporales y binarios innecesarios..." -ForegroundColor Cyan
$deletedCount = 0

foreach ($pattern in $PathsToClean) {
    Get-ChildItem -Path $ProjectRoot -Recurse -Force -ErrorAction SilentlyContinue -Include $pattern | ForEach-Object {
        try {
            Remove-Item $_.FullName -Force -Recurse -ErrorAction SilentlyContinue
            Add-Content $LogFile ("Eliminado: " + $_.FullName)
            $deletedCount++
        } catch {
            Add-Content $LogFile ("Error eliminando: " + $_.FullName)
        }
    }
}

# --- Limpieza completada ---
Write-Host ""
Write-Host "✅ Limpieza completada." -ForegroundColor Green
Write-Host "🗂️ Archivos eliminados: $deletedCount"
Write-Host "📜 Registro: $LogFile"
Write-Host "💾 Backup de seguridad creado en: $BackupFolder"
Write-Host "-------------------------------------------------------------"
Write-Host "🔒 Ningún archivo fuente o script fue modificado ni eliminado."
Write-Host "-------------------------------------------------------------"
Write-Host ""
