using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    public class SimConnectHandler
    {
        private const int WM_USER_SIMCONNECT = 0x0402;
        private SimConnect? simconnect;
        private readonly IntPtr handle;
        private readonly bool isHost;
        private readonly ConnectionManager connection;

        public bool IsConnected { get; private set; } = false;

        public SimConnectHandler(IntPtr windowHandle, bool host, ConnectionManager conn)
        {
            handle = windowHandle;
            isHost = host;
            connection = conn;
        }

        public void Connect()
        {
            try
            {
                simconnect = new SimConnect("SharedCockpit", handle, WM_USER_SIMCONNECT, null, 0);
                simconnect.OnRecvOpen += Simconnect_OnRecvOpen;
                simconnect.OnRecvQuit += Simconnect_OnRecvQuit;
                simconnect.OnRecvException += Simconnect_OnRecvException;
                simconnect.OnRecvSimobjectData += Simconnect_OnRecvSimobjectData;

                DefineDataStructures();

                IsConnected = true;
                connection?.SendAsync("[SimConnect] Conectado.");
            }
            catch (Exception ex)
            {
                connection?.SendAsync($"[SimConnect Error] {ex.Message}");
            }
        }

        private void Simconnect_OnRecvOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            connection?.SendAsync("[SimConnect] Conexión establecida con MSFS.");
        }

        private void Simconnect_OnRecvQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            IsConnected = false;
            connection?.SendAsync("[SimConnect] Simulador cerrado.");
        }

        private void Simconnect_OnRecvException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            connection?.SendAsync($"[SimConnect Exception] {data.dwException}");
        }

        private void DefineDataStructures()
        {
            if (simconnect == null)
            {
                Console.WriteLine("SimConnect no está inicializado. No se pueden definir las estructuras de datos.");
                return;
            }

            try
            {
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                simconnect.AddToDataDefinition(DEFINITIONS.PlaneData, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);

                simconnect.RegisterDataDefineStruct<PlaneData>(DEFINITIONS.PlaneData);

                Console.WriteLine("Estructuras de datos definidas correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al definir estructuras de datos: {ex.Message}");
            }
        }


        public void RequestData()
        {
            if (simconnect == null || !IsConnected) return;
            simconnect.RequestDataOnSimObject(DATA_REQUESTS.Request1, DEFINITIONS.PlaneData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        private void Simconnect_OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var rec = (PlaneData)data.dwData[0];
            if (isHost)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.Request1, DEFINITIONS.PlaneData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
                string message = $"{rec.Latitude},{rec.Longitude},{rec.Altitude},{rec.Heading}";
                _ = connection.SendAsync(message);
            }
        }

        public void ReceiveRemoteData(string msg)
        {
            try
            {
                string[] parts = msg.Split(',');
                if (parts.Length < 4) return;

                double lat = double.Parse(parts[0]);
                double lon = double.Parse(parts[1]);
                double alt = double.Parse(parts[2]);
                double hdg = double.Parse(parts[3]);

                PlaneData plane = new PlaneData() { Latitude = lat, Longitude = lon, Altitude = alt, Heading = hdg };
                simconnect?.SetDataOnSimObject(DEFINITIONS.PlaneData, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, plane);
            }
            catch (Exception ex)
            {
                connection?.SendAsync($"[Data Error] {ex.Message}");
            }
        }

        public void Disconnect()
        {
            if (IsConnected)
            {
                simconnect?.Dispose();
                IsConnected = false;
            }
        }

        enum DEFINITIONS { PlaneData }
        enum DATA_REQUESTS { Request1 }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct PlaneData
        {
            public double Latitude;
            public double Longitude;
            public double Altitude;
            public double Heading;
        }
    }
}
