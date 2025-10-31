#nullable enable
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharedCockpitClient.Network;
using SharedCockpitClient.Session;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    public partial class MainForm : Form
    {
        private const int DefaultPort = 8081;
        private readonly StartupSessionInfo _sessionInfo;
        private readonly AircraftStateManager _stateManager = new();
        private readonly SnapshotStore _snapshotStore = new();
        private SimConnectManager? _simManager;
        private WebSocketManager? _wsManager;
        private RealtimeSyncManager? _realtimeSync;
        private CancellationTokenSource? _networkCts;
        private LanDiscoveryBroadcaster? _broadcaster;
        private readonly object _logLock = new();
        private int _lastDiffCount;
        private DateTime? _lastSentUtc;
        private int _lastSentBytes;
        private DateTime? _lastReceivedUtc;
        private int _lastReceivedBytes;
        private readonly System.Windows.Forms.Timer _latencyTimer;
        private string _currentNetworkStatus = string.Empty;

        public MainForm(StartupSessionInfo sessionInfo)
        {
            _sessionInfo = sessionInfo ?? throw new ArgumentNullException(nameof(sessionInfo));

            // 🧩 Prevent nullable warnings for WinForms designer
            InitializeComponent();

            Load += MainForm_Load;
            FormClosing += MainForm_FormClosing;

            _latencyTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _latencyTimer.Tick += (_, __) =>
            {
                var latency = _wsManager?.AverageRttMs ?? 0;
                UpdateLatency(latency);

                var status = "🔴 Desconectado";
                if (_wsManager != null)
                {
                    if (_wsManager.IsConnected)
                        status = "🟢 Conectado";
                    else if (_sessionInfo.Role == SessionRole.Host)
                        status = "🟡 Esperando conexión";
                    else
                        status = "🔴 Desconectado";
                }

                UpdateNetworkStatus(status, status);
            };
        }

        private async void MainForm_Load(object? sender, EventArgs e)
        {
            lblRoleValue!.Text = _sessionInfo.Role == SessionRole.Host ? "HOST" : "CLIENT";
            lblRoomValue!.Text = _sessionInfo.RoomName;
            UpdateNetworkStatus("🟡 Iniciando", "🟡 Iniciando");
            UpdateLatency(0);
            UpdateDiffCount(0);
            UpdateLastSent(null, 0);
            UpdateLastReceived(null, 0);
            DisableSessionButtons();

            AppendLog("────────────────────────────────");
            AppendLog("✈️ SharedCockpitClient iniciado");
            AppendLog($"[Boot] Rol seleccionado: {lblRoleValue!.Text} | Sala: {_sessionInfo.RoomName}");
            AppendLog("────────────────────────────────");

            await InitializeSimConnectAsync().ConfigureAwait(false);
        }

        private async Task InitializeSimConnectAsync()
        {
            try
            {
                _simManager = new SimConnectManager(_stateManager);
                _simManager.OnSnapshot += HandleSimSnapshot;
                _simManager.OnCommand += HandleSimCommand;
                _simManager.Start();
            }
            catch (Exception ex)
            {
                AppendLog($"[SimConnect] ❌ Error inicializando: {ex.Message}");
                MessageBox.Show(this,
                    "❌ MSFS no detectado. Abra el simulador y reinicie SharedCockpitClient.",
                    "SharedCockpitClient",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                UpdateNetworkStatus("🔴 SimConnect no disponible", "🔴 Desconectado");
                DisableSessionButtons();
                return;
            }

            if (_simManager == null)
                return;

            if (!_simManager.IsConnected)
            {
                AppendLog("[SimConnect] ⏳ Esperando conexión con MSFS...");
                await _simManager.WaitForCockpitReadyAsync().ConfigureAwait(false);
            }

            if (!_simManager.IsConnected)
            {
                AppendLog("[SimConnect] ❌ MSFS no detectado. Abra el simulador y reinicie SharedCockpitClient.");
                MessageBox.Show(this,
                    "❌ MSFS no detectado. Abra el simulador y reinicie SharedCockpitClient.",
                    "SharedCockpitClient",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                UpdateNetworkStatus("🔴 SimConnect no disponible", "🔴 Desconectado");
                DisableSessionButtons();
                return;
            }

            AppendLog("[SimConnect] ✅ Conexión establecida con MSFS2024");
            EnableButtonsForRole();

            if (_sessionInfo.Role == SessionRole.Host)
                await StartHostAsync().ConfigureAwait(false);
            else
                await StartClientAsync().ConfigureAwait(false);
        }

        private async Task StartHostAsync()
        {
            DisableButtonsDuringAction();
            AppendLog("[Network] 🟡 Iniciando modo HOST...");

            _networkCts?.Cancel();
            _networkCts = new CancellationTokenSource();
            _wsManager = new WebSocketManager(true, portOverride: DefaultPort);
            _wsManager.OnMessage += msg => AppendLog($"[WebSocket] {msg}");

            try
            {
                await _wsManager.StartAsync(_networkCts.Token).ConfigureAwait(false);
                AppendLog($"[Network] 🟡 Esperando copiloto en ws://0.0.0.0:{DefaultPort}");
                UpdateNetworkStatus("🟡 Esperando conexión", "🟡 Esperando conexión");
            }
            catch (Exception ex)
            {
                AppendLog($"[Network] ❌ Error iniciando host: {ex.Message}");
                UpdateNetworkStatus("🔴 Error", "🔴 Desconectado");
                StopNetwork();
                EnableButtonsForRole();
                return;
            }

            if (_sessionInfo.IsPublicBroadcast)
            {
                var hostIp = ResolveLocalAddress();
                _broadcaster = new LanDiscoveryBroadcaster(_sessionInfo.RoomName, hostIp, DefaultPort, 9801);
            }

            await InitializeRealtimeSyncAsync().ConfigureAwait(false);
            EnableButtonsForRole();
        }

        private async Task StartClientAsync()
        {
            if (string.IsNullOrWhiteSpace(_sessionInfo.HostEndpoint))
            {
                AppendLog("[Network] ❌ No se especificó host para el modo cliente.");
                UpdateNetworkStatus("🔴 Sin host", "🔴 Desconectado");
                return;
            }

            DisableButtonsDuringAction();
            AppendLog($"[Network] 🔗 Conectando a {_sessionInfo.HostEndpoint}...");

            _networkCts?.Cancel();
            _networkCts = new CancellationTokenSource();
            var uri = new Uri($"ws://{_sessionInfo.HostEndpoint}");
            _wsManager = new WebSocketManager(false, uri);
            _wsManager.OnMessage += msg => AppendLog($"[WebSocket] {msg}");

            try
            {
                await _wsManager.StartAsync(_networkCts.Token).ConfigureAwait(false);
                UpdateNetworkStatus("🟢 Conectado", "🟢 Conectado");
                AppendLog("[Network] 🟢 Pareja conectada");
            }
            catch (Exception ex)
            {
                AppendLog($"[Network] ❌ No se pudo conectar: {ex.Message}");
                UpdateNetworkStatus("🔴 Desconectado", "🔴 Desconectado");
                StopNetwork();
                EnableButtonsForRole();
                return;
            }

            await InitializeRealtimeSyncAsync().ConfigureAwait(false);
            EnableButtonsForRole();
        }

        private async Task InitializeRealtimeSyncAsync()
        {
            if (_simManager == null || _wsManager == null)
                return;

            _realtimeSync?.Dispose();
            _realtimeSync = new RealtimeSyncManager(_simManager, _wsManager);
            _wsManager.OnStateDiff -= HandleRemoteDiff;
            _wsManager.OnStateDiff += HandleRemoteDiff;

            AppendLog("[RealtimeSync] 🟢 Sincronización activa");
            AppendLog("[RealtimeSync] Bidireccional habilitada");
            _latencyTimer.Start();

            var previous = await _snapshotStore.LoadAsync(CancellationToken.None).ConfigureAwait(false);
            foreach (var entry in previous)
            {
                _stateManager.Set(entry.Key, entry.Value);
            }
        }

        private void HandleSimSnapshot(SimStateSnapshot snapshot, bool isDiff)
        {
            if (snapshot == null || _realtimeSync == null)
                return;

            var payloadBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(snapshot.ToFlatDictionary());
            UpdateLastSent(DateTime.UtcNow, payloadBytes.Length);
            _lastDiffCount = snapshot.Values.Count;
            UpdateDiffCount(_lastDiffCount);

            _realtimeSync.UpdateAndSync(snapshot, lblRoleValue?.Text ?? string.Empty);
        }

        private void HandleSimCommand(SimCommandMessage message)
        {
            AppendLog($"[SimConnect] Cmd -> {message.Command} = {message.Value}");
        }

        private void HandleRemoteDiff(string? role, string? originId, long sequence, Dictionary<string, object?> diff)
        {
            if (_simManager == null)
                return;

            _lastDiffCount = diff?.Count ?? 0;
            UpdateDiffCount(_lastDiffCount);

            if (diff == null || diff.Count == 0)
                return;

            var payloadBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(diff);
            UpdateLastReceived(DateTime.UtcNow, payloadBytes.Length);

            _ = Task.Run(async () =>
            {
                foreach (var kvp in diff)
                {
                    try
                    {
                        await _simManager.ApplyRemoteChangeAsync(kvp.Key, kvp.Value, CancellationToken.None)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        AppendLog($"[RealtimeSync] ⚠️ Error aplicando {kvp.Key}: {ex.Message}");
                    }
                }
            });
        }

        private void btnStartHost_Click(object? sender, EventArgs e)
        {
            if (_sessionInfo.Role != SessionRole.Host)
            {
                MessageBox.Show(this, "La sesión fue configurada como cliente.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _ = StartHostAsync();
        }

        private void btnConnectClient_Click(object? sender, EventArgs e)
        {
            if (_sessionInfo.Role != SessionRole.Client)
            {
                MessageBox.Show(this, "La sesión fue configurada como host.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _ = StartClientAsync();
        }

        private void btnStop_Click(object? sender, EventArgs e)
        {
            AppendLog("[Network] 🛑 Sesión detenida por el usuario");
            StopNetwork();
            UpdateNetworkStatus("🔴 Desconectado", "🔴 Desconectado");
        }

        private void StopNetwork()
        {
            _latencyTimer.Stop();
            _networkCts?.Cancel();
            _networkCts?.Dispose();
            _networkCts = null;

            if (_wsManager != null)
            {
                _wsManager.OnStateDiff -= HandleRemoteDiff;
                _wsManager.Dispose();
                _wsManager = null;
            }

            _realtimeSync?.Dispose();
            _realtimeSync = null;

            _broadcaster?.Dispose();
            _broadcaster = null;

            UpdateLatency(0);
        }

        private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            StopNetwork();

            if (_simManager != null)
            {
                _simManager.OnSnapshot -= HandleSimSnapshot;
                _simManager.OnCommand -= HandleSimCommand;
                _simManager.Dispose();
                _simManager = null;
            }

            _latencyTimer.Stop();
            _latencyTimer.Dispose();
        }

        private void AppendLog(string line)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), line);
                return;
            }

            lock (_logLock)
            {
                if (txtLog != null)
                {
                    if (txtLog.TextLength > 0)
                        txtLog.AppendText(Environment.NewLine);
                    txtLog.AppendText(line);
                }
            }
            Logger.Info(line);
        }

        private void UpdateNetworkStatus(string text, string consoleText)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, string>(UpdateNetworkStatus), text, consoleText);
                return;
            }

            if (string.Equals(_currentNetworkStatus, text, StringComparison.Ordinal))
                return;

            _currentNetworkStatus = text;
            lblNetworkValue!.Text = text;
            Logger.Info($"[Network] {consoleText}");
        }

        private void UpdateLatency(double latencyMs)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<double>(UpdateLatency), latencyMs);
                return;
            }

            lblLatencyValue!.Text = latencyMs <= 0 ? "-" : $"{latencyMs:F0} ms";
        }

        private void UpdateDiffCount(int count)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int>(UpdateDiffCount), count);
                return;
            }

            lblDiffValue!.Text = count > 0 ? count.ToString() : "-";
        }

        private void UpdateLastSent(DateTime? timestampUtc, int bytes)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<DateTime?, int>(UpdateLastSent), timestampUtc, bytes);
                return;
            }

            _lastSentUtc = timestampUtc;
            _lastSentBytes = bytes;
            lblLastSentValue!.Text = timestampUtc == null
                ? "-"
                : $"{timestampUtc:HH:mm:ss} UTC · {bytes} bytes";
        }

        private void UpdateLastReceived(DateTime? timestampUtc, int bytes)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<DateTime?, int>(UpdateLastReceived), timestampUtc, bytes);
                return;
            }

            _lastReceivedUtc = timestampUtc;
            _lastReceivedBytes = bytes;
            lblLastReceivedValue!.Text = timestampUtc == null
                ? "-"
                : $"{timestampUtc:HH:mm:ss} UTC · {bytes} bytes";
        }

        private void DisableButtonsDuringAction()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(DisableButtonsDuringAction));
                return;
            }

            btnStartHost!.Enabled = false;
            btnConnectClient!.Enabled = false;
            btnStop!.Enabled = false;
        }

        private void EnableButtonsForRole()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(EnableButtonsForRole));
                return;
            }

            btnStartHost!.Enabled = _sessionInfo.Role == SessionRole.Host;
            btnConnectClient!.Enabled = _sessionInfo.Role == SessionRole.Client;
            btnStop!.Enabled = true;
        }

        private void DisableSessionButtons()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(DisableSessionButtons));
                return;
            }

            btnStartHost!.Enabled = false;
            btnConnectClient!.Enabled = false;
            btnStop!.Enabled = false;
        }

        private static string ResolveLocalAddress()
        {
            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (iface.OperationalStatus != OperationalStatus.Up)
                    continue;

                var props = iface.GetIPProperties();
                foreach (var addr in props.UnicastAddresses)
                {
                    if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return addr.Address.ToString();
                    }
                }
            }

            return "127.0.0.1";
        }
    }
}
