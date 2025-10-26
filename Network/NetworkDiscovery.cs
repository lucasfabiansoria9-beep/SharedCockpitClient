using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Descubre automáticamente cabinas compartidas (hosts) en la red local.
    /// Soporta sesiones públicas y privadas con contraseña local.
    /// </summary>
    public class NetworkDiscovery : IDisposable
    {
        private readonly UdpClient udpClient;
        private readonly string sessionName;
        private readonly string localIp;
        private readonly int wsPort;
        private readonly ConcurrentDictionary<string, string> passwords = new(); // name -> password
        private CancellationTokenSource? broadcastToken;
        private CancellationTokenSource? listenToken;

        public event Action<string, string, bool>? OnHostDiscovered; // (ip, name, isPublic)

        public NetworkDiscovery(string sessionName, string localIp, int wsPort)
        {
            this.sessionName = sessionName;
            this.localIp = localIp;
            this.wsPort = wsPort;
            udpClient = new UdpClient { EnableBroadcast = true };
        }

        /// <summary>
        /// El host comienza a anunciar su sesión en la red local.
        /// </summary>
        public void StartBroadcast(bool isPublic, string password)
        {
            passwords[sessionName] = password;
            broadcastToken = new CancellationTokenSource();
            var token = broadcastToken.Token;

            _ = Task.Run(async () =>
            {
                var endpoint = new IPEndPoint(IPAddress.Broadcast, 55055);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var payload = new
                        {
                            name = sessionName,
                            ip = localIp,
                            port = wsPort,
                            isPublic
                        };

                        var json = JsonSerializer.Serialize(payload);
                        var bytes = Encoding.UTF8.GetBytes(json);
                        await udpClient.SendAsync(bytes, bytes.Length, endpoint);

                        await Task.Delay(2000, token); // cada 2 segundos
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            Logger.Warn($"⚠️ Error en broadcast LAN: {ex.Message}");
                    }
                }
            }, token);

            Logger.Info($"📡 Broadcasting activo: {sessionName} ({(isPublic ? "pública" : "privada")})");
        }

        /// <summary>
        /// El copiloto comienza a escuchar anuncios LAN.
        /// </summary>
        public void StartListening()
        {
            listenToken = new CancellationTokenSource();
            var token = listenToken.Token;

            _ = Task.Run(async () =>
            {
                using var listener = new UdpClient(55055);
                var endpoint = new IPEndPoint(IPAddress.Any, 0);

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var result = await listener.ReceiveAsync(token);
                        var json = Encoding.UTF8.GetString(result.Buffer);

                        using var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        var name = root.GetProperty("name").GetString() ?? "CabinaDesconocida";
                        var ip = root.GetProperty("ip").GetString() ?? "0.0.0.0";
                        var isPublic = root.TryGetProperty("isPublic", out var p) && p.GetBoolean();

                        OnHostDiscovered?.Invoke(ip, name, isPublic);
                    }
                    catch (OperationCanceledException)
                    {
                        // Cancel normal al detener la escucha
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            Logger.Warn($"⚠️ Error escuchando broadcast: {ex.Message}");
                    }
                }
            }, token);

            Logger.Info("👂 Escuchando broadcasts LAN...");
        }

        /// <summary>
        /// Valida la contraseña para una sesión privada.
        /// </summary>
        public bool ValidatePassword(string session, string entered)
        {
            return passwords.TryGetValue(session, out var correct) && correct == entered;
        }

        /// <summary>
        /// Detiene todas las tareas activas de broadcast y escucha.
        /// </summary>
        public void Stop()
        {
            try
            {
                broadcastToken?.Cancel();
                listenToken?.Cancel();
                udpClient?.Close();
                Logger.Info("🛑 NetworkDiscovery detenido correctamente.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error al detener NetworkDiscovery: {ex.Message}");
            }
        }

        public void Dispose() => Stop();
    }
}
