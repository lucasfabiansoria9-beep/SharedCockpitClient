using System;
using System.Threading;
using SharedCockpitClient.FlightData;

namespace SharedCockpitClient
{
    /// <summary>
    /// Consola interactiva para modo laboratorio. Permite cancelarse automáticamente
    /// cuando se establece conexión real con SimConnect.
    /// </summary>
    public static class LabConsole
    {
        private static readonly object s_gate = new();
        private static CancellationTokenSource? s_cts;
        private static bool s_running;

        public static bool IsRunning
        {
            get
            {
                lock (s_gate)
                {
                    return s_running;
                }
            }
        }

        public static CancellationToken CancellationToken
        {
            get
            {
                lock (s_gate)
                {
                    return s_cts?.Token ?? CancellationToken.None;
                }
            }
        }

        public static bool StartIfEnabledAndOffline(SimConnectManager? sim)
        {
            if (sim == null)
                return false;

            if (!GlobalFlags.IsLabMode || sim.IsConnected)
                return false;

            lock (s_gate)
            {
                if (s_running)
                    return true;

                s_cts = new CancellationTokenSource();
                s_running = true;
            }

            Console.WriteLine("Lab Mode activo. Comandos: set <ruta> <valor>, toggle <ruta>, pose <lat> <lon> <alt> <hdg>, state, exit");
            return true;
        }

        public static void StopSafe()
        {
            CancellationTokenSource? cts = null;

            lock (s_gate)
            {
                if (!s_running)
                    return;

                s_running = false;
                cts = s_cts;
                s_cts = null;
            }

            try { cts?.Cancel(); }
            catch { }

            cts?.Dispose();
        }
    }
}
