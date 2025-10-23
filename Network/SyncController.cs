using System;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network;

public sealed class SyncController : IDisposable
{
    private readonly SimConnectManager _sim;
    private readonly object _broadcastLock = new();
    private readonly TimeSpan _broadcastInterval = TimeSpan.FromMilliseconds(100);

    private WebSocketHost? _host;
    private WebSocketManager? _client;

    private bool _isHost = true;
    private string _webSocketUrl = "ws://localhost:8081";

    private string? _pendingPayload;
    private DateTime _lastBroadcastUtc = DateTime.MinValue;
    private DateTime _suppressBroadcastUntilUtc = DateTime.MinValue;

    private FlightSnapshot? _lastSentSnapshot;
    private FlightSnapshot? _lastRemoteSnapshot;
    private FlightSnapshot? _lastLocalSnapshot;

    private DateTime _lastRemoteUtc = DateTime.MinValue;
    private bool _remoteConnected;
    private bool _latencyWarned;
    private bool _disposed;

    public SyncController(SimConnectManager sim)
    {
        _sim = sim ?? throw new ArgumentNullException(nameof(sim));
        _sim.OnFlightDataUpdated += HandleLocalFlightData;
    }

    public async Task RunAsync()
    {
        FlightDisplay.Initialize();
        ConfigureMode();
        FlightDisplay.ShowWaiting("⚠️ esperando datos...");

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        AppDomain.CurrentDomain.ProcessExit += (_, __) => cts.Cancel();

        var simTask = _sim.StartListeningAsync(cts.Token);
        var wsTask = ListenWebSocketAsync(cts.Token);

        try
        {
            await Task.WhenAll(simTask, wsTask).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
        finally
        {
            Dispose();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _host?.Dispose();
        _client?.Dispose();
        _sim.Dispose();
    }

    private void ConfigureMode()
    {
        Logger.Info("Selecciona modo de operación:");
        Logger.Info("1) Host (piloto principal)");
        Logger.Info("2) Cliente (copiloto)");
        Console.Write("Opción [1]: ");
        var option = Console.ReadLine();
        _isHost = option?.Trim() != "2";

        if (_isHost)
        {
            Logger.Info("➡️  Modo HOST seleccionado. Se iniciará el servidor en el puerto 8081.");
            StartHost();
        }
        else
        {
            Console.Write("IP o hostname del host [localhost]: ");
            var hostInput = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(hostInput))
            {
                hostInput = "localhost";
            }

            _webSocketUrl = $"ws://{hostInput.Trim()}:8081";
            Logger.Info($"➡️  Modo CLIENTE seleccionado. Intentando conectar a {_webSocketUrl}");
            StartClient();
        }

        Console.WriteLine();
        Logger.Info("Presiona Ctrl+C para cerrar la aplicación.");
    }

    private void StartHost()
    {
        _host = new WebSocketHost(8081);
        _host.OnClientConnected += () =>
        {
            _remoteConnected = true;
            _latencyWarned = false;
            FlightDisplay.ShowWaiting("⚠️ esperando datos remotos...");
        };
        _host.OnClientDisconnected += () =>
        {
            _remoteConnected = false;
            _latencyWarned = false;
            FlightDisplay.ShowWaiting("⚠️ esperando copiloto...");
        };
        _host.OnMessage += HandleRemoteMessage;
        _host.Start();
    }

    private void StartClient()
    {
        _client = new WebSocketManager(_webSocketUrl);
        _client.OnOpen += () =>
        {
            _remoteConnected = true;
            _latencyWarned = false;
            FlightDisplay.ShowWaiting("⚠️ esperando datos del host...");
        };
        _client.OnClose += () =>
        {
            _remoteConnected = false;
            FlightDisplay.ShowWaiting("⚠️ reconectando...");
        };
        _client.OnError += _ => FlightDisplay.ShowWaiting("⚠️ error de red, reintentando...");
        _client.OnMessage += HandleRemoteMessage;
        _client.Connect();
    }

    private void HandleLocalFlightData(FlightSnapshot snapshot)
    {
        _lastLocalSnapshot = snapshot;

        bool showLocal = _isHost || !_lastRemoteUtc.IsRecent(0.5);
        bool inSync = _remoteConnected && (!_isHost || _host?.HasClients == true) && (_isHost || _client?.IsConnected == true);

        if (showLocal)
        {
            FlightDisplay.ShowFlightData(snapshot, inSync);
        }

        if (!_remoteConnected)
        {
            return;
        }

        if (!snapshot.HasPrimaryFlightValues())
        {
            return;
        }

        if (!snapshot.IsMeaningfullyDifferent(_lastSentSnapshot, 0.002))
        {
            return;
        }

        QueueBroadcast(snapshot);
        _lastSentSnapshot = new FlightSnapshot
        {
            Attitude = snapshot.Attitude,
            Position = snapshot.Position,
            Speed = snapshot.Speed,
            Controls = snapshot.Controls,
            Cabin = snapshot.Cabin,
            Doors = snapshot.Doors,
            Ground = snapshot.Ground,
            Timestamp = snapshot.Timestamp
        };
    }

    private void HandleRemoteMessage(string message)
    {
        if (!FlightSnapshot.TryFromJson(message, out var snapshot) || snapshot == null)
        {
            return;
        }

        if (!snapshot.HasPrimaryFlightValues())
        {
            return;
        }

        snapshot.Timestamp = DateTime.UtcNow;
        _lastRemoteSnapshot = snapshot;
        _lastRemoteUtc = snapshot.Timestamp;
        _remoteConnected = true;
        _latencyWarned = false;
        _suppressBroadcastUntilUtc = DateTime.UtcNow.AddMilliseconds(200);

        if (!_isHost)
        {
            FlightDisplay.ShowFlightData(snapshot, true);
        }
        else
        {
            Logger.Debug("Datos recibidos del copiloto");
            bool localRecent = _lastLocalSnapshot != null && _lastLocalSnapshot.Timestamp.IsRecent(0.5);
            if (!localRecent)
            {
                FlightDisplay.ShowFlightData(snapshot, true);
            }
        }
    }

    private void QueueBroadcast(FlightSnapshot snapshot)
    {
        string payload = snapshot.ToJson();
        lock (_broadcastLock)
        {
            var now = DateTime.UtcNow;
            if (now < _suppressBroadcastUntilUtc)
            {
                _pendingPayload = payload;
                return;
            }

            if (now - _lastBroadcastUtc < _broadcastInterval)
            {
                _pendingPayload = payload;
                return;
            }

            _pendingPayload = null;
            _lastBroadcastUtc = now;
            SendPayload(payload);
        }
    }

    private void FlushPendingBroadcast()
    {
        string? payload = null;

        lock (_broadcastLock)
        {
            if (_pendingPayload == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            if (now < _suppressBroadcastUntilUtc)
            {
                return;
            }

            if (now - _lastBroadcastUtc < _broadcastInterval)
            {
                return;
            }

            payload = _pendingPayload;
            _pendingPayload = null;
            _lastBroadcastUtc = now;
        }

        if (payload != null)
        {
            SendPayload(payload);
        }
    }

    private void SendPayload(string payload)
    {
        if (_isHost)
        {
            if (_host?.HasClients == true)
            {
                _host.Broadcast(payload);
            }
            else if (_remoteConnected)
            {
                _remoteConnected = false;
                FlightDisplay.ShowWaiting("⚠️ esperando copiloto...");
            }
        }
        else
        {
            if (_client?.IsConnected == true)
            {
                _client.Send(payload);
            }
            else if (_remoteConnected)
            {
                _remoteConnected = false;
                FlightDisplay.ShowWaiting("⚠️ reconectando...");
            }
        }
    }

    private async Task ListenWebSocketAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            FlushPendingBroadcast();
            MonitorLatency();

            try
            {
                await Task.Delay(100, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void MonitorLatency()
    {
        var now = DateTime.UtcNow;
        var lastDisplay = FlightDisplay.LastUpdateUtc;

        if (lastDisplay != DateTime.MinValue)
        {
            var age = now - lastDisplay;
            if (age > TimeSpan.FromSeconds(3))
            {
                FlightDisplay.ShowNoData();
            }
            else if (age > TimeSpan.FromSeconds(1))
            {
                FlightDisplay.ShowLagging(age);
            }
        }

        if (_remoteConnected && _lastRemoteUtc != DateTime.MinValue)
        {
            if (now - _lastRemoteUtc > TimeSpan.FromSeconds(2))
            {
                if (!_latencyWarned)
                {
                    Logger.Warn("⚠️ Retraso o pérdida de sincronización...");
                    _latencyWarned = true;
                }
            }
            else
            {
                _latencyWarned = false;
            }
        }
    }
}
