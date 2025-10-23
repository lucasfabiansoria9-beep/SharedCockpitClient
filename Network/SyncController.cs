using System;
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
    private string webSocketUrl = "ws://localhost:8081";
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
        Console.Write("Opcin [1]: ");

        var option = Console.ReadLine();
        isHost = option?.Trim() == "2" ? false : true;

        if (isHost)
        {
            Logger.Info("➡️  Modo HOST seleccionado. Este equipo iniciará el servidor WebSocket en el puerto 8081.");
            Logger.Info("   Pide al copiloto que se conecte a ws://<tu-ip>:8081");
        }
        else
        {
            Console.Write("IP o hostname del piloto principal [localhost]: ");
            var hostInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostInput))
            {
                hostInput = "localhost";
            }

            webSocketUrl = $"ws://{hostInput.Trim()}:8081";
            Logger.Info($"➡️  Modo CLIENTE seleccionado. Intentando conectar a {webSocketUrl}");
        }

        Logger.Info(string.Empty);
    }

    private void SetupWebSocket()
    {
        if (isHost)
        {
            hostServer = new WebSocketHost(8081);
            hostServer.OnClientConnected += () =>
            {
                Logger.Info("✅ Copiloto conectado. Comenzaremos a enviar los datos de vuelo en cuanto cambien.");
                warnedNoConnection = false;
            };
            hostServer.OnClientDisconnected += () => Logger.Info("ℹ️ Copiloto desconectado del servidor.");
            hostServer.OnMessage += OnWebSocketMessage;
            hostServer.Start();
        }
        else
        {
            ws = new WebSocketManager(webSocketUrl);
            ws.OnOpen += () =>
            {
                Logger.Info("🌐 Conectado al piloto principal.");
                warnedNoConnection = false;
            };
            ws.OnError += (msg) => Logger.Warn("⚠️ Error WebSocket: " + msg);
            ws.OnClose += () => Logger.Info("🔌 Conexión WebSocket cerrada");
            ws.OnMessage += OnWebSocketMessage;
            ws.Connect();
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
