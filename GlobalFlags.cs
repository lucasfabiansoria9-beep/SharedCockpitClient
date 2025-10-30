using System;

namespace SharedCockpitClient
{
    /// <summary>
    /// Banderas y configuración global de ejecución.
    /// </summary>
    public static class GlobalFlags
    {
        /// <summary>
        /// Indica si el modo laboratorio está activo (--lab o variable SCC_LAB_PIN).
        /// </summary>
        public static bool IsLabMode { get; internal set; } = InitializeLabMode();

        /// <summary>
        /// Habilita el escaneo dinámico de ensamblados para el catálogo embebido.
        /// </summary>
        public static bool EnableAssemblyScanCatalog { get; set; } = false;

        /// <summary>
        /// Rol lógico del nodo (HOST / CLIENT) para WebSocketManager.
        /// </summary>
        public static string Role { get; set; } = string.Empty;

        /// <summary>
        /// Dirección IP o host remoto al que se conectará el cliente.
        /// </summary>
        public static string PeerAddress { get; set; } = string.Empty;

        /// <summary>
        /// Rol del usuario en la sesión (PILOT / COPILOT).
        /// </summary>
        public static string UserRole { get; set; } = "PILOT";

        /// <summary>
        /// Fuerza el modo laboratorio desde código (por ejemplo con --lab).
        /// </summary>
        public static void ForceLabMode() => IsLabMode = true;

        /// <summary>
        /// Desactiva explícitamente el modo laboratorio al detectar SimConnect real.
        /// </summary>
        public static void DisableLabMode() => IsLabMode = false;

        private static bool InitializeLabMode()
        {
            try
            {
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
