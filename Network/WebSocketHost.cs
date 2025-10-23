using System;
using SharedCockpitClient.Utils;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace SharedCockpitClient.Network;

/// <summary>
/// Servidor WebSocket sencillo que acepta conexiones entrantes del copiloto.
/// Permite enviar mensajes a todos los clientes conectados y expone eventos
/// para reaccionar a la conexión, desconexión y mensajes recibidos.
/// </summary>
public sealed class WebSocketHost : IDisposable
{
    private readonly WebSocketServer server;

    public event Action OnClientConnected = delegate { };
    public event Action OnClientDisconnected = delegate { };
    public event Action<string> OnMessage = delegate { };

    public WebSocketHost(int port)
    {
        server = new WebSocketServer(System.Net.IPAddress.Any, port);
        server.AddWebSocketService("/", () => new HostBehavior(this));
    }

    public void Start()
    {
        if (!server.IsListening)
        {
            server.Start();
            Logger.Info($"🛰️ Servidor WebSocket activo en ws://0.0.0.0:{server.Port}");
        }
    }

    public void Stop()
    {
        if (server.IsListening)
        {
            server.Stop();
            Logger.Info("🛑 Servidor WebSocket detenido.");
        }
    }

    public bool HasClients
    {
        get
        {
            if (!server.IsListening) return false;
            var host = server.WebSocketServices["/"];
            return host?.Sessions?.Count > 0;
        }
    }

    public void Broadcast(string message)
    {
        if (!server.IsListening) return;

        var host = server.WebSocketServices["/"];
        host?.Sessions?.Broadcast(message);
    }

    public void Dispose()
    {
        Stop();
    }

    private sealed class HostBehavior : WebSocketBehavior
    {
        private readonly WebSocketHost parent;

        public HostBehavior(WebSocketHost parent)
        {
            this.parent = parent;
        }

        protected override void OnOpen()
        {
            base.OnOpen();
            Logger.Info("👥 Copiloto conectado al servidor WebSocket.");
            parent.OnClientConnected.Invoke();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            base.OnClose(e);
            Logger.Info("👋 Copiloto desconectado del servidor WebSocket.");
            parent.OnClientDisconnected.Invoke();
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            parent.OnMessage.Invoke(e.Data);
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
            Logger.Warn($"⚠️ Error en servidor WebSocket: {e.Message}");
        }
    }
}
