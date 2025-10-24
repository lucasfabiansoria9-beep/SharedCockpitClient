using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network
{
    public sealed class SyncController : IDisposable
    {
        private readonly SimConnectManager sim;

        private bool isHost = true;
        private string webSocketUrl = string.Empty;
        private bool warnedNoConnection;
        private string localRole = "pilot";

        private WebSocketManager? ws;
        private WebSocketHost? hostServer;
        private CancellationTokenSource? loopToken;

        public SyncController(SimConnectManager sim)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
            this.sim.OnSimStateChanged += HandleSimStateChanged;
        }

        public async Task RunAsync()
        {
            Console.OutputEncoding = Encoding.UTF8;

            ConfigureMode();
            SetupWebSocket();
            SetupShutdownHandlers();

            if (!sim.Initialize())
                return;

            Logger.Info("Presiona Ctrl+C para cerrar la aplicación.");
            Logger.Info("🌍 Cabina compartida activa: sincronización total entre piloto y copiloto.");
            Logger.Info("🔁 Transmitiendo y aplicando estado del simulador en tiempo real.");

            using var cts = new CancellationTokenSource();
            loopToken = cts;

            try
            {
                while (!cts.IsCancellationRequested)
                {
                    sim.ReceiveMessage();
                    await Task.Delay(100, cts.Token);
                }
            }
            catch (TaskCanceledException)
            {
                // Ignorado: cancelación esperada al cerrar
            }
            finally
            {
                loopToken = null;
            }
        }

        private void ConfigureMode()
        {
            Logger.Info("Selecciona modo de operación:");
            Logger.Info("1) Host (piloto principal)");
            Logger.Info("2) Cliente (copiloto)");
            Console.Write("> ");

            var option = Console.ReadLine();
            isHost = option?.Trim() != "2";

            if (isHost)
            {
                var localIp = GetLocalIpAddress();

                Logger.Info("🚀 Modo HOST iniciado.");
                Logger.Info($"Tu dirección local es: {localIp}");
                Logger.Info($"Pídele al copiloto que se conecte a: ws://{localIp}:8081");
                Logger.Info("Esperando conexión del copiloto...");

                localRole = "pilot";
                sim.SetUserRole("PILOT");
            }
            else
            {
                Logger.Info("🌍 Ingresá la IP del piloto:");
                Console.Write("> ");

                var hostInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(hostInput))
                    hostInput = "127.0.0.1";

                // ✅ Si el usuario ya puso el puerto, no agregamos otro
                if (!hostInput.StartsWith("ws://"))
                    hostInput = "ws://" + hostInput;

                if (!hostInput.Contains(":"))
                    hostInput += ":8081";

                webSocketUrl = hostInput;
                Logger.Info($"🌍 Intentando conectar con {webSocketUrl}...");

                localRole = "copilot";
                sim.SetUserRole("COPILOT");
            }

            Logger.Info(string.Empty);
        }

        private void SetupWebSocket()
        {
            if (isHost)
            {
                hostServer = new WebSocketHost(8081);

                hostServer.OnClientConnected += (id) =>
                {
                    Logger.Info("🟢 Copiloto conectado correctamente.");
                    warnedNoConnection = false;
                    SendFullSnapshotToClient(id);
                };

                hostServer.OnClientDisconnected += (id) =>
                {
                    Logger.Warn("🔴 Copiloto desconectado.");
                    warnedNoConnection = false;
                };

                hostServer.OnMessage += OnWebSocketMessage;
                hostServer.Start();
            }
            else
            {
                ws = new WebSocketManager(webSocketUrl, sim);

                ws.OnOpen += () =>
                {
                    Logger.Info("✅ Conectado correctamente al host (piloto).");
                    warnedNoConnection = false;
                    SendFullSnapshotToHost();
                };

                ws.OnError += (msg) =>
                {
                    Logger.Error("❌ No se pudo conectar al host. Verificá la IP o que el piloto esté en modo HOST.");
                    if (!string.IsNullOrWhiteSpace(msg))
                        Logger.Warn("Detalles del error: " + msg);
                };

                ws.OnClose += () => Logger.Warn("🔌 Conexión WebSocket cerrada.");
                ws.OnMessage += OnWebSocketMessage;
                ws.Connect();
            }
        }

        private static string GetLocalIpAddress()
        {
            try
            {
                return Dns.GetHostAddresses(Dns.GetHostName())
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)
                    ?.ToString() ?? "127.0.0.1";
            }
            catch (SocketException ex)
            {
                Logger.Warn($"⚠️ No se pudo obtener la IP local: {ex.Message}");
                return "127.0.0.1";
            }
        }

        private void SetupShutdownHandlers()
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Shutdown();
                loopToken?.Cancel();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
        }

        private void OnWebSocketMessage(string message)
        {
            FlightDisplay.ShowReceivedMessage(message);

            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (message.StartsWith("ROLE:", StringComparison.OrdinalIgnoreCase))
            {
                var assignedRole = message.Substring("ROLE:".Length).Trim();
                if (!string.IsNullOrWhiteSpace(assignedRole))
                {
                    sim.SetUserRole(assignedRole);
                }
                return;
            }

            ApplyRemotePayload(message);
        }

        private void HandleSimStateChanged(string payload)
        {
            SendToPeers(payload);
        }

        private void SendToPeers(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload))
            {
                return;
            }

            if (isHost)
            {
                if (hostServer != null && hostServer.HasClients)
                {
                    hostServer.Broadcast(payload);
                    FlightDisplay.ShowSentToCopilot(payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Logger.Info("⌛ Aún no hay copilotos conectados. Los datos se enviarán automáticamente en cuanto se unan.");
                    warnedNoConnection = true;
                }
            }
            else
            {
                if (ws != null)
                {
                    ws.Send(payload);
                    FlightDisplay.ShowSentToHost(payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Logger.Warn("⚠️ No se puede enviar datos porque el WebSocket no está conectado.");
                    warnedNoConnection = true;
                }
            }
        }

        private void Shutdown()
        {
            ws?.Close();
            hostServer?.Stop();
            sim.Dispose();
        }

        public void Dispose()
        {
            sim.OnSimStateChanged -= HandleSimStateChanged;
            Shutdown();
            loopToken?.Cancel();
            loopToken?.Dispose();
        }

        private void SendFullSnapshotToClient(Guid clientId)
        {
            if (hostServer == null)
            {
                return;
            }

            var snapshot = sim.GetFullSnapshotPayload();
            if (string.IsNullOrWhiteSpace(snapshot))
            {
                Logger.Warn("⚠️ No hay estado disponible para sincronizar al nuevo copiloto.");
                return;
            }

            hostServer.SendToClient(clientId, snapshot);
            Logger.Info("📡 Snapshot completo enviado al copiloto para re-sincronizar.");
        }

        private void SendFullSnapshotToHost()
        {
            if (ws == null)
            {
                return;
            }

            var snapshot = sim.GetFullSnapshotPayload();
            if (string.IsNullOrWhiteSpace(snapshot))
            {
                Logger.Warn("⚠️ No hay estado local para enviar al piloto todavía.");
                return;
            }

            ws.Send(snapshot);
            Logger.Info("📡 Snapshot completo enviado al host para sincronización inicial.");
        }

        private void ApplyRemotePayload(string message)
        {
            try
            {
                using var doc = JsonDocument.Parse(message);
                var root = doc.RootElement;

                if (root.TryGetProperty("src", out var srcElement))
                {
                    var source = srcElement.GetString();
                    if (!string.IsNullOrWhiteSpace(source) &&
                        string.Equals(source, localRole, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }
            catch (JsonException ex)
            {
                Logger.Warn($"⚠️ Mensaje remoto inválido: {ex.Message}");
                return;
            }

            sim.ApplyRemoteCommand(message);
        }
    }
}
