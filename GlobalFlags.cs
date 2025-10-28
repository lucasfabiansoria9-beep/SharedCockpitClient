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
        public static bool IsLabMode { get; private set; } = InitializeLabMode();

        /// <summary>
        /// Rol del usuario en la sesión (PILOT / COPILOT).
        /// </summary>
        public static string UserRole { get; set; } = "PILOT";

        /// <summary>
        /// Rol lógico del nodo (HOST / CLIENT) para WebSocketManager.
        /// </summary>
        public static string Role { get; set; } = "HOST";

        /// <summary>
        /// Dirección IP o host remoto al que se conectará el cliente.
        /// </summary>
        public static string PeerAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// Fuerza el modo laboratorio desde código (por ejemplo con --lab).
        /// </summary>
        public static void ForceLabMode() => IsLabMode = true;

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
