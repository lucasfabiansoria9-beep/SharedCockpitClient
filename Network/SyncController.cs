using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network;

public sealed class SyncController : IDisposable
{
    private readonly SimConnectManager sim;

    private bool isHost = true;
    private string webSocketUrl = string.Empty;
    private bool warnedNoConnection;

    private WebSocketManager? ws;
    private WebSocketHost? hostServer;
    private CancellationTokenSource? loopToken;

    public SyncController(SimConnectManager sim)
    {
        this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
        this.sim.OnFlightDataReady += HandleFlightDataReady;
    }

    public async Task RunAsync()
    {
        Console.OutputEncoding = Encoding.UTF8;

        ConfigureMode();
        SetupWebSocket();
        SetupShutdownHandlers();

        if (!sim.Initialize())
        {
            return;
        }

        Logger.Info("Presiona Ctrl+C para cerrar la aplicación.");

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
            // Ignorado: la cancelación es el flujo esperado al cerrar la aplicación.
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

            sim.SetUserRole("PILOT");
        }
        else
        {
            Logger.Info("🌍 Ingresá la IP del piloto:");
            Console.Write("> ");

            var hostInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostInput))
            {
                hostInput = "127.0.0.1";
            }

            webSocketUrl = $"ws://{hostInput.Trim()}:8081";
            Logger.Info($"🌍 Intentando conectar con {webSocketUrl}...");
        }

        Logger.Info(string.Empty);
    }

    private void SetupWebSocket()
    {
        if (isHost)
        {
            hostServer = new WebSocketHost(8081);
            hostServer.ReservePilotRole();
            hostServer.OnClientConnected += () =>
            {
                warnedNoConnection = false;
            };
            hostServer.OnClientDisconnected += () =>
            {
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
                Logger.Info("✅ Conectado correctamente al host.");
                warnedNoConnection = false;
            };
            ws.OnError += (msg) =>
            {
                Logger.Error("❌ No se pudo conectar al host. Verificá la IP o que el piloto esté en modo HOST.");

                if (!string.IsNullOrWhiteSpace(msg))
                {
                    Logger.Warn("Detalles del error: " + msg);
                }
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
    }

    private void HandleFlightDataReady(string payload)
    {
        SendToPeers(payload);
    }

    private void SendToPeers(string payload)
    {
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
        sim.OnFlightDataReady -= HandleFlightDataReady;
        Shutdown();
        loopToken?.Cancel();
        loopToken?.Dispose();
    }
}
