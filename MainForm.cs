using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Persistence;
using SharedCockpitClient.Sync;

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
        private RealtimeSyncManager? _realtimeSync;
        private CancellationTokenSource? _wsCts;
        private readonly object _snapshotLock = new();
        private SimStateSnapshot? _latestSnapshot;
        private Panel? _hudPanel;
        private Label? _hudLabel;
        private Timer? _hudTimer;
        private bool _hudVisible;

        public MainForm()
        {
            InitializeComponent();
            KeyPreview = true;
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
                _realtimeSync = new RealtimeSyncManager(_simManager, _wsManager);
                _simManager.OnSnapshot += HandleSimSnapshot;
                _wsManager.OnStateDiff += HandleStateDiff;

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
                _simManager.OnSnapshot -= HandleSimSnapshot;
                _simManager.Dispose();

                if (_wsManager != null)
                {
                    _wsManager.OnStateDiff -= HandleStateDiff;
                    _wsManager.Dispose();
                }

                try { _wsCts?.Cancel(); } catch { }
                _wsCts?.Dispose();

                if (_hudTimer != null)
                {
                    _hudTimer.Stop();
                    _hudTimer.Dispose();
                }

                Console.WriteLine("[MainForm] 📴 Cliente cerrado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MainForm] ⚠️ Error al cerrar: {ex.Message}");
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.F12)
            {
                ToggleHud();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void HandleSimSnapshot(SimStateSnapshot snapshot, bool _)
        {
            lock (_snapshotLock)
            {
                _latestSnapshot = snapshot.Clone();
            }

            _realtimeSync?.UpdateAndSync(snapshot, GlobalFlags.UserRole);
        }

        private void HandleStateDiff(string? role, Dictionary<string, object?> diff)
        {
            _realtimeSync?.ApplyRemoteDiff(role, diff);

            lock (_snapshotLock)
            {
                if (_latestSnapshot == null)
                {
                    _latestSnapshot = new SimStateSnapshot();
                }

                _latestSnapshot.ApplyDiff(diff);
            }
        }

        private void ToggleHud()
        {
            if (!_hudVisible)
            {
                EnsureHud();
                _hudPanel!.Visible = true;
                _hudTimer?.Start();
                _hudVisible = true;
            }
            else
            {
                _hudPanel?.Hide();
                _hudTimer?.Stop();
                _hudVisible = false;
            }
        }

        private void EnsureHud()
        {
            if (_hudPanel != null)
                return;

            _hudPanel = new Panel
            {
                Size = new Size(220, 110),
                BackColor = Color.FromArgb(160, 16, 16, 16),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(ClientSize.Width - 240, 20)
            };

            _hudLabel = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240)
            };

            _hudPanel.Controls.Add(_hudLabel);
            Controls.Add(_hudPanel);
            _hudPanel.BringToFront();

            _hudTimer = new Timer { Interval = 500 };
            _hudTimer.Tick += UpdateHud;
        }

        private void UpdateHud(object? sender, EventArgs e)
        {
            if (!_hudVisible || _hudLabel == null)
                return;

            SimStateSnapshot? snapshotCopy = null;
            lock (_snapshotLock)
            {
                if (_latestSnapshot != null)
                    snapshotCopy = _latestSnapshot.Clone();
            }

            string ias = "--";
            string alt = "--";
            if (snapshotCopy != null)
            {
                if (snapshotCopy.TryGetDouble("AIRSPEED INDICATED", out var iasValue))
                    ias = iasValue.ToString("0");
                if (snapshotCopy.TryGetDouble("PLANE ALTITUDE", out var altValue))
                    alt = altValue.ToString("0");
            }

            var wsState = _wsManager?.IsConnected == true ? "🟢" : "🔴";
            var rate = _realtimeSync?.CurrentDiffRate ?? 0;

            _hudLabel.Text = $"IAS {ias} kt\nALT {alt} ft\nWS {wsState}\nSyncRate {rate:0.0} diff/s";
        }

        // ====== Handlers requeridos por el Designer ======
        private void txtIp_TextChanged(object sender, EventArgs e) { }
        private void btnHost_Click(object sender, EventArgs e) { }
        private void btnClient_Click(object sender, EventArgs e) { }
        private void btnStop_Click(object sender, EventArgs e) { }
    }
}
