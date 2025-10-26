using System;
using System.Collections.Generic;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData
{
    public class SimConnectManager : IDisposable
    {
        private SimConnect? simconnect;
        private bool connected;
        private bool mockMode;
        private readonly object syncLock = new();
        private SimStateSnapshot currentSnapshot = new();

        public event Action<SimStateSnapshot>? OnSnapshot;

        public bool IsMockMode => mockMode;

        public void Initialize(IntPtr windowHandle)
        {
            try
            {
                if (mockMode)
                {
                    Console.WriteLine("[SimConnect] Modo simulado activo, sin inicializar conexión real.");
                    return;
                }

                simconnect = new SimConnect("SharedCockpitClient", windowHandle, 0, null, 0);
                simconnect.OnRecvSimobjectData += OnSimDataReceived;
                connected = true;
                Console.WriteLine("[SimConnect] Inicializado correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] Error al inicializar: {ex.Message}");
            }
        }

        public void EnableMockMode()
        {
            mockMode = true;
            Console.WriteLine("[SimConnect] Modo simulado activado.");
        }

        public void ReceiveMessage()
        {
            if (!connected || simconnect == null) return;
            try { simconnect.ReceiveMessage(); }
            catch (Exception ex) { Console.WriteLine($"[SimConnect] Error recibiendo mensajes: {ex.Message}"); }
        }

        private void OnSimDataReceived(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            try
            {
                var def = (SimDataDefinition)data.dwDefineID;
                var snapshot = currentSnapshot.Clone();

                switch (def)
                {
                    case SimDataDefinition.Attitude: snapshot.Attitude = (AttitudeStruct)data.dwData[0]; break;
                    case SimDataDefinition.Position: snapshot.Position = (PositionStruct)data.dwData[0]; break;
                    case SimDataDefinition.Speed: snapshot.Speed = (SpeedStruct)data.dwData[0]; break;
                    case SimDataDefinition.Controls: snapshot.Controls = (ControlsStruct)data.dwData[0]; break;
                    case SimDataDefinition.Cabin: snapshot.Cabin = (CabinStruct)data.dwData[0]; break;
                    case SimDataDefinition.Systems: snapshot.Systems = (SystemsStruct)data.dwData[0]; break;
                    case SimDataDefinition.Environment: snapshot.Environment = (EnvironmentStruct)data.dwData[0]; break;
                    case SimDataDefinition.Avionics: snapshot.Avionics = (AvionicsStruct)data.dwData[0]; break;
                }

                lock (syncLock) currentSnapshot = snapshot;
                OnSnapshot?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] Error procesando datos: {ex.Message}");
            }
        }

        public void InjectSnapshot(SimStateSnapshot snapshot)
        {
            if (snapshot == null) return;
            lock (syncLock)
                currentSnapshot = snapshot.Clone();
            OnSnapshot?.Invoke(snapshot);
        }

        public void ApplyRemoteChanges(Dictionary<string, object?> changes)
        {
            if (changes == null) return;

            var updated = currentSnapshot.Clone();
            foreach (var kvp in changes)
                updated.TryApplyChange(kvp.Key, kvp.Value);

            lock (syncLock)
                currentSnapshot = updated;

            OnSnapshot?.Invoke(updated);
        }

        public SimStateSnapshot GetCurrentSnapshot()
        {
            lock (syncLock)
                return currentSnapshot.Clone();
        }

        public void SetUserRole(string role)
        {
            Console.WriteLine($"[SimConnect] Rol local establecido: {role}");
        }

        public void Dispose()
        {
            try
            {
                if (simconnect != null)
                {
                    simconnect.OnRecvSimobjectData -= OnSimDataReceived;
                    simconnect.Dispose();
                    simconnect = null;
                }

                connected = false;
                Console.WriteLine("[SimConnect] Conexión cerrada correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] Error al liberar recursos: {ex.Message}");
            }
        }
    }
}
