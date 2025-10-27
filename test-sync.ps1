# test-sync.ps1
# 🔧 Inicia dos instancias del cliente en modo laboratorio local.
# Host y Client se comunican por WebSocket interno (127.0.0.1:8081)

Write-Host "🧪 Iniciando prueba de sincronización en modo laboratorio..." -ForegroundColor Cyan

# Rutas absolutas seguras
$exePath = Join-Path $PSScriptRoot "SharedCockpitClient.exe"

# Verifica existencia
if (-not (Test-Path $exePath)) {
    Write-Host "❌ No se encontró el ejecutable: $exePath" -ForegroundColor Red
    Write-Host "Ejecutá primero: dotnet publish -c Release -r win-x64"
    exit 1
}

# Host (piloto principal)
Start-Process -FilePath $exePath -ArgumentList "--lab", "--role", "host" -WindowStyle Normal

# Espera 2 segundos para que inicie el host
Start-Sleep -Seconds 2

# Cliente (copiloto)
Start-Process -FilePath $exePath -ArgumentList "--lab", "--role", "client", "--peer", "127.0.0.1:8081" -WindowStyle Normal

Write-Host "✅ Consolas iniciadas: Host + Client (modo laboratorio activo)" -ForegroundColor Green
Write-Host "🧭 Usá los comandos: flaps <num>, gear, lights on/off, engine on/off, door open/close, state, exit"
