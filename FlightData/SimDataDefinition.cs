using System;
using System.Collections.Generic;
using System.Linq;
namespace SharedCockpitClient
{
    public enum SimDataType
    {
        Float64,
        Float32,
        Int32,
        Bool,
        String256
    }

    public sealed record SimVarDescriptor(
        string Path,
        string Name,
        string Units,
        SimDataType DataType,
        bool Writable,
        string Category,
        int? Index = null,
        string? EventWrite = null,
        double? MinDelta = null)
    {
        public string DefinitionKey => Index.HasValue ? $"{Name}:{Index.Value}" : Name;
    }

    public sealed record SimEventDescriptor(string Path, string EventName, string Category);

    /// <summary>
    /// Tabla centralizada de SimVars y SimEvents utilizados por el proyecto.
    /// </summary>
    public static class SimDataDefinition
    {
        private static readonly Lazy<IReadOnlyList<SimVarDescriptor>> _allVars = new(() => BuildVarList());
        private static readonly Lazy<IReadOnlyDictionary<string, SimVarDescriptor>> _varsByPath
            = new(() => _allVars.Value.ToDictionary(v => v.Path, v => v, StringComparer.OrdinalIgnoreCase));

        private static readonly Lazy<IReadOnlyDictionary<string, SimVarDescriptor>> _varsBySimVarName
            = new(() =>
            {
                var dict = new Dictionary<string, SimVarDescriptor>(StringComparer.OrdinalIgnoreCase);
                foreach (var descriptor in _allVars.Value)
                {
                    var key = $"SimVars.{descriptor.Name}";
                    if (!dict.ContainsKey(key))
                        dict[key] = descriptor;

                    if (descriptor.Index.HasValue)
                    {
                        var indexKey = $"SimVars.{descriptor.DefinitionKey}";
                        if (!dict.ContainsKey(indexKey))
                            dict[indexKey] = descriptor;
                    }
                }

                return dict;
            });

        private static readonly Lazy<IReadOnlyList<SimEventDescriptor>> _allEvents = new(() => BuildEvents());

        private static readonly Lazy<IReadOnlyDictionary<string, SimEventDescriptor>> _eventsByPath
            = new(() => _allEvents.Value.ToDictionary(e => e.Path, e => e, StringComparer.OrdinalIgnoreCase));

        private static readonly Lazy<IReadOnlyDictionary<string, SimEventDescriptor>> _eventsByNormalizedName
            = new(() =>
            {
                var dict = new Dictionary<string, SimEventDescriptor>(StringComparer.OrdinalIgnoreCase);
                foreach (var descriptor in _allEvents.Value)
                {
                    var normalized = NormalizeEventName(descriptor.EventName);
                    if (!string.IsNullOrWhiteSpace(normalized) && !dict.ContainsKey(normalized))
                        dict[normalized] = descriptor;
                }

                return dict;
            });

        public static IReadOnlyList<SimVarDescriptor> AllSimVars => _allVars.Value;

        public static IReadOnlyDictionary<string, SimVarDescriptor> VarsByPath => _varsByPath.Value;

        public static IReadOnlyDictionary<string, SimVarDescriptor> VarsBySimVarName => _varsBySimVarName.Value;

        public static IReadOnlyList<SimEventDescriptor> AllSimEvents => _allEvents.Value;

        public static IReadOnlyDictionary<string, SimEventDescriptor> EventsByPath => _eventsByPath.Value;

        public static IReadOnlyDictionary<string, SimEventDescriptor> EventsByNormalizedName => _eventsByNormalizedName.Value;

        public static bool TryGetVar(string path, out SimVarDescriptor descriptor)
        {
            return VarsByPath.TryGetValue(path, out descriptor!);
        }

        public static bool TryGetVarBySimVarKey(string simVarKey, out SimVarDescriptor descriptor)
        {
            if (string.IsNullOrWhiteSpace(simVarKey))
            {
                descriptor = null!;
                return false;
            }

            if (!simVarKey.StartsWith("SimVars.", StringComparison.OrdinalIgnoreCase))
                simVarKey = $"SimVars.{simVarKey}";

            return VarsBySimVarName.TryGetValue(simVarKey, out descriptor!);
        }

        public static bool TryGetEvent(string path, out SimEventDescriptor descriptor)
        {
            return EventsByPath.TryGetValue(path, out descriptor!);
        }

        public static bool TryGetEventByName(string eventName, out SimEventDescriptor descriptor)
        {
            descriptor = null!;
            if (string.IsNullOrWhiteSpace(eventName))
                return false;

            var normalized = NormalizeEventName(eventName);
            return EventsByNormalizedName.TryGetValue(normalized, out descriptor!);
        }

        public static string NormalizeEventName(string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
                return eventName;

            var trimmed = eventName.Trim();
            if (trimmed.StartsWith("K:", StringComparison.OrdinalIgnoreCase))
                trimmed = trimmed.Substring(2);

            return trimmed;
        }

        private static IReadOnlyList<SimVarDescriptor> BuildVarList()
        {
            var vars = new List<SimVarDescriptor>
            {
                // ===== Controles =====
                new("Controls.Throttle[1]", "GENERAL ENG THROTTLE LEVER POSITION:1", "Percent", SimDataType.Float64, true, "Controls", 1, MinDelta: 0.01),
                new("Controls.Throttle[2]", "GENERAL ENG THROTTLE LEVER POSITION:2", "Percent", SimDataType.Float64, true, "Controls", 2, MinDelta: 0.01),
                new("Controls.Propeller[1]", "GENERAL ENG PROP LEVER POSITION:1", "Percent", SimDataType.Float64, true, "Controls", 1, MinDelta: 0.01),
                new("Controls.Propeller[2]", "GENERAL ENG PROP LEVER POSITION:2", "Percent", SimDataType.Float64, true, "Controls", 2, MinDelta: 0.01),
                new("Controls.Mixture[1]", "GENERAL ENG MIXTURE LEVER POSITION:1", "Percent", SimDataType.Float64, true, "Controls", 1, MinDelta: 0.01),
                new("Controls.Mixture[2]", "GENERAL ENG MIXTURE LEVER POSITION:2", "Percent", SimDataType.Float64, true, "Controls", 2, MinDelta: 0.01),
                new("Controls.Aileron", "AILERON POSITION", "Position", SimDataType.Float64, true, "Controls", MinDelta: 0.005),
                new("Controls.Elevator", "ELEVATOR POSITION", "Position", SimDataType.Float64, true, "Controls", MinDelta: 0.005),
                new("Controls.Rudder", "RUDDER POSITION", "Position", SimDataType.Float64, true, "Controls", MinDelta: 0.005),
                new("Controls.ElevatorTrim", "ELEVATOR TRIM POSITION", "Radians", SimDataType.Float64, true, "Controls", MinDelta: 0.002),
                new("Controls.AileronTrim", "AILERON TRIM", "Radians", SimDataType.Float64, true, "Controls", MinDelta: 0.002),
                new("Controls.RudderTrim", "RUDDER TRIM", "Radians", SimDataType.Float64, true, "Controls", MinDelta: 0.002),
                new("Controls.BrakeParking", "BRAKE PARKING POSITION", "Bool", SimDataType.Bool, true, "Brakes"),
                new("Controls.Flaps.Handle", "FLAPS HANDLE INDEX", "Number", SimDataType.Int32, true, "FlightControls"),
                new("Controls.Spoilers.Handle", "SPOILERS HANDLE POSITION", "Percent", SimDataType.Float64, true, "FlightControls", MinDelta: 0.01),
                new("Controls.Gear.Handle", "GEAR HANDLE POSITION", "Bool", SimDataType.Bool, true, "Gear"),

                // ===== Motor / Engine =====
                new("Systems.Engine[1].Starter", "GENERAL ENG STARTER:1", "Bool", SimDataType.Bool, true, "Engine", 1),
                new("Systems.Engine[2].Starter", "GENERAL ENG STARTER:2", "Bool", SimDataType.Bool, true, "Engine", 2),
                new("Systems.Engine[1].Combustion", "GENERAL ENG COMBUSTION:1", "Bool", SimDataType.Bool, false, "Engine", 1),
                new("Systems.Engine[2].Combustion", "GENERAL ENG COMBUSTION:2", "Bool", SimDataType.Bool, false, "Engine", 2),
                new("Systems.Engine[1].OilPressure", "ENG OIL PRESSURE:1", "Psi", SimDataType.Float64, false, "Engine", 1),
                new("Systems.Engine[2].OilPressure", "ENG OIL PRESSURE:2", "Psi", SimDataType.Float64, false, "Engine", 2),
                new("Systems.Engine[1].N1", "TURB ENG N1:1", "Percent", SimDataType.Float64, false, "Engine", 1),
                new("Systems.Engine[2].N1", "TURB ENG N1:2", "Percent", SimDataType.Float64, false, "Engine", 2),

                // ===== Fuel =====
                new("Systems.Fuel.TotalQuantity", "FUEL TOTAL QUANTITY", "Gallons", SimDataType.Float64, false, "Fuel"),
                new("Systems.Fuel.Pump[1]", "FUEL PUMP SWITCH:1", "Bool", SimDataType.Bool, true, "Fuel", 1),
                new("Systems.Fuel.Pump[2]", "FUEL PUMP SWITCH:2", "Bool", SimDataType.Bool, true, "Fuel", 2),
                new("Systems.Fuel.Selector", "FUEL TANK SELECTOR", "Enum", SimDataType.Int32, true, "Fuel"),

                // ===== Electric =====
                new("Systems.Electrical.MasterBattery", "ELECTRICAL MASTER BATTERY", "Bool", SimDataType.Bool, true, "Electrical"),
                new("Systems.Electrical.Avionics", "AVIONICS MASTER SWITCH", "Bool", SimDataType.Bool, true, "Electrical"),
                new("Systems.Electrical.BusVoltage", "ELECTRICAL MAIN BUS VOLTAGE", "Volts", SimDataType.Float64, false, "Electrical"),

                // ===== Lights =====
                new("Systems.Lights.Beacon", "LIGHT BEACON", "Bool", SimDataType.Bool, true, "Lights"),
                new("Systems.Lights.Nav", "LIGHT NAV", "Bool", SimDataType.Bool, true, "Lights"),
                new("Systems.Lights.Strobe", "LIGHT STROBE", "Bool", SimDataType.Bool, true, "Lights"),
                new("Systems.Lights.Taxi", "LIGHT TAXI", "Bool", SimDataType.Bool, true, "Lights"),
                new("Systems.Lights.Landing", "LIGHT LANDING", "Bool", SimDataType.Bool, true, "Lights"),

                // ===== Autopilot =====
                new("Systems.Autopilot.AP_MASTER", "AUTOPILOT MASTER", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_MASTER"),
                new("Systems.Autopilot.FD", "AUTOPILOT FLIGHT DIRECTOR ACTIVE", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "FLIGHT_DIRECTOR_TOGGLE"),
                new("Systems.Autopilot.HDG_HOLD", "AUTOPILOT HEADING LOCK", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_HDG_HOLD"),
                new("Systems.Autopilot.NAV", "AUTOPILOT NAV1 LOCK", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_NAV1_HOLD"),
                new("Systems.Autopilot.APP", "AUTOPILOT APPROACH HOLD", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_APR_HOLD"),
                new("Systems.Autopilot.ALT_HOLD", "AUTOPILOT ALTITUDE LOCK", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_ALT_HOLD"),
                new("Systems.Autopilot.VS_HOLD", "AUTOPILOT VERTICAL HOLD", "Bool", SimDataType.Bool, true, "Autopilot", EventWrite: "AP_VS_HOLD"),
                new("Systems.Autopilot.HDG", "AUTOPILOT HEADING LOCK DIR", "Degrees", SimDataType.Float64, true, "Autopilot", EventWrite: "HEADING_BUG_SET"),
                new("Systems.Autopilot.ALT", "AUTOPILOT ALTITUDE LOCK VAR", "Feet", SimDataType.Float64, true, "Autopilot", EventWrite: "AP_ALT_VAR_SET_ENGLISH"),
                new("Systems.Autopilot.VS", "AUTOPILOT VERTICAL HOLD VAR", "Feet per minute", SimDataType.Float64, true, "Autopilot", EventWrite: "AP_VS_VAR_SET_ENGLISH"),

                // ===== Radios =====
                new("Systems.Radios.Com1Active", "COM ACTIVE FREQUENCY:1", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Com1Standby", "COM STANDBY FREQUENCY:1", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Com2Active", "COM ACTIVE FREQUENCY:2", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Com2Standby", "COM STANDBY FREQUENCY:2", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Nav1Active", "NAV ACTIVE FREQUENCY:1", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Nav1Standby", "NAV STANDBY FREQUENCY:1", "MHz", SimDataType.Float64, true, "Radios"),
                new("Systems.Radios.Transponder", "TRANSPONDER CODE", "Number", SimDataType.Int32, true, "Radios"),

                // ===== Anti-Ice =====
                new("Systems.AntiIce.Pitot", "PITOT HEAT", "Bool", SimDataType.Bool, true, "AntiIce"),
                new("Systems.AntiIce.Structural", "STRUCTURAL DEICE SWITCH", "Bool", SimDataType.Bool, true, "AntiIce"),
                new("Systems.AntiIce.Engine[1]", "ENG ANTI ICE:1", "Bool", SimDataType.Bool, true, "AntiIce", 1),
                new("Systems.AntiIce.Engine[2]", "ENG ANTI ICE:2", "Bool", SimDataType.Bool, true, "AntiIce", 2),

                // ===== Doors & Cabina =====
                new("Systems.Doors.Cabin", "EXIT OPEN:0", "Percent", SimDataType.Float64, true, "Doors"),
                new("Systems.Doors.Cargo", "EXIT OPEN:1", "Percent", SimDataType.Float64, true, "Doors"),
                new("Cabin.Seatbelt", "SEAT BELT SWITCH", "Bool", SimDataType.Bool, true, "Cabin"),
                new("Cabin.NoSmoking", "NO SMOKING SWITCH", "Bool", SimDataType.Bool, true, "Cabin"),

                // ===== Gear =====
                new("Systems.Gear.Nose", "GEAR CENTER POSITION", "Percent", SimDataType.Float64, false, "Gear"),
                new("Systems.Gear.Left", "GEAR LEFT POSITION", "Percent", SimDataType.Float64, false, "Gear"),
                new("Systems.Gear.Right", "GEAR RIGHT POSITION", "Percent", SimDataType.Float64, false, "Gear"),

                // ===== Spoilers/Slats =====
                new("Systems.Spoilers.Position", "SPOILERS POSITION", "Percent", SimDataType.Float64, false, "Spoilers"),
                new("Systems.Spoilers.Armed", "SPOILERS ARMED", "Bool", SimDataType.Bool, true, "Spoilers"),

                // ===== Pressurización =====
                new("Systems.Pressurization.CabinAltitude", "PRESSURIZATION CABIN ALTITUDE", "Feet", SimDataType.Float64, false, "Pressurization"),
                new("Systems.Pressurization.DiffPressure", "PRESSURIZATION DIFFERENTIAL PRESSURE", "Psi", SimDataType.Float64, false, "Pressurization"),

                // ===== APU =====
                new("Systems.APU.Available", "APU GENERATOR SWITCH", "Bool", SimDataType.Bool, true, "APU", EventWrite: "APU_GENERATOR_SWITCH"),
                new("Systems.APU.RPM", "APU PCT RPM", "Percent", SimDataType.Float64, false, "APU"),

                // ===== Environment =====
                new("Environment.OutsideAirTemp", "AMBIENT TEMPERATURE", "Celsius", SimDataType.Float64, false, "Environment"),
                new("Environment.TotalAirTemp", "TOTAL AIR TEMPERATURE", "Celsius", SimDataType.Float64, false, "Environment"),
                new("Environment.Icing", "STRUCTURAL ICE PCT", "Percent", SimDataType.Float64, false, "Environment"),
            };

            if (SimVarCatalogGenerator.TryGetCatalog(out var catalog))
            {
                foreach (var descriptor in catalog.SimVars)
                {
                    if (!vars.Any(existing => existing.Path.Equals(descriptor.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        vars.Add(descriptor);
                    }
                }
            }

            return vars;
        }

        private static IReadOnlyList<SimEventDescriptor> BuildEvents()
        {
            var events = new List<SimEventDescriptor>
            {
                new("Systems.Autopilot.AP_MASTER", "K:AP_MASTER", "Autopilot"),
                new("Systems.Autopilot.FD", "K:FLIGHT_DIRECTOR_TOGGLE", "Autopilot"),
                new("Systems.Autopilot.HDG_HOLD", "K:AP_HDG_HOLD", "Autopilot"),
                new("Systems.Autopilot.NAV", "K:AP_NAV1_HOLD", "Autopilot"),
                new("Systems.Autopilot.APP", "K:AP_APR_HOLD", "Autopilot"),
                new("Systems.Autopilot.ALT_HOLD", "K:AP_ALT_HOLD", "Autopilot"),
                new("Systems.Autopilot.VS_HOLD", "K:AP_VS_HOLD", "Autopilot"),
                new("Systems.Autopilot.HDG", "K:HEADING_BUG_SET", "Autopilot"),
                new("Systems.Autopilot.ALT", "K:AP_ALT_VAR_SET_ENGLISH", "Autopilot"),
                new("Systems.Autopilot.VS", "K:AP_VS_VAR_SET_ENGLISH", "Autopilot"),
                new("Controls.Flaps.StepUp", "K:FLAPS_DECR", "FlightControls"),
                new("Controls.Flaps.StepDown", "K:FLAPS_INCR", "FlightControls"),
                new("Controls.Spoilers.Arm", "K:SPOILERS_ARM_SET", "FlightControls"),
                new("Controls.Gear.Up", "K:GEAR_UP", "Gear"),
                new("Controls.Gear.Down", "K:GEAR_DOWN", "Gear"),
                new("Systems.Lights.Beacon", "K:TOGGLE_BEACON_LIGHTS", "Lights"),
                new("Systems.Lights.Nav", "K:TOGGLE_NAV_LIGHTS", "Lights"),
                new("Systems.Lights.Strobe", "K:STROBES_TOGGLE", "Lights"),
                new("Systems.Lights.Taxi", "K:TOGGLE_TAXI_LIGHTS", "Lights"),
                new("Systems.Lights.Landing", "K:LANDING_LIGHTS_TOGGLE", "Lights"),
                new("Systems.Radios.Com1Swap", "K:COM_STBY_RADIO_SWAP", "Radios"),
                new("Systems.Radios.Com2Swap", "K:COM2_RADIO_SWAP", "Radios"),
                new("Systems.Radios.Nav1Swap", "K:NAV1_RADIO_SWAP", "Radios"),
                new("Cabin.Seatbelt", "K:SEATBELTS_SIGN_TOGGLE", "Cabin"),
                new("Cabin.NoSmoking", "K:NO_SMOKING_SIGN_TOGGLE", "Cabin"),
                new("Systems.AntiIce.Pitot", "K:PITOT_HEAT_TOGGLE", "AntiIce"),
                new("Systems.AntiIce.Structural", "K:ANTI_ICE_TOGGLE", "AntiIce"),
                new("Systems.Fuel.Selector", "K:FUEL_SELECTOR_ALL", "Fuel")
            };

            if (SimVarCatalogGenerator.TryGetCatalog(out var catalog))
            {
                foreach (var descriptor in catalog.SimEvents)
                {
                    if (!events.Any(existing => existing.Path.Equals(descriptor.Path, StringComparison.OrdinalIgnoreCase)))
                    {
                        events.Add(descriptor);
                    }
                }
            }

            return events;
        }
    }
}
