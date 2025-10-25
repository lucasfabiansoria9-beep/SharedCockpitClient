using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Utils;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            bool useMockSim = args.Length > 0 && args[0].Equals("--mock-sim", StringComparison.OrdinalIgnoreCase);
            EnsureFirewallRuleOrPrompt(8081);

            Logger.Info("🛫 Iniciando SharedCockpitClient...");

            if (useMockSim)
            {
                Logger.Info("🧪 Modo de simulación interna activo - no se usará SimConnect.");
                Logger.Info("🧪 Generando datos simulados de vuelo...");

                // No crear ni usar SimConnectManager real
                var mockManager = new SimConnectManager();
                var mock = new SimDataMock(mockManager);
                mock.Start(); // genera snapshots ficticios

                using var sync = new SyncController(mockManager);
                await sync.RunAsync();
            }
            else
            {
                using var sim = new SimConnectManager();
                using var sync = new SyncController(sim);
                await sync.RunAsync();
            }
        }

        static void EnsureFirewallRuleOrPrompt(int port)
        {
            try
            {
                using var listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                listener.Stop();
            }
            catch (SocketException)
            {
                // Windows mostrará el aviso de firewall automáticamente
            }

            try
            {
                var process = new Process();
                process.StartInfo.FileName = "netsh";
                process.StartInfo.Arguments =
                    $"advfirewall firewall add rule name=\"SharedCockpitClient\" " +
                    $"dir=in action=allow protocol=TCP localport={port}";
                process.StartInfo.Verb = "runas";
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                process.WaitForExit(2000);
            }
            catch
            {
                // No interrumpir si el usuario cancela
            }
        }
    }
}
