using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Utils;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace SharedCockpitClient
{
    internal class Program
    {
        static async Task Main()
        {
            // 🧱 Fuerza la alerta del firewall o agrega la regla automáticamente
            EnsureFirewallRuleOrPrompt(8081);

            Logger.Info("🛫 Iniciando SharedCockpitClient...");
            using var sync = new SyncController(new SimConnectManager());
            await sync.RunAsync();
        }

        /// <summary>
        /// Asegura que el firewall de Windows permita conexiones del SharedCockpitClient.
        /// Muestra la alerta clásica la primera vez, o agrega la regla automáticamente.
        /// </summary>
        static void EnsureFirewallRuleOrPrompt(int port)
        {
            try
            {
                // 🎯 Intenta abrir un socket temporal (dispara la alerta de Windows Firewall si no hay permiso)
                using var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                // 🔒 Si el firewall bloquea, Windows mostrará automáticamente el cartel “Permitir acceso”.
            }

            // 🔁 Si no se muestra la alerta (por antivirus o políticas), agregamos la regla manualmente.
            try
            {
                var process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments =
                    $"advfirewall firewall add rule name=\"SharedCockpitClient\" " +
                    $"dir=in action=allow protocol=TCP localport={port}";
                process.StartInfo.Verb = "runas"; // Pide elevación (UAC)
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit(2000);
            }
            catch
            {
                // El usuario puede cancelar el UAC, no interrumpe la app.
            }
        }
    }
}
