#nullable enable
using System;
using System.Linq;
using System.Windows.Forms;
using SharedCockpitClient.Network;
using SharedCockpitClient.Session;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    public partial class RoleDialog : Form
    {
        private const int DiscoveryPort = 9801;
        private LanDiscoveryListener? _listener;
        private readonly System.Windows.Forms.Timer _refreshTimer;
        private LanDiscoveryMessage[] _currentRooms = Array.Empty<LanDiscoveryMessage>();

        public RoleDialog()
        {
            // 🧩 Prevent nullable warnings for WinForms designer
            InitializeComponent();

            rbHost!.CheckedChanged += (_, __) => UpdateRoleUI();
            rbClient!.CheckedChanged += (_, __) => UpdateRoleUI();
            lstRooms!.SelectedIndexChanged += (_, __) => ApplyRoomSelection();
            lstRooms!.DoubleClick += (_, __) => btnContinue?.PerformClick();

            _refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _refreshTimer.Tick += (_, __) => RefreshDiscoveryList();

            Shown += (_, __) =>
            {
                if (rbHost != null)
                {
                    rbHost.Checked = true;
                }
                UpdateRoleUI();
                txtPlayerName?.Focus();
            };

            FormClosed += (_, __) =>
            {
                CleanupListener();
                _refreshTimer.Stop();
                _refreshTimer.Dispose();
            };
        }

        public StartupSessionInfo? StartupInfo { get; private set; }

        private void UpdateRoleUI()
        {
            var isHost = rbHost?.Checked == true;
            if (panelHost != null)
            {
                panelHost.Visible = isHost;
            }

            if (panelClient != null)
            {
                panelClient.Visible = !isHost;
            }

            if (isHost)
            {
                CleanupListener();
                txtRoomName?.Focus();
            }
            else
            {
                EnsureListener();
                txtManualEndpoint?.Focus();
            }
        }

        private void EnsureListener()
        {
            if (_listener != null)
                return;

            try
            {
                _listener = new LanDiscoveryListener(DiscoveryPort);
                _listener.Updated += OnDiscoveryUpdated;
                _refreshTimer.Start();
                RefreshDiscoveryList();
            }
            catch (Exception ex)
            {
                Logger.Error($"[Discovery] ❌ No se pudo iniciar listener: {ex.Message}");
            }
        }

        private void CleanupListener()
        {
            _refreshTimer.Stop();
            if (_listener != null)
            {
                _listener.Updated -= OnDiscoveryUpdated;
                _listener.Dispose();
                _listener = null;
            }
        }

        private void OnDiscoveryUpdated()
        {
            if (IsDisposed)
                return;

            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshDiscoveryList));
                return;
            }

            RefreshDiscoveryList();
        }

        private void RefreshDiscoveryList()
        {
            var listener = _listener;
            if (listener == null)
                return;

            var snapshot = listener.Snapshot();
            if (snapshot.SequenceEqual(_currentRooms))
                return;

            _currentRooms = snapshot;
            if (lstRooms != null)
            {
                lstRooms.BeginUpdate();
                lstRooms.Items.Clear();
                foreach (var room in snapshot)
                {
                    lstRooms.Items.Add(room.Display);
                }
                lstRooms.EndUpdate();
            }
        }

        private void ApplyRoomSelection()
        {
            if (lstRooms == null || lstRooms.SelectedIndex < 0 || lstRooms.SelectedIndex >= _currentRooms.Length)
                return;

            var room = _currentRooms[lstRooms.SelectedIndex];
            if (txtManualEndpoint != null)
            {
                txtManualEndpoint.Text = $"{room.Address}:{room.Port}";
            }
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            var playerName = txtPlayerName?.Text.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(playerName))
            {
                MessageBox.Show(this, "Ingrese el nombre del jugador.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPlayerName?.Focus();
                return;
            }

            if (rbHost?.Checked == true)
            {
                var roomName = txtRoomName?.Text.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(roomName))
                {
                    MessageBox.Show(this, "Ingrese un nombre de sala.", "SharedCockpitClient",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtRoomName?.Focus();
                    return;
                }

                StartupInfo = new StartupSessionInfo(
                    playerName,
                    SessionRole.Host,
                    roomName,
                    chkPublic?.Checked ?? false,
                    hostEndpoint: null);
            }
            else
            {
                var endpoint = txtManualEndpoint?.Text.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(endpoint))
                {
                    MessageBox.Show(this, "Seleccione una sala o ingrese IP:Puerto.", "SharedCockpitClient",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtManualEndpoint?.Focus();
                    return;
                }

                if (!TryParseEndpoint(endpoint, out var normalized))
                {
                    MessageBox.Show(this, "Formato de IP:Puerto inválido.", "SharedCockpitClient",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtManualEndpoint?.Focus();
                    txtManualEndpoint?.SelectAll();
                    return;
                }

                var roomName = ResolveSelectedRoomName(normalized) ?? "Direct";
                StartupInfo = new StartupSessionInfo(
                    playerName,
                    SessionRole.Client,
                    roomName,
                    isPublicBroadcast: false,
                    hostEndpoint: normalized);
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private string? ResolveSelectedRoomName(string endpoint)
        {
            foreach (var room in _currentRooms)
            {
                var displayEndpoint = $"{room.Address}:{room.Port}";
                if (string.Equals(displayEndpoint, endpoint, StringComparison.OrdinalIgnoreCase))
                    return room.RoomName;
            }

            return null;
        }

        private static bool TryParseEndpoint(string input, out string normalized)
        {
            normalized = string.Empty;
            var parts = input.Split(':');
            if (parts.Length != 2)
                return false;

            var host = parts[0].Trim();
            if (string.IsNullOrWhiteSpace(host))
                return false;

            if (!int.TryParse(parts[1].Trim(), out var port) || port <= 0 || port > 65535)
                return false;

            normalized = $"{host}:{port}";
            return true;
        }
    }
}
