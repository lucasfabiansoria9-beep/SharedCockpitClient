#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.Network
{
    public sealed class LanDiscoveryListener : IDisposable
    {
        private readonly UdpClient _client;
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentDictionary<string, LanDiscoveryMessage> _latestByRoom = new();
        private readonly TimeSpan _pruneWindow = TimeSpan.FromSeconds(10);
        private readonly Task _loopTask;
        private readonly object _lock = new();

        public event Action? Updated;

        public LanDiscoveryListener(int port)
        {
            _client = new UdpClient(port);
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _client.EnableBroadcast = true;
            _loopTask = Task.Run(ListenLoopAsync);
        }

        public LanDiscoveryMessage[] Snapshot()
        {
            var now = DateTime.UtcNow;
            foreach (var kvp in _latestByRoom)
            {
                if (now - kvp.Value.ReceivedUtc > _pruneWindow)
                {
                    _latestByRoom.TryRemove(kvp.Key, out _);
                }
            }

            return _latestByRoom.Values
                .OrderByDescending(m => m.ReceivedUtc)
                .ToArray();
        }

        private async Task ListenLoopAsync()
        {
            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    var result = await _client.ReceiveAsync(_cts.Token).ConfigureAwait(false);
                    var message = ParseMessage(result.Buffer, result.RemoteEndPoint);
                    if (message == null)
                        continue;

                    var key = $"{message.RoomName}@{message.Address}:{message.Port}";
                    _latestByRoom[key] = message;
                    Updated?.Invoke();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Discovery] ⚠️ Error en listener: {ex.Message}");
            }
        }

        private static LanDiscoveryMessage? ParseMessage(byte[] buffer, IPEndPoint remote)
        {
            try
            {
                var text = Encoding.UTF8.GetString(buffer).Trim();
                var parts = text.Split('|');
                if (parts.Length != 4)
                    return null;

                if (!string.Equals(parts[0], "SharedCockpit", StringComparison.OrdinalIgnoreCase))
                    return null;

                var room = parts[1];
                var host = string.IsNullOrWhiteSpace(parts[2]) ? remote.Address.ToString() : parts[2];
                if (!int.TryParse(parts[3], out var port))
                    return null;

                return new LanDiscoveryMessage(room, host, port, DateTime.UtcNow);
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _client.Close();
            }
            catch
            {
            }
            _client.Dispose();
            _cts.Dispose();
            try
            {
                _loopTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch
            {
            }
        }
    }
}
