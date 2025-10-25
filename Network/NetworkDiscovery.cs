using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Permite descubrir automáticamente cabinas compartidas (hosts) en la red local.
    /// Soporta sesiones públicas y privadas con contraseña local.
    /// </summary>
    public class NetworkDiscovery
    {
        private readonly UdpClient udpClient;
        private readonly string sessionName;
        private readonly string localIp;
        private readonly int wsPort;
        private readonly ConcurrentDictionary<string, string> passwords = new(); // name -> password
        private bool isBroadcasting;
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
        /// El host empieza a anunciar su sesión.
        /// </summary>
        public void StartBroadcast(bool isPublic, string password)
        {
            isBroadcasting = true;
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

                        await Task.Delay(2000, token); // cada 2 seg
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn($"⚠️ Error en broadcast LAN: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// El copiloto empieza a escuchar anuncios LAN.
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

                        var name = doc.RootElement.GetProperty("name").GetString() ?? "CabinaDesconocida";
                        var ip = doc.RootElement.GetProperty("ip").GetString() ?? "0.0.0.0";
                        var isPublic = doc.RootElement.TryGetProperty("isPublic", out var p) && p.GetBoolean();

                        OnHostDiscovered?.Invoke(ip, name, isPublic);
                    }
                    catch (Exception ex)
                    {
                        if (!token.IsCancellationRequested)
                            Logger.Warn($"⚠️ Error escuchando broadcast: {ex.Message}");
                    }
                }
            });
        }

        /// <summary>
        /// Valida la contraseña para una sesión privada (solo local, sin red).
        /// </summary>
        public bool ValidatePassword(string session, string entered)
        {
            if (passwords.TryGetValue(session, out var correct))
                return correct == entered;
            return false;
        }

        public void Stop()
        {
            try
            {
                broadcastToken?.Cancel();
                listenToken?.Cancel();
                udpClient.Close();
            }
            catch { }
        }
    }
}
