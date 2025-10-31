using System;
using System.Windows.Forms;

namespace SharedCockpitClient
{
    public partial class RoleDialog : Form
    {
        public RoleDialog()
        {
            InitializeComponent();

            rbHost.CheckedChanged += (_, __) => UpdatePeerVisibility();
            rbClient.CheckedChanged += (_, __) => UpdatePeerVisibility();

            if (string.Equals(GlobalFlags.Role, "client", StringComparison.OrdinalIgnoreCase))
            {
                rbClient.Checked = true;
            }
            else
            {
                rbHost.Checked = true;
            }

            txtPeerAddress.Text = string.IsNullOrWhiteSpace(GlobalFlags.PeerAddress)
                ? string.Empty
                : GlobalFlags.PeerAddress;
            txtRoomName.Text = GlobalFlags.RoomName ?? string.Empty;
            chkPublic.Checked = GlobalFlags.IsPublicRoom;

            UpdatePeerVisibility();
        }

        private void UpdatePeerVisibility()
        {
            var isClient = rbClient.Checked;
            txtPeerAddress.Visible = isClient;
            lblPeer.Visible = isClient;

            if (isClient)
            {
                txtPeerAddress.Focus();
                txtPeerAddress.SelectAll();
            }
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            var role = rbHost.Checked ? "host" : rbClient.Checked ? "client" : null;
            if (string.IsNullOrEmpty(role))
            {
                MessageBox.Show("Seleccione un rol.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            var roomName = txtRoomName.Text.Trim();
            if (string.IsNullOrWhiteSpace(roomName))
            {
                MessageBox.Show("Ingrese un nombre de sala.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            var peer = txtPeerAddress.Text.Trim();
            if (role == "client" && string.IsNullOrWhiteSpace(peer))
            {
                MessageBox.Show("Ingrese dirección del host.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
                return;
            }

            GlobalFlags.Role = role;
            GlobalFlags.RoomName = roomName;
            GlobalFlags.IsPublicRoom = chkPublic.Checked;
            GlobalFlags.PeerAddress = peer;

        }
    }
}
