using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedCockpitClient
{
    public partial class MainForm : Form
    {
        // ---- CAMPOS INICIALIZADOS PARA EVITAR NULLABLE WARNINGS ----
        private TcpListener server = null!;
        private TcpClient client = null!;
        private NetworkStream stream = null!;
        private CancellationTokenSource cts = null!;

        private WebSocketManager wsManager = null!; // WebSocket

        // ---- EVENTOS INICIALIZADOS VACÍOS PARA EVITAR NULLABLE WARNINGS ----
        public event Action OnStatusChanged = delegate { };
        public event Action<byte[]> OnDataReceived = delegate { };

        // Estado de conexión
#pragma warning disable CS0414
        private bool isConnected = false;
#pragma warning restore CS0414

        public MainForm()
        {
            InitializeComponent();

            btnStop.Enabled = false;
            txtIp.TextChanged += txtIp_TextChanged!;
        }

        private void txtIp_TextChanged(object sender, EventArgs e)
        {
            // Aquí podrías actualizar la IP si querés
        }

        private async void btnHost_Click(object sender, EventArgs e)
        {
            try
            {
                cts = new CancellationTokenSource();
                server = new TcpListener(System.Net.IPAddress.Any, 12345);
                server.Start();

                isConnected = true;
                UpdateConnectionButtons();

                UpdateStatus("Servidor iniciado.");

                // Inicializamos WebSocketManager para host
                wsManager = new WebSocketManager("ws://127.0.0.1:12345");
                HookWebSocketEvents();
                wsManager.Connect();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error iniciando servidor: " + ex.Message);
            }
        }

        private async void btnClient_Click(object sender, EventArgs e)
        {
            try
            {
                cts = new CancellationTokenSource();
                client = new TcpClient();
                await client.ConnectAsync(txtIp.Text, 12345);
                stream = client.GetStream();

                isConnected = true;
                UpdateConnectionButtons();

                UpdateStatus("Cliente conectado al servidor.");

                // Inicializamos WebSocketManager para cliente
                wsManager = new WebSocketManager($"ws://{txtIp.Text}:12345");
                HookWebSocketEvents();
                wsManager.Connect();
            }
            catch (Exception ex)
            {
                UpdateStatus("Error conectando cliente: " + ex.Message);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                cts?.Cancel();
                wsManager?.Close();

                if (client?.Connected == true)
                {
                    stream.Close();
                    client.Close();
                }

                server?.Stop();

                isConnected = false;
                UpdateConnectionButtons();
                UpdateStatus("Conexión detenida.");
            }
            catch (Exception ex)
            {
                UpdateStatus("Error deteniendo conexión: " + ex.Message);
            }
        }

        private void UpdateStatus(string message)
        {
            lblStatus.Text = message;
            OnStatusChanged.Invoke();
        }

        private void UpdateConnectionButtons()
        {
            btnHost.Enabled = !isConnected;
            btnClient.Enabled = !isConnected;
            btnStop.Enabled = isConnected;
        }

        private void HookWebSocketEvents()
        {
            if (wsManager == null) return;

            wsManager.OnOpen += () =>
            {
                isConnected = true;
                UpdateConnectionButtons();
                UpdateStatus("WebSocket abierto");
            };

            wsManager.OnMessage += msg => UpdateStatus("Mensaje WS: " + msg);

            wsManager.OnError += err =>
            {
                isConnected = false;
                UpdateConnectionButtons();
                UpdateStatus("Error WS: " + err);
            };

            wsManager.OnClose += () =>
            {
                isConnected = false;
                UpdateConnectionButtons();
                UpdateStatus("WebSocket cerrado");
            };
        }
    }
}
