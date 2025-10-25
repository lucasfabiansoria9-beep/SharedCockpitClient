Write-Host "✈️  Iniciando simulación de cabina compartida local..." -ForegroundColor Cyan

# Ruta del ejecutable publicado
$exePath = "bin\Release\net8.0-windows\win-x64\publish\SharedCockpitClient.exe"

if (-Not (Test-Path $exePath)) {
    Write-Host "⚠️  No se encontró el ejecutable compilado. Ejecutá ./update.ps1 primero." -ForegroundColor Red
    exit 1
}

# Puertos de prueba
$hostPort = 8080
$clientPort = 8081

# Instancia Piloto
Start-Process -FilePath $exePath -ArgumentList "--role host --port $hostPort" -WorkingDirectory (Split-Path $exePath)
Write-Host "🧑‍✈️  Piloto iniciado en ws://localhost:$hostPort" -ForegroundColor Green

Start-Sleep -Seconds 2

# Instancia Copiloto
Start-Process -FilePath $exePath -ArgumentList "--role client --connect ws://localhost:$hostPort --port $clientPort" -WorkingDirectory (Split-Path $exePath)
Write-Host "👨‍✈️  Copiloto iniciado y conectado al Piloto en ws://localhost:$hostPort" -ForegroundColor Green

Write-Host ""
Write-Host "🛰️  Ambas instancias están corriendo. Observá los logs en consola o en archivos de salida." -ForegroundColor Cyan
Write-Host "💡  Si todo está bien, deberías ver sincronización de variables (flaps, luces, AP, etc.) en ambos lados."
