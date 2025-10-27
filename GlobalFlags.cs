using System;

namespace SharedCockpitClient
{
    /// <summary>
    /// Flags globales y control de activación del modo laboratorio.
    /// El PIN se obtiene de la variable de entorno "SCC_LAB_PIN" para evitar dejar secretos en el código.
    /// </summary>
    public static class GlobalFlags
    {
        public static bool IsLabMode { get; private set; }

        /// <summary>
        /// Intenta activar modo laboratorio comparando el PIN provisto con la variable de entorno.
        /// Si la variable de entorno no está definida, por compatibilidad se acepta el PIN por defecto (9091).
        /// Recomendado: definir SCC_LAB_PIN en la máquina de desarrollo para mayor seguridad.
        /// </summary>
        public static bool TryEnableLabMode(string pin)
        {
            var secret = GetSecretPin();
            if (secret != null)
            {
                if (string.Equals(pin, secret, StringComparison.Ordinal))
                {
                    EnableLabMode();
                    return true;
                }
                return false;
            }

            // Fallback: por compatibilidad aceptamos 9091 solo si no hay variable de entorno.
            if (pin == "9091")
            {
                EnableLabMode();
                return true;
            }

            return false;
        }

        public static void EnableLabMode()
        {
            IsLabMode = true;
            ShowActivationMessage();
        }

        private static string? GetSecretPin()
        {
            try
            {
                // Buscar variable de entorno (no sensible al control de versiones)
                var env = Environment.GetEnvironmentVariable("SCC_LAB_PIN");
                if (!string.IsNullOrWhiteSpace(env))
                    return env.Trim();
            }
            catch { }
            return null;
        }

        private static void ShowActivationMessage()
        {
            var prev = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("🧪==============================================🧪");
            Console.WriteLine("   LAB MODE ACTIVADO — ENTORNO DE DESARROLLO");
            Console.WriteLine("   (Sin conexión real a MSFS - solo para pruebas)");
            Console.WriteLine("🧪==============================================🧪");
            Console.ForegroundColor = prev;
        }

        public static string GetCurrentModeLabel()
            => IsLabMode ? "🧪 Modo Laboratorio" : "✈️ Modo Real (SimConnect)";
    }
}
