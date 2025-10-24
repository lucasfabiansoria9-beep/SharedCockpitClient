Write-Host "🔄 Actualizando proyecto SharedCockpitClient desde GitHub..."
Set-Location "E:\Descargas\proyecto cabina compartida\SharedCockpitClient"

git pull origin main

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Actualización completada correctamente."
    Write-Host "🛠️ Compilando el proyecto..."
    dotnet publish "SharedCockpitClient.csproj" -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=false -o "E:\Descargas\proyecto cabina compartida\SharedCockpitClient\bin\Release\net8.0-windows\win-x64\publish"
    if ($LASTEXITCODE -eq 0) {
        Write-Host "🚀 Proyecto compilado exitosamente."
    } else {
        Write-Host "⚠️ Hubo un error durante la compilación."
    }
} else {
    Write-Host "❌ No se pudo actualizar desde Git. Verificá tu conexión o rama."
}
