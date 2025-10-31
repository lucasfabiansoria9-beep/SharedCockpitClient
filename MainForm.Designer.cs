#nullable enable
namespace SharedCockpitClient
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer? components = null;
        private System.Windows.Forms.TableLayoutPanel? tableStatus;
        private System.Windows.Forms.Label? lblRoleCaption;
        private System.Windows.Forms.Label? lblRoleValue;
        private System.Windows.Forms.Label? lblRoomCaption;
        private System.Windows.Forms.Label? lblRoomValue;
        private System.Windows.Forms.Label? lblNetworkCaption;
        private System.Windows.Forms.Label? lblNetworkValue;
        private System.Windows.Forms.Label? lblLatencyCaption;
        private System.Windows.Forms.Label? lblLatencyValue;
        private System.Windows.Forms.Label? lblDiffCaption;
        private System.Windows.Forms.Label? lblDiffValue;
        private System.Windows.Forms.Label? lblLastSentCaption;
        private System.Windows.Forms.Label? lblLastSentValue;
        private System.Windows.Forms.Label? lblLastReceivedCaption;
        private System.Windows.Forms.Label? lblLastReceivedValue;
        private System.Windows.Forms.Button? btnStartHost;
        private System.Windows.Forms.Button? btnConnectClient;
        private System.Windows.Forms.Button? btnStop;
        private System.Windows.Forms.TextBox? txtLog;

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
            this.tableStatus = new System.Windows.Forms.TableLayoutPanel();
            this.lblRoleCaption = new System.Windows.Forms.Label();
            this.lblRoleValue = new System.Windows.Forms.Label();
            this.lblRoomCaption = new System.Windows.Forms.Label();
            this.lblRoomValue = new System.Windows.Forms.Label();
            this.lblNetworkCaption = new System.Windows.Forms.Label();
            this.lblNetworkValue = new System.Windows.Forms.Label();
            this.lblLatencyCaption = new System.Windows.Forms.Label();
            this.lblLatencyValue = new System.Windows.Forms.Label();
            this.lblDiffCaption = new System.Windows.Forms.Label();
            this.lblDiffValue = new System.Windows.Forms.Label();
            this.lblLastSentCaption = new System.Windows.Forms.Label();
            this.lblLastSentValue = new System.Windows.Forms.Label();
            this.lblLastReceivedCaption = new System.Windows.Forms.Label();
            this.lblLastReceivedValue = new System.Windows.Forms.Label();
            this.btnStartHost = new System.Windows.Forms.Button();
            this.btnConnectClient = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.tableStatus.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableStatus
            // 
            this.tableStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tableStatus.ColumnCount = 2;
            this.tableStatus.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tableStatus.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.tableStatus.Controls.Add(this.lblRoleCaption, 0, 0);
            this.tableStatus.Controls.Add(this.lblRoleValue, 1, 0);
            this.tableStatus.Controls.Add(this.lblRoomCaption, 0, 1);
            this.tableStatus.Controls.Add(this.lblRoomValue, 1, 1);
            this.tableStatus.Controls.Add(this.lblNetworkCaption, 0, 2);
            this.tableStatus.Controls.Add(this.lblNetworkValue, 1, 2);
            this.tableStatus.Controls.Add(this.lblLatencyCaption, 0, 3);
            this.tableStatus.Controls.Add(this.lblLatencyValue, 1, 3);
            this.tableStatus.Controls.Add(this.lblDiffCaption, 0, 4);
            this.tableStatus.Controls.Add(this.lblDiffValue, 1, 4);
            this.tableStatus.Controls.Add(this.lblLastSentCaption, 0, 5);
            this.tableStatus.Controls.Add(this.lblLastSentValue, 1, 5);
            this.tableStatus.Controls.Add(this.lblLastReceivedCaption, 0, 6);
            this.tableStatus.Controls.Add(this.lblLastReceivedValue, 1, 6);
            this.tableStatus.Location = new System.Drawing.Point(20, 20);
            this.tableStatus.Name = "tableStatus";
            this.tableStatus.RowCount = 7;
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.tableStatus.Size = new System.Drawing.Size(640, 168);
            this.tableStatus.TabIndex = 0;
            // 
            // lblRoleCaption
            // 
            this.lblRoleCaption.AutoSize = true;
            this.lblRoleCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoleCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblRoleCaption.Location = new System.Drawing.Point(3, 0);
            this.lblRoleCaption.Name = "lblRoleCaption";
            this.lblRoleCaption.Size = new System.Drawing.Size(218, 24);
            this.lblRoleCaption.TabIndex = 0;
            this.lblRoleCaption.Text = "Rol";
            this.lblRoleCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRoleValue
            // 
            this.lblRoleValue.AutoSize = true;
            this.lblRoleValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoleValue.Location = new System.Drawing.Point(227, 0);
            this.lblRoleValue.Name = "lblRoleValue";
            this.lblRoleValue.Size = new System.Drawing.Size(410, 24);
            this.lblRoleValue.TabIndex = 1;
            this.lblRoleValue.Text = "-";
            this.lblRoleValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRoomCaption
            // 
            this.lblRoomCaption.AutoSize = true;
            this.lblRoomCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoomCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblRoomCaption.Location = new System.Drawing.Point(3, 24);
            this.lblRoomCaption.Name = "lblRoomCaption";
            this.lblRoomCaption.Size = new System.Drawing.Size(218, 24);
            this.lblRoomCaption.TabIndex = 2;
            this.lblRoomCaption.Text = "Sala";
            this.lblRoomCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblRoomValue
            // 
            this.lblRoomValue.AutoSize = true;
            this.lblRoomValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoomValue.Location = new System.Drawing.Point(227, 24);
            this.lblRoomValue.Name = "lblRoomValue";
            this.lblRoomValue.Size = new System.Drawing.Size(410, 24);
            this.lblRoomValue.TabIndex = 3;
            this.lblRoomValue.Text = "-";
            this.lblRoomValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNetworkCaption
            // 
            this.lblNetworkCaption.AutoSize = true;
            this.lblNetworkCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNetworkCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblNetworkCaption.Location = new System.Drawing.Point(3, 48);
            this.lblNetworkCaption.Name = "lblNetworkCaption";
            this.lblNetworkCaption.Size = new System.Drawing.Size(218, 24);
            this.lblNetworkCaption.TabIndex = 4;
            this.lblNetworkCaption.Text = "Red";
            this.lblNetworkCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblNetworkValue
            // 
            this.lblNetworkValue.AutoSize = true;
            this.lblNetworkValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblNetworkValue.Location = new System.Drawing.Point(227, 48);
            this.lblNetworkValue.Name = "lblNetworkValue";
            this.lblNetworkValue.Size = new System.Drawing.Size(410, 24);
            this.lblNetworkValue.TabIndex = 5;
            this.lblNetworkValue.Text = "-";
            this.lblNetworkValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLatencyCaption
            // 
            this.lblLatencyCaption.AutoSize = true;
            this.lblLatencyCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLatencyCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLatencyCaption.Location = new System.Drawing.Point(3, 72);
            this.lblLatencyCaption.Name = "lblLatencyCaption";
            this.lblLatencyCaption.Size = new System.Drawing.Size(218, 24);
            this.lblLatencyCaption.TabIndex = 6;
            this.lblLatencyCaption.Text = "Latencia";
            this.lblLatencyCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLatencyValue
            // 
            this.lblLatencyValue.AutoSize = true;
            this.lblLatencyValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLatencyValue.Location = new System.Drawing.Point(227, 72);
            this.lblLatencyValue.Name = "lblLatencyValue";
            this.lblLatencyValue.Size = new System.Drawing.Size(410, 24);
            this.lblLatencyValue.TabIndex = 7;
            this.lblLatencyValue.Text = "-";
            this.lblLatencyValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDiffCaption
            // 
            this.lblDiffCaption.AutoSize = true;
            this.lblDiffCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDiffCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblDiffCaption.Location = new System.Drawing.Point(3, 96);
            this.lblDiffCaption.Name = "lblDiffCaption";
            this.lblDiffCaption.Size = new System.Drawing.Size(218, 24);
            this.lblDiffCaption.TabIndex = 8;
            this.lblDiffCaption.Text = "Variables en último diff";
            this.lblDiffCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDiffValue
            // 
            this.lblDiffValue.AutoSize = true;
            this.lblDiffValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDiffValue.Location = new System.Drawing.Point(227, 96);
            this.lblDiffValue.Name = "lblDiffValue";
            this.lblDiffValue.Size = new System.Drawing.Size(410, 24);
            this.lblDiffValue.TabIndex = 9;
            this.lblDiffValue.Text = "-";
            this.lblDiffValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastSentCaption
            // 
            this.lblLastSentCaption.AutoSize = true;
            this.lblLastSentCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastSentCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLastSentCaption.Location = new System.Drawing.Point(3, 120);
            this.lblLastSentCaption.Name = "lblLastSentCaption";
            this.lblLastSentCaption.Size = new System.Drawing.Size(218, 24);
            this.lblLastSentCaption.TabIndex = 10;
            this.lblLastSentCaption.Text = "Último snapshot enviado";
            this.lblLastSentCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastSentValue
            // 
            this.lblLastSentValue.AutoSize = true;
            this.lblLastSentValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastSentValue.Location = new System.Drawing.Point(227, 120);
            this.lblLastSentValue.Name = "lblLastSentValue";
            this.lblLastSentValue.Size = new System.Drawing.Size(410, 24);
            this.lblLastSentValue.TabIndex = 11;
            this.lblLastSentValue.Text = "-";
            this.lblLastSentValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastReceivedCaption
            // 
            this.lblLastReceivedCaption.AutoSize = true;
            this.lblLastReceivedCaption.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastReceivedCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.lblLastReceivedCaption.Location = new System.Drawing.Point(3, 144);
            this.lblLastReceivedCaption.Name = "lblLastReceivedCaption";
            this.lblLastReceivedCaption.Size = new System.Drawing.Size(218, 24);
            this.lblLastReceivedCaption.TabIndex = 12;
            this.lblLastReceivedCaption.Text = "Último snapshot aplicado";
            this.lblLastReceivedCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblLastReceivedValue
            // 
            this.lblLastReceivedValue.AutoSize = true;
            this.lblLastReceivedValue.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLastReceivedValue.Location = new System.Drawing.Point(227, 144);
            this.lblLastReceivedValue.Name = "lblLastReceivedValue";
            this.lblLastReceivedValue.Size = new System.Drawing.Size(410, 24);
            this.lblLastReceivedValue.TabIndex = 13;
            this.lblLastReceivedValue.Text = "-";
            this.lblLastReceivedValue.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnStartHost
            // 
            this.btnStartHost.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStartHost.Location = new System.Drawing.Point(520, 200);
            this.btnStartHost.Name = "btnStartHost";
            this.btnStartHost.Size = new System.Drawing.Size(140, 32);
            this.btnStartHost.TabIndex = 1;
            this.btnStartHost.Text = "Iniciar como HOST";
            this.btnStartHost.UseVisualStyleBackColor = true;
            this.btnStartHost.Click += new System.EventHandler(this.btnStartHost_Click);
            // 
            // btnConnectClient
            // 
            this.btnConnectClient.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnConnectClient.Location = new System.Drawing.Point(520, 240);
            this.btnConnectClient.Name = "btnConnectClient";
            this.btnConnectClient.Size = new System.Drawing.Size(140, 32);
            this.btnConnectClient.TabIndex = 2;
            this.btnConnectClient.Text = "Conectar como CLIENTE";
            this.btnConnectClient.UseVisualStyleBackColor = true;
            this.btnConnectClient.Click += new System.EventHandler(this.btnConnectClient_Click);
            // 
            // btnStop
            // 
            this.btnStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnStop.Location = new System.Drawing.Point(520, 280);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(140, 32);
            this.btnStop.TabIndex = 3;
            this.btnStop.Text = "Detener";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(20, 200);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(480, 260);
            this.txtLog.TabIndex = 4;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(684, 481);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnStop);
            this.Controls.Add(this.btnConnectClient);
            this.Controls.Add(this.btnStartHost);
            this.Controls.Add(this.tableStatus);
            this.MinimumSize = new System.Drawing.Size(700, 520);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "SharedCockpitClient";
            this.tableStatus.ResumeLayout(false);
            this.tableStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
