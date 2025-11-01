#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    /// <summary>
    /// Shim mínimo para compilar/ejecutar en laboratorio cuando no hay SimConnect nativo.
    /// Implementa la API mínima usada por la aplicación (eventos, Start, WaitForCockpitReadyAsync, ApplyRemoteChangeAsync, ApplySimVar, Dispose).
    /// </summary>
    public sealed class SimConnectManager : IDisposable
    {
        private readonly AircraftStateManager _stateManager;
        private bool _disposed;
        private volatile bool _isConnected;

        public event Action<SimStateSnapshot, bool>? OnSnapshot;
        public event Action<SimCommandMessage>? OnCommand;

        public Guid LocalInstanceId { get; } = Guid.NewGuid();
        public string LocalInstanceKey { get; } = string.Empty;

        public bool IsConnected => _isConnected;

        public SimConnectManager(AircraftStateManager stateManager)
        {
            _stateManager = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
        }

        public void Start()
        {
            if (_isConnected) return;
            _isConnected = true;

            // Emitir snapshot inicial en background
            Task.Run(() =>
            {
                try
                {
                    var snapshot = _stateManager.ExportFullSnapshot();
                    OnSnapshot?.Invoke(snapshot, false);
                }
                catch
                {
                    // Silencioso: shim resistente a fallos
                }
            });
        }

        public async Task WaitForCockpitReadyAsync(CancellationToken cancellationToken = default)
        {
            if (IsConnected) return;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!IsConnected && sw.Elapsed < TimeSpan.FromSeconds(10))
            {
                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        public Task ApplyRemoteChangeAsync(string path, object? value, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                try { ApplySimVar(path, value); }
                catch { /* no propagar desde shim */ }
            }
            return Task.CompletedTask;
        }

        public void ApplySimVar(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            try { _stateManager.Set(key, value); } catch { /* resistente */ }
        }

        public void RaiseCommand(SimCommandMessage cmd)
        {
            if (cmd == null) return;
            OnCommand?.Invoke(cmd);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // No hay recursos nativos por liberar en el shim.
        }
    }
}