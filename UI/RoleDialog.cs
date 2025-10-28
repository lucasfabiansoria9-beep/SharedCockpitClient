using System;
using System.Drawing;
using System.Net;
using System.Windows.Forms;

namespace SharedCockpitClient.UI
{
    public sealed class RoleDialog : Form
    {
        private readonly RadioButton rbHost;
        private readonly RadioButton rbClient;
        private readonly TextBox txtHostIp;
        private readonly Button btnOk;
        private readonly Button btnCancel;

        public string SelectedRole { get; private set; } = "HOST";
        public string? PeerIp { get; private set; }

        public RoleDialog()
        {
            Text = "Seleccionar rol";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(320, 160);

            rbHost = new RadioButton
            {
                Text = "Host",
                Location = new Point(20, 20),
                AutoSize = true,
                Checked = true
            };

            rbClient = new RadioButton
            {
                Text = "Cliente",
                Location = new Point(20, 50),
                AutoSize = true
            };
            rbClient.CheckedChanged += (_, _) => UpdateClientFields();

            txtHostIp = new TextBox
            {
                Location = new Point(40, 80),
                Width = 240,
                Visible = false
            };

            btnOk = new Button
            {
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Location = new Point(80, 120),
                Width = 100
            };
            btnOk.Click += (_, _) =>
            {
                if (!ValidateAndAssign())
                {
                    DialogResult = DialogResult.None;
                }
            };

            btnCancel = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new Point(190, 120),
                Width = 100
            };

            Controls.AddRange(new Control[] { rbHost, rbClient, txtHostIp, btnOk, btnCancel });

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private void UpdateClientFields()
        {
            txtHostIp.Visible = rbClient.Checked;
            if (rbClient.Checked)
            {
                txtHostIp.Focus();
                txtHostIp.SelectAll();
            }
        }

        private bool ValidateAndAssign()
        {
            if (rbClient.Checked)
            {
                var input = txtHostIp.Text.Trim();
                if (string.IsNullOrWhiteSpace(input))
                {
                    MessageBox.Show(this, "Ingresa la IP o host del compañero.", "SharedCockpit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                if (!IsValidHost(input))
                {
                    MessageBox.Show(this, "Dirección inválida. Usa IP o hostname válido.", "SharedCockpit", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                SelectedRole = "CLIENT";
                PeerIp = input;
            }
            else
            {
                SelectedRole = "HOST";
                PeerIp = null;
            }

            return true;
        }

        private static bool IsValidHost(string value)
        {
            if (IPAddress.TryParse(value, out _))
                return true;

            var hostNameType = Uri.CheckHostName(value);
            return hostNameType == UriHostNameType.Dns || hostNameType == UriHostNameType.IPv4 || hostNameType == UriHostNameType.IPv6;
        }
    }
}
