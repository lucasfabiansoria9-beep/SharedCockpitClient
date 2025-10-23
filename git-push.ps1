# === git-push.ps1 ===
# Script para subir cambios a GitHub fácilmente
# Lucas Soria - Proyecto SharedCockpitClient

# Mostrar ubicación actual
Write-Host "📁 Carpeta actual: $PWD"

# Preguntar por el mensaje del commit
$message = Read-Host "📝 Escribí un mensaje para el commit"

# Verificar estado de los archivos
git status

# Agregar todos los cambios
git add .

# Crear el commit con el mensaje que escribiste
git commit -m "$message"

# Subir a GitHub
git push

# Mostrar confirmación
Write-Host "✅ Cambios subidos correctamente a GitHub."
