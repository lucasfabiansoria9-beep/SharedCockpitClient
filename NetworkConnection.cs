using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    public class NetworkConnection
    {
        private readonly bool isHost;
        private readonly string? remoteIp;
        private TcpListener? listener;
        private TcpClient? client;
        private NetworkStream? stream;
        private CancellationTokenSource? cts;

        public event Action<string>? MessageReceived;

        public NetworkConnection(bool isHost, string? ip = null)
        {
            this.isHost = isHost;
            remoteIp = ip;
        }

        public async Task StartAsync()
        {
            cts = new CancellationTokenSource();

            if (isHost)
            {
                // 🟢 Modo host
                listener = new TcpListener(IPAddress.Any, 9000);
                listener.Start();
                Console.WriteLine("Esperando conexión entrante...");
                client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Cliente conectado.");
            }
            else
            {
                // 🔵 Modo cliente
                if (string.IsNullOrEmpty(remoteIp))
                    throw new InvalidOperationException("No se especificó una dirección IP para conectar.");

                client = new TcpClient();
                await client.ConnectAsync(remoteIp, 9000);
                Console.WriteLine($"Conectado al host {remoteIp}");
            }

            stream = client?.GetStream();

            _ = Task.Run(() => ReceiveLoop(cts.Token));
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            if (stream == null)
                return;

            byte[] buffer = new byte[1024];

            try
            {
                while (!token.IsCancellationRequested)
                {
                    int bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                    if (bytesRead == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    MessageReceived?.Invoke(message);
                    Console.WriteLine($"Mensaje recibido: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error en recepción: {ex.Message}");
            }
        }

        public async Task SendAsync(string message)
        {
            if (stream == null)
                throw new InvalidOperationException("No hay conexión activa.");

            byte[] data = Encoding.UTF8.GetBytes(message);
            await stream.WriteAsync(data, 0, data.Length);
        }

        public void Stop()
        {
            try
            {
                cts?.Cancel();
                stream?.Close();
                client?.Close();
                listener?.Stop();
                Console.WriteLine("Conexión detenida.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al detener conexión: {ex.Message}");
            }
        }
    }
}
