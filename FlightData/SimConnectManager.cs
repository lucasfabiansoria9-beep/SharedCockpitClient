using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Orquesta la interacción con SimConnect o su mock, generando snapshots y aplicando comandos remotos.
    /// </summary>
    public sealed class SimConnectManager : IDisposable
    {
        private readonly AircraftStateManager _aircraftState;
        private readonly object _snapshotLock = new();
        private SimStateSnapshot _lastSnapshot = new();
        private readonly SimDataCollector _collector;
        private readonly SimCommandApplier _commandApplier;
        private readonly SimDataMock _mockData;
        private bool _started;

        public event Action<SimStateSnapshot, bool>? OnSnapshot;

        public SimConnectManager(AircraftStateManager aircraftState)
        {
            _aircraftState = aircraftState ?? throw new ArgumentNullException(nameof(aircraftState));

            if (GlobalFlags.IsLabMode)
            {
                Console.WriteLine("[SimConnect] 🧪 Inicializando en modo laboratorio (mock).");
            }
            else
            {
                Console.WriteLine("[SimConnect] ⚠️ Conexión real aún no disponible en este entorno. Usando mock.");
            }

            _mockData = new SimDataMock();
            _collector = new SimDataCollector(_mockData.SnapshotAsync);
            _collector.OnSnapshot += HandleSnapshot;

            _commandApplier = new SimCommandApplier(
                async (descriptor, value, ct) =>
                {
                    await _mockData.ApplyVarAsync(descriptor, value, ct).ConfigureAwait(false);
                    MirrorState(descriptor.Path, value);
                    return true;
                },
                async (descriptor, value, ct) =>
                {
                    await _mockData.TriggerEventAsync(descriptor, value, ct).ConfigureAwait(false);
                    MirrorState(descriptor.Path, value);
                    return true;
                },
                MirrorState);
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _collector.Start();
        }

        public Task StopAsync() => _collector.StopAsync();

        public Task<bool> ApplyRemoteChangeAsync(string path, object? value, CancellationToken ct)
        {
            return _commandApplier.ApplyAsync(path, value, ct);
        }

        public SimStateSnapshot GetLastSnapshot()
        {
            lock (_snapshotLock)
            {
                return _lastSnapshot.Clone();
            }
        }

        private void HandleSnapshot(SimStateSnapshot snapshot, bool isDiff)
        {
            lock (_snapshotLock)
            {
                if (_lastSnapshot.Values.Count == 0 || !snapshot.IsDiff)
                {
                    _lastSnapshot = snapshot.Clone();
                }
                else
                {
                    _lastSnapshot = _lastSnapshot.MergeDiff(snapshot);
                }
            }

            var flat = snapshot.ToFlatDictionary();
            _aircraftState.ApplySnapshot(flat);
            OnSnapshot?.Invoke(snapshot, isDiff);
        }

        private void MirrorState(string path, object? value)
        {
            lock (_snapshotLock)
            {
                _lastSnapshot.Set(path, value);
            }

            _aircraftState.Set(path, value);
        }

        public void Dispose()
        {
            _collector.Dispose();
        }
    }
}
