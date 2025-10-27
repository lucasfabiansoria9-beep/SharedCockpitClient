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
    /// Controla sincronización: anti-eco, idempotencia, LWW y delega animaciones y estado.
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

        // 🧩 Integración con el estado global del avión
        private readonly AircraftStateManager? aircraftState;

        public Guid LocalInstanceId => localInstanceId;

        /// <summary>
        /// Crea un nuevo controlador de sincronización.
        /// </summary>
        public SyncController(INetworkBus bus, AircraftStateManager? stateManager = null)
        {
            this.bus = bus ?? throw new ArgumentNullException(nameof(bus));
            this.bus.OnMessage += HandleIncomingJson;
            this.aircraftState = stateManager;

            // Si existe un manager de estado, suscribirse a sus cambios locales.
            if (aircraftState != null)
            {
                aircraftState.OnPropertyChanged += async (prop, value) =>
                {
                    try
                    {
                        var jsonVal = JsonSerializer.SerializeToElement(value);
                        var msg = NewMessage(prop, jsonVal);
                        Console.WriteLine($"[Send] {prop} = {value} (seq={msg.sequence}, origin={msg.originId})");
                        await SendAsync(msg).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Sync] Error enviando {prop}: {ex.Message}");
                    }
                };
            }
        }

        public void Dispose()
        {
            bus.OnMessage -= HandleIncomingJson;
            flapsAnimator.Dispose();
        }

        #region ======== Entradas locales ========

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
            aircraftState?.Set("Flaps", stepped);

            var msg = NewMessage("Flaps", JsonSerializer.SerializeToElement(stepped));
            Console.WriteLine($"[Send] Flaps = {stepped} (seq={msg.sequence}, origin={msg.originId})");
            await SendAsync(msg).ConfigureAwait(false);
        }

        public async Task ToggleGearLocalAsync()
        {
            gearDown = !gearDown;
            Console.WriteLine($"[AnimStart] GearDown -> {gearDown}");
            Console.WriteLine("[AnimEnd] GearDown completado");
            aircraftState?.Set("GearDown", gearDown);

            var msg = NewMessage("GearDown", JsonSerializer.SerializeToElement(gearDown));
            Console.WriteLine($"[Send] GearDown = {gearDown} (seq={msg.sequence}, origin={msg.originId})");
            await SendAsync(msg).ConfigureAwait(false);
        }

        #endregion

        #region ======== Mensajes entrantes ========

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

                if (!Guid.TryParse(msg.originId, out var originGuid))
                    return;

                if (originGuid == localInstanceId)
                    return; // Anti-eco

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
            try
            {
                switch (msg.prop)
                {
                    case "Flaps":
                        if (msg.value.ValueKind == JsonValueKind.Number && msg.value.TryGetDouble(out var target))
                        {
                            Console.WriteLine($"[RemoteChange] Flaps = {target} (from {msg.originId}, seq={msg.sequence})");
                            flapsAnimator.AnimateTo(target);
                            aircraftState?.Set("Flaps", target);
                        }
                        break;

                    case "GearDown":
                        if (msg.value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        {
                            var newVal = msg.value.GetBoolean();
                            gearDown = newVal;
                            Console.WriteLine($"[RemoteChange] GearDown = {gearDown} (from {msg.originId}, seq={msg.sequence})");
                            aircraftState?.Set("GearDown", gearDown);
                        }
                        break;

                    default:
                        Console.WriteLine($"[RemoteChange] {msg.prop} = {msg.value} (from {msg.originId}, seq={msg.sequence})");
                        ApplyGenericRemote(msg);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] Error aplicando estado remoto: {ex.Message}");
            }
        }

        // Aplica estados remotos genéricos (luces, motor, etc.)
        private void ApplyGenericRemote(StateChangeMessage msg)
        {
            if (aircraftState == null) return;

            object? val = msg.value.ValueKind switch
            {
                JsonValueKind.Number => msg.value.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => msg.value.GetString(),
                _ => msg.value.ToString()
            };

            aircraftState.Set(msg.prop, val);
        }

        #endregion

        #region ======== Utilitarios ========

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

        #endregion
    }
}
