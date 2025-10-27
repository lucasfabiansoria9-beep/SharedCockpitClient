using System;

namespace SharedCockpitClient
{
    /// <summary>
    /// Banderas y configuración global de ejecución.
    /// </summary>
    public static class GlobalFlags
    {
        /// <summary>
        /// Indica si el modo laboratorio está activo.
        /// </summary>
        public static bool IsLabMode { get; private set; } = InitializeLabMode();

        /// <summary>
        /// Fuerza el modo laboratorio desde código (por ejemplo con --lab).
        /// </summary>
        public static void ForceLabMode() => IsLabMode = true;

        private static bool InitializeLabMode()
        {
            try
            {
                // Si existe una variable de entorno que habilita el modo lab, activamos.
                // (No revelamos ni validamos el valor aquí por seguridad)
                var pin = Environment.GetEnvironmentVariable("SCC_LAB_PIN");
                return !string.IsNullOrWhiteSpace(pin);
            }
            catch
            {
                return false;
            }
        }
    }
}
