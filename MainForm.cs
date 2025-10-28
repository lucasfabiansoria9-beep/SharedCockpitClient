using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Persistence;
using SharedCockpitClient.Sync;
using SharedCockpitClient.UI;

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
        private System.Windows.Forms.Timer? _hudTimer;   // ✅ corregido
        private bool _hudVisible;
        private bool _hudDiagnosticsExpanded;
        private System.Windows.Forms.Timer? _autosaveTimer;

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
                TryRestoreRoleFromSettings();
                if (!EnsureRoleSelection())
                {
                    Close();
                    return;
                }

                // 1️⃣ Restaurar snapshot previo
                var previous = await _snapshotStore.LoadAsync(default);
                foreach (var kv in previous)
                    _stateManager.Set(kv.Key, kv.Value);

                // 2️⃣ Determinar rol (HOST / CLIENT)
                bool isHost = string.Equals(GlobalFlags.Role, "HOST", StringComparison.OrdinalIgnoreCase);
                Uri? peerUri = string.IsNullOrWhiteSpace(GlobalFlags.PeerAddress)
                    ? null
                    : new Uri($"ws://{GlobalFlags.PeerAddress}:8081");

                // 3️⃣ Iniciar WebSocket
                _wsManager = new WebSocketManager(isHost, peerUri);
                _wsCts = new CancellationTokenSource();
                _ = _wsManager.StartAsync(_wsCts.Token);

                // 4️⃣ Iniciar SimConnect
                _simManager.Start();

                // 5️⃣ Enchufar sincronización en tiempo real
                _realtimeSync = new RealtimeSyncManager(_simManager, _wsManager);
                _simManager.OnSnapshot += HandleSimSnapshot;
                _wsManager.OnStateDiff += HandleStateDiff;

                StartAutosaveTimer();

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

                if (_autosaveTimer != null)
                {
                    _autosaveTimer.Stop();
                    _autosaveTimer.Dispose();
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
            if (keyData == (Keys.Control | Keys.F12))
            {
                ToggleHudDiagnostics();
                return true;
            }

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

            _realtimeSync?.UpdateAndSync(snapshot, GlobalFlags.Role);
        }

        private void HandleStateDiff(string? role, Dictionary<string, object?> diff)
        {
            if (diff == null || diff.Count == 0)
                return;

            var filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in diff)
            {
                if (!SimStateSnapshot.LooksLikeSimVar(kv.Key))
                    continue;
                if (kv.Value is null)
                    continue;
                filtered[kv.Key] = kv.Value;
            }

            if (filtered.Count == 0)
                return;

            _realtimeSync?.ApplyRemoteDiff(role, filtered);

            lock (_snapshotLock)
            {
                if (_latestSnapshot == null)
                {
                    _latestSnapshot = new SimStateSnapshot();
                }

                _latestSnapshot.ApplyDiff(filtered);
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
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.FromArgb(160, 16, 16, 16),
                ForeColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };

            _hudLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(260, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font(FontFamily.GenericMonospace, 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(240, 240, 240)
            };

            _hudPanel.Controls.Add(_hudLabel);
            Controls.Add(_hudPanel);
            _hudPanel.BringToFront();

            _hudTimer = new System.Windows.Forms.Timer { Interval = 500 }; // ✅ corregido
            _hudTimer.Tick += UpdateHud;
            Resize += (_, _) => PositionHud();
            PositionHud();
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

            string ias = "—";
            string alt = "—";
            if (snapshotCopy != null)
            {
                if (snapshotCopy.TryGetDouble("AIRSPEED INDICATED", out var iasValue))
                    ias = iasValue.ToString("0");
                if (snapshotCopy.TryGetDouble("PLANE ALTITUDE", out var altValue))
                    alt = altValue.ToString("0");
            }

            var wsState = _wsManager?.IsConnected == true ? "🟢" : "🔴";
            var simState = _simManager?.IsConnected == true ? "🟢" : "⚪ Offline";
            var rate = _realtimeSync?.CurrentDiffRate ?? 0;
            var builder = new StringBuilder();
            builder.AppendLine($"IAS {ias} kt");
            builder.AppendLine($"ALT {alt} ft");
            builder.AppendLine($"WS {wsState}");
            builder.AppendLine($"SIM {simState}");

            if (_hudDiagnosticsExpanded)
            {
                var roleText = string.IsNullOrWhiteSpace(GlobalFlags.Role) ? "—" : GlobalFlags.Role.ToUpperInvariant();
                var ping = _wsManager?.AverageRttMs ?? -1;
                var fps = _simManager?.LastFps ?? -1;
                builder.AppendLine($"Role {roleText}");
                builder.AppendLine($"Ping {(ping < 0 ? "—" : ping.ToString("0"))} ms");
                builder.AppendLine($"FPS {(fps < 0 ? "—" : fps.ToString("0"))}");
            }

            builder.Append($"SyncRate {rate:0.0} diff/s");

            _hudLabel.Text = builder.ToString();
            PositionHud();
        }

        private void PositionHud()
        {
            if (_hudPanel == null)
                return;

            const int margin = 20;
            var x = Math.Max(margin, ClientSize.Width - _hudPanel.Width - margin);
            var y = margin;
            _hudPanel.Location = new Point(x, y);
        }

        // ====== Handlers requeridos por el Designer ======
        private void txtIp_TextChanged(object sender, EventArgs e) { }
        private void btnHost_Click(object sender, EventArgs e) { }
        private void btnClient_Click(object sender, EventArgs e) { }
        private void btnStop_Click(object sender, EventArgs e) { }

        private bool EnsureRoleSelection()
        {
            var needRole = string.IsNullOrWhiteSpace(GlobalFlags.Role) || Control.ModifierKeys.HasFlag(Keys.F2);
            if (!needRole)
                return true;

            using var dlg = new RoleDialog();
            var result = dlg.ShowDialog(this);
            if (result != DialogResult.OK)
                return false;

            GlobalFlags.Role = dlg.SelectedRole ?? "HOST";
            GlobalFlags.PeerAddress = dlg.PeerIp ?? string.Empty;
            TryPersistRoleSettings(GlobalFlags.Role, GlobalFlags.PeerAddress);
            return true;
        }

        private void ToggleHudDiagnostics()
        {
            if (!_hudVisible)
            {
                ToggleHud();
            }

            _hudDiagnosticsExpanded = !_hudDiagnosticsExpanded;
            UpdateHud(this, EventArgs.Empty);
        }

        private void StartAutosaveTimer()
        {
            _autosaveTimer = new System.Windows.Forms.Timer { Interval = 5000 };
            _autosaveTimer.Tick += AutosaveTimerTick;
            _autosaveTimer.Start();
        }

        private async void AutosaveTimerTick(object? sender, EventArgs e)
        {
            SimStateSnapshot? snapshotCopy = null;
            lock (_snapshotLock)
            {
                if (_latestSnapshot != null)
                    snapshotCopy = _latestSnapshot.Clone();
            }

            if (snapshotCopy == null)
                return;

            try
            {
                await _snapshotStore.SaveIfChangedAsync(snapshotCopy, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Autosave] ⚠️ Error guardando snapshot: {ex.Message}");
            }
        }

        private static void TryRestoreRoleFromSettings()
        {
            try
            {
                var settingsType = Type.GetType("SharedCockpitClient.Properties.Settings, SharedCockpitClient");
                if (settingsType == null)
                    return;

                var defaultProperty = settingsType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                if (defaultProperty?.GetValue(null) is not object settings)
                    return;

                var roleProp = settingsType.GetProperty("Role");
                var peerProp = settingsType.GetProperty("PeerAddress");

                if (roleProp?.GetValue(settings) is string role && !string.IsNullOrWhiteSpace(role))
                    GlobalFlags.Role = role;

                if (peerProp?.GetValue(settings) is string peer && !string.IsNullOrWhiteSpace(peer))
                    GlobalFlags.PeerAddress = peer;
            }
            catch
            {
                // Ignorar si no existe esquema de settings.
            }
        }

        private static void TryPersistRoleSettings(string role, string peer)
        {
            try
            {
                var settingsType = Type.GetType("SharedCockpitClient.Properties.Settings, SharedCockpitClient");
                if (settingsType == null)
                    return;

                var defaultProperty = settingsType.GetProperty("Default", BindingFlags.Public | BindingFlags.Static);
                if (defaultProperty?.GetValue(null) is not object settings)
                    return;

                var roleProp = settingsType.GetProperty("Role");
                var peerProp = settingsType.GetProperty("PeerAddress");

                roleProp?.SetValue(settings, role);
                peerProp?.SetValue(settings, peer);

                var saveMethod = settingsType.GetMethod("Save", BindingFlags.Instance | BindingFlags.Public);
                saveMethod?.Invoke(settings, null);
            }
            catch
            {
                // Ignorar si no hay settings persistentes.
            }
        }
    }
}
