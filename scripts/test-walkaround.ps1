if (-not (Test-Path './logs')) { New-Item -ItemType Directory -Path './logs' | Out-Null }

Write-Host "Simulando poses remotas cada 0.2s"
for ($i = 0; $i -lt 20; $i++) {
    $lat = -34.5 + ($i * 0.0001)
    $lon = -58.4 + ($i * 0.0001)
    $payload = @{ type = 'avatarPose'; originId = [guid]::NewGuid().ToString(); sequence = $i; pose = @{ lat = $lat; lon = $lon; alt = 75; hdg = 180; pitch = 0; bank = 0; state = 'walk' } } | ConvertTo-Json -Depth 3
    Write-Host "[Test] -> $payload"
    Add-Content -Path ".\logs\walkaround-test.jsonl" -Value $payload
    Start-Sleep -Milliseconds 200
}
Write-Host "Logs escritos en .\\logs\\walkaround-test.jsonl"
