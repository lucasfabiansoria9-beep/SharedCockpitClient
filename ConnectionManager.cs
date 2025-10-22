using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    public class ConnectionManager
    {
        private TcpListener server = null!;
        private TcpClient client = null!;
        private NetworkStream stream = null!;
        private CancellationTokenSource cts = null!;

        private readonly int port;
        private readonly bool isHost;
        private readonly string hostIp;

        public event Action<string> OnStatusChanged = delegate { };
        public event Action<string> OnDataReceived = delegate { };

        public bool IsConnected => client != null && client.Connected;

        public ConnectionManager(bool hostMode, int port, string hostIp = "127.0.0.1")
        {
            isHost = hostMode;
            this.port = port;
            this.hostIp = hostIp;
        }

        public async Task StartAsync()
        {
            cts = new CancellationTokenSource();

            if (isHost)
            {
                await StartServerAsync();
            }
            else
            {
                await ConnectToServerAsync();
            }
        }

        private async Task StartServerAsync()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, port);
                server.Start();
                OnStatusChanged.Invoke($"Servidor iniciado en puerto {port}. Esperando conexión...");

                client = await server.AcceptTcpClientAsync();
                stream = client.GetStream();

                OnStatusChanged.Invoke("Cliente conectado ✅");

                _ = Task.Run(() => ReceiveLoop(cts.Token));
            }
            catch (Exception ex)
            {
                OnStatusChanged.Invoke($"Error al iniciar servidor: {ex.Message}");
            }
        }

        private async Task ConnectToServerAsync()
        {
            try
            {
                client = new TcpClient();
                OnStatusChanged.Invoke($"Conectando al host {hostIp}:{port}...");

                await client.ConnectAsync(IPAddress.Parse(hostIp), port);
                stream = client.GetStream();

                OnStatusChanged.Invoke("Conectado al servidor ✅");

                _ = Task.Run(() => ReceiveLoop(cts.Token));
            }
            catch (Exception ex)
            {
                OnStatusChanged.Invoke($"Error al conectar: {ex.Message}");
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            var buffer = new byte[1024];

            try
            {
                while (!token.IsCancellationRequested && stream != null)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                    if (bytesRead == 0)
                    {
                        OnStatusChanged.Invoke("Conexión cerrada ⚠️");
                        Stop();
                        return;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    OnDataReceived.Invoke(message);
                }
            }
            catch (Exception ex)
            {
                OnStatusChanged.Invoke($"Error en recepción: {ex.Message}");
            }
        }

        public async Task SendAsync(string message)
        {
            if (stream == null || !client.Connected) return;

            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                OnStatusChanged.Invoke($"Error al enviar datos: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
                stream?.Close();
                client?.Close();
                server?.Stop();
                OnStatusChanged.Invoke("Conexión detenida.");
            }
            catch (Exception ex)
            {
                OnStatusChanged.Invoke($"Error al detener conexión: {ex.Message}");
            }
        }
    }
}
