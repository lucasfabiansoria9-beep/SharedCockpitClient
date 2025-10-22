namespace SharedCockpitClient
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtIp;
        private System.Windows.Forms.TextBox txtPort;
        private System.Windows.Forms.Button btnHost;
        private System.Windows.Forms.Button btnClient;
        private System.Windows.Forms.Button btnStop;

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
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtIp = new System.Windows.Forms.TextBox();
            this.txtPort = new System.Windows.Forms.TextBox();
            this.btnHost = new System.Windows.Forms.Button();
            this.btnClient = new System.Windows.Forms.Button();
            this.btnStop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 9);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 17);
            this.lblStatus.TabIndex = 0;
            this.lblStatus.Text = "Estado";
            // 
            // txtLog
            // 
            this.txtLog.Location = new System.Drawing.Point(15, 40);
            this.txtLog.Multiline = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(400, 200);
            this.txtLog.ReadOnly = true;
            this.txtLog.TabIndex = 1;
            // 
            // txtIp
            // 
            this.txtIp.Location = new System.Drawing.Point(15, 250);
            this.txtIp.Name = "txtIp";
            this.txtIp.Size = new System.Drawing.Size(150, 22);
            this.txtIp.TabIndex = 2;
            this.txtIp.TextChanged += new System.EventHandler(this.txtIp_TextChanged);
            // 
            // txtPort
            // 
            this.txtPort.Location = new System.Drawing.Point(180, 250);
            this.txtPort.Name = "txtPort";
            this.txtPort.Size = new System.Drawing.Size(60, 22);
            this.txtPort.TabIndex = 3;
            this.txtPort.Text = "8765";
            // 
            // btnHost
            // 
            this.btnHost.Location = new System.Drawing.Point(250, 248);
            this.btnHost.Name = "btnHost";
            this.btnHost.Size = new System.Drawing.Size(75, 25);
            this.btnHost.TabIndex = 4;
            this.btnHost.Text = "Host";
            this.btnHost.UseVisualStyleBackColor = true;
            this.btnHost.Click += new System.EventHandler(this.btnHost_Click);
            // 
            // btnClient
            // 
            this.btnClient.Location = new System.Drawing.Point(330, 248);
            this.btnClient.Name = "btnClient";
            this.btnClient.Size = new System.Drawing.Size(75, 25);
            this.btnClient.TabIndex = 5;
            this.btnClient.Text = "Cliente";
            this.btnClient.UseVisualStyleBackColor = true;
            this.btnClient.Click += new System.EventHandler(this.btnClient_Click);
            // 
            // btnStop
            // 
            this.btnStop.Location = new System.Drawing.Point(410, 248);
            this.btnStop.Name = "btnStop";
            this.btnStop.Size = new System.Drawing.Size(75, 25);
            this.btnStop.TabIndex = 6;
            this.btnStop.Text = "Detener";
            this.btnStop.UseVisualStyleBackColor = true;
            this.btnStop.Click += new System.EventHandler(this.btnStop_Click);
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(500, 300);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.txtIp);
            this.Controls.Add(this.txtPort);
            this.Controls.Add(this.btnHost);
            this.Controls.Add(this.btnClient);
            this.Controls.Add(this.btnStop);
            this.Name = "MainForm";
            this.Text = "Shared Cockpit Client";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
