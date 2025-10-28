using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Persistence;

namespace SharedCockpitClient
{
    /// <summary>
    /// Ventana principal de SharedCockpitClient.
    /// Orquesta SimConnect, WebSocket, sincronización en tiempo real y persistencia.
    /// </summary>
    public partial class MainForm : Form
    {
        private readonly AircraftStateManager _stateManager = new();
        private readonly SimConnectManager _simManager;
        private readonly SnapshotStore _snapshotStore = new();
        private WebSocketManager? _wsManager;
        private RealtimeSyncManager? _syncManager;
        private CancellationTokenSource? _wsCts;

        public MainForm()
        {
            InitializeComponent();
            Load += OnLoad;
            FormClosing += OnFormClosing;

            Console.WriteLine("[MainForm] 🛫 Inicializando cliente compartido...");
            _simManager = new SimConnectManager(_stateManager);
        }

        private async void OnLoad(object? sender, EventArgs e)
        {
            try
            {
                // 1️⃣ Restaurar snapshot previo
                var previous = await _snapshotStore.LoadAsync(default);
                foreach (var kv in previous)
                    _stateManager.Set(kv.Key, kv.Value);

                // 2️⃣ Determinar rol (HOST / CLIENT)
                bool isHost = string.Equals(GlobalFlags.Role, "HOST", StringComparison.OrdinalIgnoreCase);
                Uri? peerUri = null;

                if (!isHost)
                {
                    var peer = GlobalFlags.PeerAddress;
                    if (!string.IsNullOrWhiteSpace(peer))
                        peerUri = new Uri($"ws://{peer}:8081");
                }

                // 3️⃣ Iniciar WebSocket
                _wsManager = new WebSocketManager(isHost, peerUri);
                _wsCts = new CancellationTokenSource();
                if (!isHost)
                    _ = _wsManager.StartAsync(_wsCts.Token); // cliente

                // 4️⃣ Iniciar SimConnect
                _simManager.Start();

                // 5️⃣ Enchufar sincronización en tiempo real
                _syncManager = new RealtimeSyncManager(_stateManager, _simManager, _wsManager, _snapshotStore);
                _syncManager.Start();

                Console.WriteLine("[MainForm] ✅ Cliente iniciado correctamente (sincronización RT activa).");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] ⚠️ Error al iniciar: {ex.Message}");
            }
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            try
            {
                _syncManager?.Dispose();
                _simManager.Dispose();

                if (_wsManager != null)
                {
                    _wsManager.Dispose();
                }

                try { _wsCts?.Cancel(); } catch { }
                _wsCts?.Dispose();

                Console.WriteLine("[MainForm] 📴 Cliente cerrado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] ⚠️ Error al cerrar: {ex.Message}");
            }
        }

        // ====== Handlers requeridos por el Designer ======
        private void txtIp_TextChanged(object sender, EventArgs e) { }
        private void btnHost_Click(object sender, EventArgs e) { }
        private void btnClient_Click(object sender, EventArgs e) { }
        private void btnStop_Click(object sender, EventArgs e) { }
    }
}
