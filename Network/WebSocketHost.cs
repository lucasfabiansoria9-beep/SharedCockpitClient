using System;
using WebSocketSharp;
using WebSocketSharp.Server;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network;

public sealed class WebSocketHost : IDisposable
{
    private readonly WebSocketServer _server;
    private bool _disposed;

    public event Action OnClientConnected = delegate { };
    public event Action OnClientDisconnected = delegate { };
    public event Action<string> OnMessage = delegate { };

    public WebSocketHost(int port)
    {
        _server = new WebSocketServer(System.Net.IPAddress.Any, port);
        _server.AddWebSocketService("/", () => new HostBehavior(this));
    }

    public bool HasClients
    {
        get
        {
            if (!_server.IsListening)
            {
                return false;
            }

            var service = _server.WebSocketServices["/"];
            return service?.Sessions?.Count > 0;
        }
    }

    public void Start()
    {
        if (_disposed)
        {
            return;
        }

        if (_server.IsListening)
        {
            return;
        }

        _server.Start();
        Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{_server.Port}");
    }

    public void Broadcast(string message)
    {
        if (!_server.IsListening)
        {
            return;
        }

        var service = _server.WebSocketServices["/"];
        service?.Sessions?.Broadcast(message);
    }

    public void Stop()
    {
        if (!_server.IsListening)
        {
            return;
        }

        _server.Stop();
        Logger.Warn("🛑 Servidor WebSocket detenido");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();
    }

    private sealed class HostBehavior : WebSocketBehavior
    {
        private readonly WebSocketHost _parent;

        public HostBehavior(WebSocketHost parent)
        {
            _parent = parent;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Logger.Info("👥 Copiloto conectado");
            _parent.OnClientConnected.Invoke();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Logger.Warn("👋 Copiloto desconectado");
            _parent.OnClientDisconnected.Invoke();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            _parent.OnMessage.Invoke(e.Data);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            base.OnError(e);
            Logger.Warn($"⚠️ Error en servidor WebSocket: {e.Message}");
        }
    }
}
