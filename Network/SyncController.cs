using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Abstracción mínima del bus de red. Adaptado al administrador WebSocket del proyecto.
    /// </summary>
    public interface INetworkBus
    {
        Task SendAsync(string json);
        event Action<string>? OnMessage; // JSON entrante
    }

    /// <summary>
    /// Controla sincronización: anti-eco, idempotencia, LWW y delega animaciones.
    /// </summary>
    public sealed class SyncController : IDisposable
    {
        private readonly INetworkBus bus;
        private readonly Guid localInstanceId = Guid.NewGuid();
        private int localSequence = 0;
        private readonly ConcurrentDictionary<string, int> lastSeqByOrigin = new();

        private readonly FlapAnimator flapsAnimator = new(stepMs: 50, stepValue: 0.5);
        private volatile bool gearDown;

        private readonly object sendLock = new();
        private DateTime lastSendUtc = DateTime.MinValue;
        private double lastSentFlapsTarget = double.NaN;
        private readonly TimeSpan minSendInterval = TimeSpan.FromMilliseconds(50);

        public Guid LocalInstanceId => localInstanceId;

        public SyncController(INetworkBus bus)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.bus.OnMessage += HandleIncomingJson;
        }

        public void Dispose()
        {
            bus.OnMessage -= HandleIncomingJson;
            flapsAnimator.Dispose();
        }

        public async Task SetFlapsLocalAsync(double target)
        {
            var stepped = Math.Round(target * 2.0, MidpointRounding.AwayFromZero) / 2.0;
            lock (sendLock)
            {
                var now = DateTime.UtcNow;
                if (now - lastSendUtc < minSendInterval && Math.Abs(stepped - lastSentFlapsTarget) < 0.001)
                    return;
                lastSendUtc = now;
                lastSentFlapsTarget = stepped;
            }

            flapsAnimator.AnimateTo(stepped);

            var msg = NewMessage("Flaps", JsonSerializer.SerializeToElement(stepped));
            Console.WriteLine($"[Send] Flaps = {stepped} (seq={msg.sequence}, origin={msg.originId})");
            await SendAsync(msg).ConfigureAwait(false);
        }

        public async Task ToggleGearLocalAsync()
        {
            gearDown = !gearDown;
            Console.WriteLine($"[AnimStart] GearDown -> {gearDown}");
            Console.WriteLine("[AnimEnd] GearDown completado");

            var msg = NewMessage("GearDown", JsonSerializer.SerializeToElement(gearDown));
            Console.WriteLine($"[Send] GearDown = {gearDown} (seq={msg.sequence}, origin={msg.originId})");
            await SendAsync(msg).ConfigureAwait(false);
        }

        private void HandleIncomingJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (!root.TryGetProperty("type", out var t) || t.GetString() != "stateChange")
                    return;

                var msg = JsonSerializer.Deserialize<StateChangeMessage>(json);
                if (msg == null)
                    return;

                if (Guid.TryParse(msg.originId, out var originGuid))
                {
                    if (originGuid == localInstanceId)
                        return;
                }
                else
                {
                    return;
                }

                var lastSeen = lastSeqByOrigin.GetOrAdd(msg.originId, -1);
                if (msg.sequence <= lastSeen)
                    return;
                lastSeqByOrigin[msg.originId] = msg.sequence;

                ApplyRemoteState(msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] Error parseando mensaje: {ex.Message}");
            }
        }

        private void ApplyRemoteState(StateChangeMessage msg)
        {
            switch (msg.prop)
            {
                case "Flaps":
                    if (msg.value.ValueKind == JsonValueKind.Number && msg.value.TryGetDouble(out var target))
                    {
                        Console.WriteLine($"[RemoteChange] Flaps = {target} (from {msg.originId}, seq={msg.sequence})");
                        flapsAnimator.AnimateTo(target);
                    }
                    break;
                case "GearDown":
                    if (msg.value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                    {
                        var newVal = msg.value.GetBoolean();
                        gearDown = newVal;
                        Console.WriteLine($"[RemoteChange] GearDown = {gearDown} (from {msg.originId}, seq={msg.sequence})");
                    }
                    break;
                default:
                    Console.WriteLine($"[RemoteChange] {msg.prop} = {msg.value} (from {msg.originId}, seq={msg.sequence})");
                    break;
            }
        }

        private StateChangeMessage NewMessage(string prop, JsonElement value)
        {
            var seq = Interlocked.Increment(ref localSequence);
            return new StateChangeMessage
            {
                prop = prop,
                value = value,
                originId = localInstanceId.ToString(),
                sequence = seq,
                serverTime = 0
            };
        }

        private Task SendAsync(StateChangeMessage msg)
        {
            var json = JsonSerializer.Serialize(msg);
            return bus.SendAsync(json);
        }
    }
}
