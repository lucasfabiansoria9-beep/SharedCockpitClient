using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Persistence;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Sincronización en tiempo real entre host y copiloto.
    /// - Escucha snapshots del SimConnectManager.
    /// - Los envía vía WebSocket (host: Broadcast, cliente: Send).
    /// - Recibe mensajes del peer y los aplica (stateChange o snapshot).
    /// - Persiste periódicamente el estado convergido.
    /// </summary>
    public sealed class RealtimeSyncManager : IDisposable
    {
        private readonly AircraftStateManager _state;
        private readonly SimConnectManager _sim;
        private readonly WebSocketManager _ws;
        private readonly SnapshotStore _store;
        private readonly bool _isHost;

        private readonly TimeSpan _persistEvery = TimeSpan.FromSeconds(5);
        private DateTime _lastPersist = DateTime.MinValue;

        private bool _started;
        private long _lastOutboundStamp;
        private readonly object _stampLock = new();

        public RealtimeSyncManager(
            AircraftStateManager state,
            SimConnectManager sim,
            WebSocketManager ws,
            SnapshotStore? store = null)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
            _sim = sim ?? throw new ArgumentNullException(nameof(sim));
            _ws = ws ?? throw new ArgumentNullException(nameof(ws));
            _store = store ?? new SnapshotStore();
            _isHost = string.Equals(GlobalFlags.Role, "HOST", StringComparison.OrdinalIgnoreCase);
        }

        public void Start()
        {
            if (_started) return;
            _started = true;

            _sim.OnSnapshot += HandleLocalSnapshot;
            _ws.OnMessage += HandleWsMessage;
        }

        private async void HandleLocalSnapshot(SimStateSnapshot snapshot, bool isDiff)
        {
            try
            {
                // 1) Enviar a peer
                var flat = snapshot.ToFlatDictionary();
                var envelope = new WireEnvelope
                {
                    type = isDiff ? "stateChange" : "snapshot",
                    payload = flat,
                    serverTime = StampServerTime()
                };
                string json = JsonSerializer.Serialize(envelope);

                if (_isHost)
                    await _ws.BroadcastAsync(json).ConfigureAwait(false);
                else
                    await _ws.SendAsync(json).ConfigureAwait(false);

                // 2) Persistencia periódica
                var now = DateTime.UtcNow;
                if (now - _lastPersist >= _persistEvery)
                {
                    _lastPersist = now;
                    await _store.SaveAsync(flat, CancellationToken.None).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] ⚠️ Error enviando snapshot: {ex.Message}");
            }
        }

        private async void HandleWsMessage(string json)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<WireEnvelope>(json);
                if (msg == null) return;

                // Anti-eco básico: si el serverTime coincide con lo último que emitimos, ignoramos
                if (msg.serverTime.HasValue && msg.serverTime.Value == ReadLastOutboundStamp())
                    return;

                switch (msg.type)
                {
                    case "stateChange":
                        if (msg.path != null)
                        {
                            // Cambio puntual path/value (modo compacto)
                            await _sim.ApplyRemoteChangeAsync(msg.path, msg.value, CancellationToken.None).ConfigureAwait(false);
                        }
                        else if (msg.payload != null)
                        {
                            // Diff de múltiples keys
                            _state.ApplySnapshot(msg.payload);
                        }
                        break;

                    case "snapshot":
                        if (msg.payload != null)
                        {
                            // Snapshot completo
                            _state.ApplySnapshot(msg.payload);
                            await _store.SaveAsync(msg.payload, CancellationToken.None).ConfigureAwait(false);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] ⚠️ Error procesando mensaje WS: {ex.Message}");
            }
        }

        private long StampServerTime()
        {
            var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            lock (_stampLock) _lastOutboundStamp = ts;
            return ts;
        }

        private long ReadLastOutboundStamp()
        {
            lock (_stampLock) return _lastOutboundStamp;
        }

        public void Dispose()
        {
            _sim.OnSnapshot -= HandleLocalSnapshot;
            _ws.OnMessage -= HandleWsMessage;
        }

        // ===== Mensaje de cableado (envelope) =====
        private sealed class WireEnvelope
        {
            public string? type { get; set; }            // "stateChange" | "snapshot"
            // para compact path/value:
            public string? path { get; set; }
            public object? value { get; set; }
            // para diffs o snapshot completo:
            public Dictionary<string, object?>? payload { get; set; }
            // anti-eco y ordenación:
            public long? serverTime { get; set; }
        }
    }
}
