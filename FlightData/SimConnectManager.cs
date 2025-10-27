using System;
using System.Collections.Generic;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Administra la conexión SimConnect o modo simulado (Lab Mode).
    /// Actualiza el estado del avión y propaga snapshots hacia AircraftStateManager.
    /// </summary>
    public sealed class SimConnectManager : IDisposable
    {
        private SimConnect? simconnect;
        private bool isMock;

        private string userRole = "PILOT"; // ✅ Requerido por LegacyWebSocketManager
        private readonly AircraftStateManager aircraftState;
        private readonly object stateLock = new();
        public event Action<SimStateSnapshot>? OnSnapshot;

        private readonly Dictionary<string, object?> liveData = new(StringComparer.OrdinalIgnoreCase);

        public SimConnectManager(AircraftStateManager stateManager)
        {
            aircraftState = stateManager ?? throw new ArgumentNullException(nameof(stateManager));
            isMock = GlobalFlags.IsLabMode; // 🧪 Detecta modo laboratorio automáticamente
        }

        /// <summary>
        /// Inicializa la conexión SimConnect real, o simula si está en modo laboratorio.
        /// </summary>
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

        /// <summary>
        /// Permite activar el modo simulado manualmente (sin SimConnect real).
        /// </summary>
        public void EnableMockMode()
        {
            isMock = true;
            Console.WriteLine("[SimConnect] 🧪 Modo laboratorio forzado manualmente.");
        }

        /// <summary>
        /// Configura el rol del usuario local (PILOT / COPILOT) para logging y control.
        /// </summary>
        public void SetUserRole(string role)
        {
            userRole = role.ToUpperInvariant();
            Console.WriteLine($"[SimConnect] Rol configurado: {userRole}");
        }

        /// <summary>
        /// Inyecta un snapshot simulado en el estado global del avión.
        /// </summary>
        public void InjectSnapshot(SimStateSnapshot snapshot)
        {
            lock (stateLock)
            {
                var dict = ConvertSnapshotToDictionary(snapshot);
                aircraftState.ApplySnapshot(dict);
            }

            OnSnapshot?.Invoke(snapshot);
        }

        /// <summary>
        /// Actualiza el estado del avión leyendo variables simuladas o de SimConnect.
        /// </summary>
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
                AvionicsOn = TryGetBool(liveData, "avionicsOn"),
                EngineOn = TryGetBool(liveData, "engineOn")
            };

            var snapshot = new SimStateSnapshot
            {
                Controls = controls,
                Systems = systems
            };

            lock (stateLock)
            {
                var dict = ConvertSnapshotToDictionary(snapshot);
                aircraftState.ApplySnapshot(dict);
            }

            OnSnapshot?.Invoke(snapshot);
        }

        /// <summary>
        /// Convierte un snapshot estructurado a un diccionario compatible con AircraftStateManager.
        /// </summary>
        private static Dictionary<string, object?> ConvertSnapshotToDictionary(SimStateSnapshot s)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                { "Throttle", s.Controls.Throttle },
                { "Flaps", s.Controls.Flaps },
                { "GearDown", s.Controls.GearDown },
                { "ParkingBrake", s.Controls.ParkingBrake },
                { "Lights", s.Systems.LightsOn },
                { "DoorOpen", s.Systems.DoorOpen },
                { "AvionicsOn", s.Systems.AvionicsOn },
                { "EngineOn", s.Systems.EngineOn }
            };
        }

        /// <summary>
        /// Procesa mensajes entrantes desde SimConnect.
        /// </summary>
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

        // === Helpers ===
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
