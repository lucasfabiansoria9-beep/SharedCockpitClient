#nullable enable
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.Network
{
    public sealed class LanDiscoveryBroadcaster : IDisposable
    {
        private readonly UdpClient _client;
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _loopTask;
        private readonly string _payload;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);
        private readonly int _port;

        public LanDiscoveryBroadcaster(string roomName, string address, int port, int discoveryPort)
        {
            _client = new UdpClient { EnableBroadcast = true };
            _port = discoveryPort;
            _payload = $"SharedCockpit|{roomName}|{address}|{port}";
            _loopTask = Task.Run(BroadcastLoopAsync);
        }

        private async Task BroadcastLoopAsync()
        {
            var bytes = Encoding.UTF8.GetBytes(_payload);
            var endpoint = new IPEndPoint(IPAddress.Broadcast, _port);

            try
            {
                while (!_cts.IsCancellationRequested)
                {
                    await _client.SendAsync(bytes, bytes.Length, endpoint).ConfigureAwait(false);
                    await Task.Delay(_interval, _cts.Token).ConfigureAwait(false);
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
                Console.WriteLine($"[Discovery] ⚠️ Error emitiendo broadcast: {ex.Message}");
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
