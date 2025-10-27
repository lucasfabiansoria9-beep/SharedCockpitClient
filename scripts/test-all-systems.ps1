if (-not (Test-Path './logs')) { New-Item -ItemType Directory -Path './logs' | Out-Null }

$paths = @(
    'Systems.Autopilot.AP_MASTER',
    'Systems.Autopilot.HDG',
    'Systems.Lights.Beacon',
    'Systems.AntiIce.Pitot',
    'Systems.Fuel.Pump[1]',
    'Controls.Gear.Handle',
    'Controls.Flaps.Handle'
)

foreach ($path in $paths) {
    Write-Host "[Test] Cambiando $path"
    $payload = @{ type = 'stateChange'; prop = $path; value = $true; originId = [guid]::NewGuid(); sequence = 1; serverTime = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() } | ConvertTo-Json -Compress
    Add-Content -Path ".\logs\all-systems.jsonl" -Value $payload
}
Write-Host "Prueba registrada en .\\logs\\all-systems.jsonl"
