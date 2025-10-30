namespace SharedCockpitClient
{
    partial class RoleDialog
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.RadioButton rbHost;
        private System.Windows.Forms.RadioButton rbClient;
        private System.Windows.Forms.Label lblPeer;
        private System.Windows.Forms.TextBox txtPeerAddress;
        private System.Windows.Forms.Label lblRoom;
        private System.Windows.Forms.TextBox txtRoomName;
        private System.Windows.Forms.CheckBox chkPublic;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.rbHost = new System.Windows.Forms.RadioButton();
            this.rbClient = new System.Windows.Forms.RadioButton();
            this.lblPeer = new System.Windows.Forms.Label();
            this.txtPeerAddress = new System.Windows.Forms.TextBox();
            this.lblRoom = new System.Windows.Forms.Label();
            this.txtRoomName = new System.Windows.Forms.TextBox();
            this.chkPublic = new System.Windows.Forms.CheckBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // rbHost
            // 
            this.rbHost.AutoSize = true;
            this.rbHost.Checked = true;
            this.rbHost.Location = new System.Drawing.Point(15, 15);
            this.rbHost.Name = "rbHost";
            this.rbHost.Size = new System.Drawing.Size(55, 19);
            this.rbHost.TabIndex = 0;
            this.rbHost.TabStop = true;
            this.rbHost.Text = "Host";
            this.rbHost.UseVisualStyleBackColor = true;
            // 
            // rbClient
            // 
            this.rbClient.AutoSize = true;
            this.rbClient.Location = new System.Drawing.Point(15, 45);
            this.rbClient.Name = "rbClient";
            this.rbClient.Size = new System.Drawing.Size(62, 19);
            this.rbClient.TabIndex = 1;
            this.rbClient.Text = "Cliente";
            this.rbClient.UseVisualStyleBackColor = true;
            // 
            // lblPeer
            // 
            this.lblPeer.AutoSize = true;
            this.lblPeer.Location = new System.Drawing.Point(35, 74);
            this.lblPeer.Name = "lblPeer";
            this.lblPeer.Size = new System.Drawing.Size(116, 15);
            this.lblPeer.TabIndex = 2;
            this.lblPeer.Text = "Dirección del host:";
            this.lblPeer.Visible = false;
            // 
            // txtPeerAddress
            // 
            this.txtPeerAddress.Location = new System.Drawing.Point(35, 92);
            this.txtPeerAddress.Name = "txtPeerAddress";
            this.txtPeerAddress.PlaceholderText = "IP o hostname";
            this.txtPeerAddress.Size = new System.Drawing.Size(260, 23);
            this.txtPeerAddress.TabIndex = 3;
            this.txtPeerAddress.Visible = false;
            // 
            // lblRoom
            // 
            this.lblRoom.AutoSize = true;
            this.lblRoom.Location = new System.Drawing.Point(15, 132);
            this.lblRoom.Name = "lblRoom";
            this.lblRoom.Size = new System.Drawing.Size(99, 15);
            this.lblRoom.TabIndex = 4;
            this.lblRoom.Text = "Nombre de sala:";
            // 
            // txtRoomName
            // 
            this.txtRoomName.Location = new System.Drawing.Point(35, 150);
            this.txtRoomName.Name = "txtRoomName";
            this.txtRoomName.PlaceholderText = "Ej: Cessna-Training";
            this.txtRoomName.Size = new System.Drawing.Size(260, 23);
            this.txtRoomName.TabIndex = 5;
            // 
            // chkPublic
            // 
            this.chkPublic.AutoSize = true;
            this.chkPublic.Location = new System.Drawing.Point(35, 179);
            this.chkPublic.Name = "chkPublic";
            this.chkPublic.Size = new System.Drawing.Size(96, 19);
            this.chkPublic.TabIndex = 6;
            this.chkPublic.Text = "Sala pública";
            this.chkPublic.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(139, 214);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 27);
            this.btnOK.TabIndex = 7;
            this.btnOK.Text = "Aceptar";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(220, 214);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 27);
            this.btnCancel.TabIndex = 8;
            this.btnCancel.Text = "Cancelar";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // RoleDialog
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(332, 253);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.chkPublic);
            this.Controls.Add(this.txtRoomName);
            this.Controls.Add(this.lblRoom);
            this.Controls.Add(this.txtPeerAddress);
            this.Controls.Add(this.lblPeer);
            this.Controls.Add(this.rbClient);
            this.Controls.Add(this.rbHost);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "RoleDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Seleccionar rol";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
