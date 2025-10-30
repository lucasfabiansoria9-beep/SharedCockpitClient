using System;
using System.Windows.Forms;

namespace SharedCockpitClient
{
    public partial class RoleDialog : Form
    {
        public string SelectedRole => rbHost.Checked ? "HOST" : "CLIENT";
        public string? PeerIp => txtPeerIp.Visible ? txtPeerIp.Text.Trim() : null;

        public RoleDialog()
        {
            InitializeComponent();
            rbHost.CheckedChanged += (_, __) => txtPeerIp.Visible = !rbHost.Checked;
            txtPeerIp.Text = GlobalFlags.PeerAddress ?? string.Empty;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            if (rbClient.Checked && string.IsNullOrWhiteSpace(txtPeerIp.Text))
            {
                MessageBox.Show("Ingrese la IP o nombre del host.", "SharedCockpitClient",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        }
    }
}
