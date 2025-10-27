using System;
using System.Collections.Generic;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Métodos de extensión para SimStateSnapshot.
    /// Permiten convertir estructuras internas a formato plano y aplicar cambios remotos.
    /// </summary>
    public static class SimStateSnapshotExtensions
    {
        /// <summary>
        /// Convierte el snapshot actual en un diccionario plano compatible con AircraftStateManager.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this SimStateSnapshot snapshot)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Throttle"] = snapshot.Controls.Throttle,
                ["Flaps"] = snapshot.Controls.Flaps,
                ["GearDown"] = snapshot.Controls.GearDown,
                ["ParkingBrake"] = snapshot.Controls.ParkingBrake,
                ["Lights"] = snapshot.Systems.LightsOn,
                ["DoorOpen"] = snapshot.Systems.DoorOpen,
                ["AvionicsOn"] = snapshot.Systems.AvionicsOn,
                ["EngineOn"] = snapshot.Systems.EngineOn
            };

            return dict;
        }

        /// <summary>
        /// Intenta aplicar un cambio remoto al snapshot actual (placeholder para futuras fases).
        /// </summary>
        public static bool TryApplyChange(this SimStateSnapshot snapshot, string path, object? value)
        {
            // Fase 5: se implementará en persistencia o sincronización avanzada.
            return false;
        }

        // === Helpers adicionales ===

        /// <summary>
        /// Indica si la estructura de controles está vacía (sin datos).
        /// </summary>
        public static bool IsDefault(this ControlsStruct c)
            => c.Equals(default(ControlsStruct));

        /// <summary>
        /// Indica si la estructura de sistemas está vacía (sin datos).
        /// </summary>
        public static bool IsDefault(this SystemsStruct s)
            => s.Equals(default(SystemsStruct));

        /// <summary>
        /// Convierte los controles en un diccionario independiente.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this ControlsStruct c)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Throttle"] = c.Throttle,
                ["Flaps"] = c.Flaps,
                ["GearDown"] = c.GearDown,
                ["ParkingBrake"] = c.ParkingBrake
            };
        }

        /// <summary>
        /// Convierte los sistemas en un diccionario independiente.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this SystemsStruct s)
        {
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["Lights"] = s.LightsOn,
                ["DoorOpen"] = s.DoorOpen,
                ["AvionicsOn"] = s.AvionicsOn,
                ["EngineOn"] = s.EngineOn
            };
        }
    }
}
