using System;
using System.Collections.Generic;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData
{
    public sealed class SimConnectManager : IDisposable
    {
        private SimConnect? simconnect;
        private bool isMock;
        private string userRole = "PILOT";

        private readonly AircraftStateManager aircraftState;
        private readonly object stateLock = new();
        public event Action<SimStateSnapshot>? OnSnapshot;

        private readonly Dictionary<string, object?> liveData = new(StringComparer.OrdinalIgnoreCase);

        public SimConnectManager(AircraftStateManager stateManager)
        {
            aircraftState = stateManager;
            isMock = GlobalFlags.IsLabMode; // 🧪 Detecta modo laboratorio automáticamente
        }

        public void Initialize(IntPtr handle)
        {
            if (isMock)
            {
                Console.WriteLine("[SimConnect] ⚙️ Modo laboratorio activo. Conexión real omitida.");
                return;
            }

            try
            {
                simconnect = new SimConnect("SharedCockpitClient", handle, 0, null, 0);
                Console.WriteLine("[SimConnect] Conexión establecida correctamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] ❌ Error al inicializar: {ex.Message}");
            }
        }

        // === Métodos utilitarios requeridos por otros módulos ===
        public void EnableMockMode()
        {
            isMock = true;
            Console.WriteLine("[SimConnect] 🧪 Modo laboratorio forzado manualmente.");
        }

        public void InjectSnapshot(SimStateSnapshot snapshot)
        {
            lock (stateLock)
            {
                aircraftState.ApplySnapshot(snapshot);
            }
            OnSnapshot?.Invoke(snapshot);
        }

        public void SetUserRole(string role)
        {
            userRole = role;
            Console.WriteLine($"[SimConnect] Rol configurado: {userRole}");
        }

        public void UpdateStateFromSim()
        {
            var controls = new ControlsStruct
            {
                Throttle = TryGetDouble(liveData, "throttle"),
                Flaps = TryGetDouble(liveData, "flaps"),
                GearDown = TryGetBool(liveData, "gearDown"),
                ParkingBrake = TryGetBool(liveData, "parkingBrake")
            };

            var systems = new SystemsStruct
            {
                LightsOn = TryGetBool(liveData, "lightsOn"),
                DoorOpen = TryGetBool(liveData, "doorOpen"),
                AvionicsOn = TryGetBool(liveData, "avionicsOn")
            };

            var snapshot = new SimStateSnapshot
            {
                Controls = controls,
                Systems = systems
            };

            lock (stateLock)
            {
                aircraftState.ApplySnapshot(snapshot);
            }

            OnSnapshot?.Invoke(snapshot);
        }

        // 🔹 Nuevo método: recibir mensajes desde SimConnect
        public void ReceiveMessage()
        {
            if (isMock || simconnect == null)
                return;

            try
            {
                simconnect.ReceiveMessage();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] ⚠️ Error recibiendo mensaje: {ex.Message}");
            }
        }

        private static double TryGetDouble(Dictionary<string, object?> src, string key, double fallback = 0)
        {
            if (src.TryGetValue(key, out var v) && v is not null)
            {
                if (v is double d) return d;
                if (v is float f) return f;
                if (v is int i) return i;
                if (double.TryParse(v.ToString(), out var p)) return p;
            }
            return fallback;
        }

        private static bool TryGetBool(Dictionary<string, object?> src, string key, bool fallback = false)
        {
            if (src.TryGetValue(key, out var v) && v is not null)
            {
                if (v is bool b) return b;
                if (v is int i) return i != 0;
                if (bool.TryParse(v.ToString(), out var p)) return p;
                if (double.TryParse(v.ToString(), out var d)) return Math.Abs(d) > 0.5;
            }
            return fallback;
        }

        public void Dispose()
        {
            simconnect?.Dispose();
            simconnect = null;
        }
    }
}
