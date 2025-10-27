using System;
using System.Collections.Generic;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Métodos de extensión para SimStateSnapshot
    /// </summary>
    public static class SimStateSnapshotExtensions
    {
        /// <summary>
        /// Convierte el snapshot actual en un diccionario plano (fase 1).
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this SimStateSnapshot snapshot)
        {
            var dict = new Dictionary<string, object?>();

            if (!snapshot.Controls.Equals(default(ControlsStruct)))
                dict["controls"] = snapshot.Controls;

            if (!snapshot.Systems.Equals(default(SystemsStruct)))
                dict["systems"] = snapshot.Systems;

            return dict;
        }

        /// <summary>
        /// Intenta aplicar un cambio remoto al snapshot (placeholder, no destructivo).
        /// </summary>
        public static bool TryApplyChange(this SimStateSnapshot snapshot, string path, object? value)
        {
            // Fase 1: aún no hay enrutamiento fino.
            return false;
        }

        // === Helpers adicionales para evitar errores de tipo ===
        public static bool IsDefault(this ControlsStruct c) => c.Equals(default(ControlsStruct));
        public static bool IsDefault(this SystemsStruct s) => s.Equals(default(SystemsStruct));

        public static Dictionary<string, object?> ToDictionary(this ControlsStruct c)
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["throttle"] = c.Throttle,
                ["flaps"] = c.Flaps,
                ["gearDown"] = c.GearDown,
                ["parkingBrake"] = c.ParkingBrake
            };
            return d;
        }

        public static Dictionary<string, object?> ToDictionary(this SystemsStruct s)
        {
            var d = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["lightsOn"] = s.LightsOn,
                ["doorOpen"] = s.DoorOpen,
                ["avionicsOn"] = s.AvionicsOn
            };
            return d;
        }
    }
}
