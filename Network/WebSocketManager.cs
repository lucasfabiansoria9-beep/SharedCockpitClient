using System;
using WebSocketSharp;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.Network;

public sealed class WebSocketManager : IDisposable
{
    private readonly string _url;
    private WebSocket? _client;
    private bool _disposed;
    private readonly object _sync = new();

    public event Action OnOpen = delegate { };
    public event Action<string> OnMessage = delegate { };
    public event Action<string> OnError = delegate { };
    public event Action OnClose = delegate { };

    public bool IsConnected => _client?.IsAlive == true;

    public WebSocketManager(string url)
    {
        _url = url ?? throw new ArgumentNullException(nameof(url));
    }

    public void Connect()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            if (_client != null)
            {
                return;
            }

            _client = new WebSocket(_url);
            _client.OnOpen += (_, _) =>
            {
                Logger.Info($"🌐 Conectado al host {_url}");
                OnOpen.Invoke();
            };
            _client.OnMessage += (_, e) => OnMessage.Invoke(e.Data);
            _client.OnError += (_, e) =>
            {
                Logger.Warn($"⚠️ Error WebSocket: {e.Message}");
                OnError.Invoke(e.Message);
            };
            _client.OnClose += (_, _) =>
            {
                Logger.Warn("🔌 Conexión WebSocket cerrada");
                OnClose.Invoke();
            };

            try
            {
                _client.Connect();
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ No se pudo conectar al host {_url}: {ex.Message}");
                OnError.Invoke(ex.Message);
            }
        }
    }

    public void Send(string message)
    {
        lock (_sync)
        {
            if (_client?.IsAlive == true)
            {
                _client.Send(message);
            }
            else
            {
                Logger.Warn("⚠️ No se puede enviar: WebSocket desconectado");
            }
        }
    }

    public void Dispose()
    {
        lock (_sync)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_client != null)
            {
                try
                {
                    _client.Close();
                }
                catch
                {
                    // Ignored
                }

                _client = null;
            }
        }
    }
}
