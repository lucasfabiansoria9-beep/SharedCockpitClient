param(
  [string]$HostIp = "127.0.0.1",
  [int]$Port = 8081
)

Write-Host "Lanzando host..."
Start-Process -FilePath "./SharedCockpitClient.exe" -ArgumentList "--role host" -NoNewWindow
Start-Sleep -Seconds 5

Write-Host "Lanzando cliente..."
Start-Process -FilePath "./SharedCockpitClient.exe" -ArgumentList "--role client --peer $HostIp`:$Port" -NoNewWindow
Write-Host "Sesión iniciada. Revise los logs en consola para verificar sincronización."
