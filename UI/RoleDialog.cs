using System;
using System.Drawing;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SharedCockpitClient.UI
{
    /// <summary>
    /// Diálogo simple para seleccionar rol y dirección IP remota.
    /// </summary>
    public sealed class RoleDialog : Form
    {
        private readonly RadioButton rbHost;
        private readonly RadioButton rbClient;
        private readonly TextBox txtHostIp;
        private readonly Button btnOk;
        private readonly Button btnCancel;
        private readonly Label lblHint;

        public RoleDialog()
        {
            Text = "Seleccionar rol";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(320, 180);

            rbHost = new RadioButton
            {
                Name = nameof(rbHost),
                Text = "Host",
                AutoSize = true,
                Location = new Point(20, 20),
                Checked = true
            };

            rbClient = new RadioButton
            {
                Name = nameof(rbClient),
                Text = "Cliente",
                AutoSize = true,
                Location = new Point(20, 50)
            };
            rbClient.CheckedChanged += (_, _) => UpdateInputVisibility();

            lblHint = new Label
            {
                AutoSize = true,
                Location = new Point(40, 80),
                Text = "IP/hostname del host:",
                Visible = false
            };

            txtHostIp = new TextBox
            {
                Name = nameof(txtHostIp),
                Location = new Point(40, 105),
                Size = new Size(240, 23),
                Visible = false
            };

            btnOk = new Button
            {
                Name = nameof(btnOk),
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Location = new Point(140, 140),
                AutoSize = true
            };
            btnOk.Click += (_, _) =>
            {
                if (!ValidateAndCommit())
                {
                    DialogResult = DialogResult.None;
                }
            };

            btnCancel = new Button
            {
                Name = nameof(btnCancel),
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new Point(220, 140),
                AutoSize = true
            };

            Controls.Add(rbHost);
            Controls.Add(rbClient);
            Controls.Add(lblHint);
            Controls.Add(txtHostIp);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            if (string.Equals(GlobalFlags.Role, "CLIENT", StringComparison.OrdinalIgnoreCase))
            {
                rbClient.Checked = true;
                txtHostIp.Text = GlobalFlags.PeerAddress;
                PeerIp = string.IsNullOrWhiteSpace(GlobalFlags.PeerAddress) ? null : GlobalFlags.PeerAddress;
            }
            else
            {
                rbHost.Checked = true;
                PeerIp = string.IsNullOrWhiteSpace(GlobalFlags.PeerAddress) ? null : GlobalFlags.PeerAddress;
            }

            UpdateInputVisibility();
        }

        public string SelectedRole { get; private set; } = "HOST";

        public string? PeerIp { get; private set; }
            = null;

        private void UpdateInputVisibility()
        {
            var clientSelected = rbClient.Checked;
            txtHostIp.Visible = clientSelected;
            lblHint.Visible = clientSelected;
        }

        private bool ValidateAndCommit()
        {
            SelectedRole = rbClient.Checked ? "CLIENT" : "HOST";
            if (SelectedRole == "CLIENT")
            {
                var input = txtHostIp.Text?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(input) || !IsValidHost(input))
                {
                    MessageBox.Show(this,
                        "Debes ingresar una IP o hostname válido del host.",
                        "Validación",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    txtHostIp.Focus();
                    return false;
                }

                PeerIp = input;
            }
            else
            {
                PeerIp = null;
            }

            return true;
        }

        private static bool IsValidHost(string value)
        {
            if (IPAddress.TryParse(value, out _))
                return true;

            // Hostname básico (RFC 1123 compliant)
            const string pattern = "^(?=.{1,255}$)([a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)(?:\\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$";
            return Regex.IsMatch(value, pattern, RegexOptions.CultureInvariant);
        }
    }
}
