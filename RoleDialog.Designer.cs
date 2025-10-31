#nullable enable
namespace SharedCockpitClient
{
    partial class RoleDialog
    {
        private System.ComponentModel.IContainer? components = null;

        private System.Windows.Forms.TextBox? txtPlayerName;
        private System.Windows.Forms.RadioButton? rbHost;
        private System.Windows.Forms.RadioButton? rbClient;
        private System.Windows.Forms.TextBox? txtRoomName;
        private System.Windows.Forms.CheckBox? chkPublic;
        private System.Windows.Forms.ListBox? lstRooms;
        private System.Windows.Forms.TextBox? txtManualEndpoint;
        private System.Windows.Forms.Label? lblManualEndpoint;
        private System.Windows.Forms.Label? lblDiscovered;
        private System.Windows.Forms.Button? btnContinue;
        private System.Windows.Forms.Button? btnCancel;
        private System.Windows.Forms.Label? lblPlayer;
        private System.Windows.Forms.Label? lblRoom;
        private System.Windows.Forms.Panel? panelClient;
        private System.Windows.Forms.Panel? panelHost;
        private System.Windows.Forms.Label? lblRole;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtPlayerName = new System.Windows.Forms.TextBox();
            this.rbHost = new System.Windows.Forms.RadioButton();
            this.rbClient = new System.Windows.Forms.RadioButton();
            this.txtRoomName = new System.Windows.Forms.TextBox();
            this.chkPublic = new System.Windows.Forms.CheckBox();
            this.lstRooms = new System.Windows.Forms.ListBox();
            this.txtManualEndpoint = new System.Windows.Forms.TextBox();
            this.lblManualEndpoint = new System.Windows.Forms.Label();
            this.lblDiscovered = new System.Windows.Forms.Label();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblPlayer = new System.Windows.Forms.Label();
            this.lblRoom = new System.Windows.Forms.Label();
            this.panelClient = new System.Windows.Forms.Panel();
            this.panelHost = new System.Windows.Forms.Panel();
            this.lblRole = new System.Windows.Forms.Label();
            this.panelClient.SuspendLayout();
            this.panelHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtPlayerName
            // 
            this.txtPlayerName.Location = new System.Drawing.Point(24, 46);
            this.txtPlayerName.Name = "txtPlayerName";
            this.txtPlayerName.Size = new System.Drawing.Size(320, 23);
            this.txtPlayerName.TabIndex = 0;
            // 
            // rbHost
            // 
            this.rbHost.AutoSize = true;
            this.rbHost.Location = new System.Drawing.Point(16, 10);
            this.rbHost.Name = "rbHost";
            this.rbHost.Size = new System.Drawing.Size(51, 19);
            this.rbHost.TabIndex = 0;
            this.rbHost.TabStop = true;
            this.rbHost.Text = "Host";
            this.rbHost.UseVisualStyleBackColor = true;
            // 
            // rbClient
            // 
            this.rbClient.AutoSize = true;
            this.rbClient.Location = new System.Drawing.Point(88, 10);
            this.rbClient.Name = "rbClient";
            this.rbClient.Size = new System.Drawing.Size(63, 19);
            this.rbClient.TabIndex = 1;
            this.rbClient.TabStop = true;
            this.rbClient.Text = "Cliente";
            this.rbClient.UseVisualStyleBackColor = true;
            // 
            // txtRoomName
            // 
            this.txtRoomName.Location = new System.Drawing.Point(16, 38);
            this.txtRoomName.Name = "txtRoomName";
            this.txtRoomName.Size = new System.Drawing.Size(320, 23);
            this.txtRoomName.TabIndex = 0;
            // 
            // chkPublic
            // 
            this.chkPublic.AutoSize = true;
            this.chkPublic.Location = new System.Drawing.Point(16, 70);
            this.chkPublic.Name = "chkPublic";
            this.chkPublic.Size = new System.Drawing.Size(165, 19);
            this.chkPublic.TabIndex = 1;
            this.chkPublic.Text = "Anunciar sala en la red LAN";
            this.chkPublic.UseVisualStyleBackColor = true;
            // 
            // lstRooms
            // 
            this.lstRooms.FormattingEnabled = true;
            this.lstRooms.ItemHeight = 15;
            this.lstRooms.Location = new System.Drawing.Point(16, 32);
            this.lstRooms.Name = "lstRooms";
            this.lstRooms.Size = new System.Drawing.Size(320, 94);
            this.lstRooms.TabIndex = 0;
            // 
            // txtManualEndpoint
            // 
            this.txtManualEndpoint.Location = new System.Drawing.Point(16, 152);
            this.txtManualEndpoint.Name = "txtManualEndpoint";
            this.txtManualEndpoint.PlaceholderText = "IP:Puerto";
            this.txtManualEndpoint.Size = new System.Drawing.Size(320, 23);
            this.txtManualEndpoint.TabIndex = 2;
            // 
            // lblManualEndpoint
            // 
            this.lblManualEndpoint.AutoSize = true;
            this.lblManualEndpoint.Location = new System.Drawing.Point(16, 134);
            this.lblManualEndpoint.Name = "lblManualEndpoint";
            this.lblManualEndpoint.Size = new System.Drawing.Size(150, 15);
            this.lblManualEndpoint.TabIndex = 3;
            this.lblManualEndpoint.Text = "Conectar manualmente (IP:Port)";
            // 
            // lblDiscovered
            // 
            this.lblDiscovered.AutoSize = true;
            this.lblDiscovered.Location = new System.Drawing.Point(16, 12);
            this.lblDiscovered.Name = "lblDiscovered";
            this.lblDiscovered.Size = new System.Drawing.Size(168, 15);
            this.lblDiscovered.TabIndex = 4;
            this.lblDiscovered.Text = "Salas disponibles en la red LAN";
            // 
            // btnContinue
            // 
            this.btnContinue.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnContinue.Location = new System.Drawing.Point(196, 344);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(88, 28);
            this.btnContinue.TabIndex = 4;
            this.btnContinue.Text = "Continuar";
            this.btnContinue.UseVisualStyleBackColor = true;
            this.btnContinue.Click += new System.EventHandler(this.btnContinue_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(290, 344);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(88, 28);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // lblPlayer
            // 
            this.lblPlayer.AutoSize = true;
            this.lblPlayer.Location = new System.Drawing.Point(24, 28);
            this.lblPlayer.Name = "lblPlayer";
            this.lblPlayer.Size = new System.Drawing.Size(114, 15);
            this.lblPlayer.TabIndex = 6;
            this.lblPlayer.Text = "Nombre del jugador";
            // 
            // lblRoom
            // 
            this.lblRoom.AutoSize = true;
            this.lblRoom.Location = new System.Drawing.Point(16, 20);
            this.lblRoom.Name = "lblRoom";
            this.lblRoom.Size = new System.Drawing.Size(119, 15);
            this.lblRoom.TabIndex = 7;
            this.lblRoom.Text = "Nombre de la sala";
            // 
            // panelClient
            // 
            this.panelClient.Controls.Add(this.lblDiscovered);
            this.panelClient.Controls.Add(this.lstRooms);
            this.panelClient.Controls.Add(this.lblManualEndpoint);
            this.panelClient.Controls.Add(this.txtManualEndpoint);
            this.panelClient.Location = new System.Drawing.Point(24, 200);
            this.panelClient.Name = "panelClient";
            this.panelClient.Size = new System.Drawing.Size(360, 180);
            this.panelClient.TabIndex = 3;
            // 
            // panelHost
            // 
            this.panelHost.Controls.Add(this.lblRoom);
            this.panelHost.Controls.Add(this.txtRoomName);
            this.panelHost.Controls.Add(this.chkPublic);
            this.panelHost.Location = new System.Drawing.Point(24, 184);
            this.panelHost.Name = "panelHost";
            this.panelHost.Size = new System.Drawing.Size(360, 110);
            this.panelHost.TabIndex = 2;
            // 
            // lblRole
            // 
            this.lblRole.AutoSize = true;
            this.lblRole.Location = new System.Drawing.Point(24, 88);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(80, 15);
            this.lblRole.TabIndex = 7;
            this.lblRole.Text = "Selecciona rol";
            // 
            // RoleDialog
            // 
            this.AcceptButton = this.btnContinue;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(400, 388);
            this.Controls.Add(this.lblRole);
            this.Controls.Add(this.panelHost);
            this.Controls.Add(this.panelClient);
            this.Controls.Add(this.lblPlayer);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnContinue);
            this.Controls.Add(this.rbClient);
            this.Controls.Add(this.rbHost);
            this.Controls.Add(this.txtPlayerName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RoleDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuración de sesión";
            this.panelClient.ResumeLayout(false);
            this.panelClient.PerformLayout();
            this.panelHost.ResumeLayout(false);
            this.panelHost.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
