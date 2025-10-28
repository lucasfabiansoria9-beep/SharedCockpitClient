using System;
using System.Collections.Generic;
using System.Linq;
using SharedCockpitClient.Tools;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Representa un snapshot (full o incremental) del estado del simulador con rutas canónicas.
    /// </summary>
    public sealed class SimStateSnapshot
    {
        private static readonly IReadOnlyList<string> s_defaultSimVarKeys = BuildDefaultSimVarList();
        private readonly Dictionary<string, object?> _values;

        public SimStateSnapshot()
            : this(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public SimStateSnapshot(IDictionary<string, object?> values, DateTime? timestampUtc = null, bool isDiff = false, long sequence = 0)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
            TimestampUtc = timestampUtc ?? DateTime.UtcNow;
            IsDiff = isDiff;
            Sequence = sequence;
            EnsureDefaultKeys();
        }

        /// <summary>
        /// Diccionario base (SimVar → valor) con comparador insensible a mayúsculas.
        /// Permite acceso directo para serialización JSON.
        /// </summary>
        public Dictionary<string, object?> Data
        {
            get => _values;
            set
            {
                _values.Clear();
                if (value != null)
                {
                    foreach (var kv in value)
                    {
                        _values[kv.Key] = kv.Value;
                    }
                }

                EnsureDefaultKeys();
            }
        }

        /// <summary>
        /// Conjunto de SimVars soportadas por defecto (300+ del catálogo MSFS 2024).
        /// </summary>
        public static IReadOnlyList<string> DefaultSimVarKeys => s_defaultSimVarKeys;

        /// <summary>
        /// Instante en UTC en que el snapshot fue generado.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Indica si representa únicamente cambios respecto al snapshot previo.
        /// </summary>
        public bool IsDiff { get; set; }

        /// <summary>
        /// Secuencia global o correlativo asociado al snapshot.
        /// </summary>
        public long Sequence { get; }

        /// <summary>
        /// Valores aplanados (ruta → valor).
        /// </summary>
        public IReadOnlyDictionary<string, object?> Values => _values;

        /// <summary>
        /// Obtiene el valor para una ruta concreta.
        /// </summary>
        public bool TryGetValue(string path, out object? value)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return _values.TryGetValue(path, out value);
        }

        /// <summary>
        /// Establece/actualiza una ruta dentro del snapshot.
        /// </summary>
        public void Set(string path, object? value)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            _values[path] = value;
        }

        /// <summary>
        /// Aplica múltiples valores en lote (ruta → valor).
        /// </summary>
        public void UpdateFromDictionary(IReadOnlyDictionary<string, object?> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            foreach (var kv in source)
            {
                _values[kv.Key] = kv.Value;
            }
        }

        /// <summary>
        /// Aplica un diff recibido (SimVar → valor). Null elimina el valor.
        /// </summary>
        public void ApplyDiff(IDictionary<string, object?> diff)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));

            foreach (var kv in diff)
            {
                if (kv.Value is null)
                    _values.Remove(kv.Key);
                else
                    _values[kv.Key] = kv.Value;
            }

            EnsureDefaultKeys();
        }

        /// <summary>
        /// Crea un snapshot incremental sobre esta instancia.
        /// </summary>
        public SimStateSnapshot CreateDiff(IDictionary<string, object?> diffValues, long sequence)
        {
            return new SimStateSnapshot(diffValues ?? throw new ArgumentNullException(nameof(diffValues)), DateTime.UtcNow, true, sequence);
        }

        /// <summary>
        /// Funde un diff dentro de una copia del snapshot actual.
        /// </summary>
        public SimStateSnapshot MergeDiff(SimStateSnapshot diff)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));

            var merged = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in diff.Values)
            {
                if (kv.Value is null)
                    continue;

                merged[kv.Key] = kv.Value;
            }

            return new SimStateSnapshot(merged, diff.TimestampUtc, false, diff.Sequence);
        }

        public bool TryGetDouble(string path, out double value)
            => SimStateSnapshotExtensions.TryGetDouble(this, path, out value);

        public bool TryGetInt32(string path, out int value)
        {
            if (!TryGetValue(path, out var raw) || raw is null)
            {
                value = 0;
                return false;
            }

            switch (raw)
            {
                case int i:
                    value = i;
                    return true;
                case long l when l >= int.MinValue && l <= int.MaxValue:
                    value = (int)l;
                    return true;
                case double d when d >= int.MinValue && d <= int.MaxValue:
                    value = (int)Math.Round(d);
                    return true;
                case string s when int.TryParse(s, out var parsed):
                    value = parsed;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        public bool TryGetInt64(string path, out long value)
        {
            if (!TryGetValue(path, out var raw) || raw is null)
            {
                value = 0;
                return false;
            }

            switch (raw)
            {
                case long l:
                    value = l;
                    return true;
                case int i:
                    value = i;
                    return true;
                case double d:
                    value = (long)Math.Round(d);
                    return true;
                case string s when long.TryParse(s, out var parsed):
                    value = parsed;
                    return true;
                default:
                    value = 0;
                    return false;
            }
        }

        public SimStateSnapshot Clone()
        {
            return new SimStateSnapshot(new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase), TimestampUtc, IsDiff, Sequence);
        }

        public override string ToString()
        {
            return $"Snapshot[{TimestampUtc:O}] (diff={IsDiff}, values={_values.Count})";
        }

        /// <summary>
        /// Construye un snapshot a partir de pares ruta/valor.
        /// </summary>
        public static SimStateSnapshot FromPairs(IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            return new SimStateSnapshot(pairs.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase));
        }

        private void EnsureDefaultKeys()
        {
            foreach (var key in s_defaultSimVarKeys)
            {
                if (!_values.ContainsKey(key))
                    _values[key] = null;
            }
        }

        private static IReadOnlyList<string> BuildDefaultSimVarList()
        {
            var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddRange(IEnumerable<string> items)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        keys.Add(item.Trim());
                }
            }

            void AddIndexed(IEnumerable<string> items, int startIndex, int endIndex)
            {
                foreach (var item in items)
                {
                    for (int i = startIndex; i <= endIndex; i++)
                    {
                        keys.Add($"{item}:{i}");
                    }
                }
            }

            AddRange(new[]
            {
                "PLANE LATITUDE",
                "PLANE LONGITUDE",
                "PLANE ALTITUDE",
                "PLANE ALT ABOVE GROUND",
                "PLANE HEADING DEGREES TRUE",
                "PLANE HEADING DEGREES MAGNETIC",
                "PLANE PITCH DEGREES",
                "PLANE BANK DEGREES",
                "VELOCITY WORLD X",
                "VELOCITY WORLD Y",
                "VELOCITY WORLD Z",
                "ACCELERATION BODY X",
                "ACCELERATION BODY Y",
                "ACCELERATION BODY Z",
                "ROTATION VELOCITY BODY X",
                "ROTATION VELOCITY BODY Y",
                "ROTATION VELOCITY BODY Z",
                "SIM ON GROUND",
                "ON GROUND",
                "AIRSPEED INDICATED",
                "AIRSPEED TRUE",
                "GROUND VELOCITY",
                "VERTICAL SPEED",
                "TOTAL WEIGHT",
                "EMPTY WEIGHT",
                "FUEL TOTAL QUANTITY",
                "FUEL TOTAL CAPACITY",
                "FUEL SELECTED QUANTITY",
                "FUEL TOTAL WEIGHT",
                "FUEL FLOW TOTAL",
                "FUEL USED SINCE START",
                "FUEL TANK CENTER LEVEL",
                "FUEL TANK CENTER2 LEVEL",
                "FUEL TANK CENTER3 LEVEL",
                "FUEL TANK LEFT MAIN LEVEL",
                "FUEL TANK LEFT AUX LEVEL",
                "FUEL TANK LEFT TIP LEVEL",
                "FUEL TANK LEFT TIP QUANTITY",
                "FUEL TANK RIGHT MAIN LEVEL",
                "FUEL TANK RIGHT AUX LEVEL",
                "FUEL TANK RIGHT TIP LEVEL",
                "FUEL TANK RIGHT TIP QUANTITY",
                "FUEL TANK EXTERNAL1 LEVEL",
                "FUEL TANK EXTERNAL2 LEVEL",
                "FUEL TANK CENTER QUANTITY",
                "FUEL TANK CENTER2 QUANTITY",
                "FUEL TANK CENTER3 QUANTITY",
                "FUEL TANK LEFT MAIN QUANTITY",
                "FUEL TANK LEFT AUX QUANTITY",
                "FUEL TANK LEFT TIP QUANTITY",
                "FUEL TANK RIGHT MAIN QUANTITY",
                "FUEL TANK RIGHT AUX QUANTITY",
                "FUEL TANK RIGHT TIP QUANTITY",
                "FUEL TANK EXTERNAL1 QUANTITY",
                "FUEL TANK EXTERNAL2 QUANTITY",
                "GPS WP NEXT ID",
                "GPS WP DISTANCE",
                "GPS WP BEARING",
                "GPS WP ETE",
                "GPS WP CROSS TRACK",
                "GPS ACTIVE FLIGHT PLAN LEG",
                "GPS POSITION LAT",
                "GPS POSITION LON",
                "GPS POSITION ALT",
                "ADF ACTIVE FREQUENCY",
                "ADF RADIAL",
                "ADF IDENT",
                "ADF FREQUENCY",
                "HSI CDI NEEDLE",
                "HSI GSI NEEDLE",
                "NAV CDI:1",
                "NAV CDI:2",
                "NAV CDI:3",
                "NAV CDI:4",
                "NAV GS ERROR:1",
                "NAV GS ERROR:2",
                "NAV GS ERROR:3",
                "NAV GS ERROR:4",
                "NAV DME:1",
                "NAV DME:2",
                "NAV DME:3",
                "NAV DME:4",
                "NAV IDENT:1",
                "NAV IDENT:2",
                "NAV IDENT:3",
                "NAV IDENT:4",
                "NAV MAGVAR:1",
                "NAV MAGVAR:2",
                "NAV MAGVAR:3",
                "NAV MAGVAR:4",
                "NAV HAS GLIDE SLOPE:1",
                "NAV HAS GLIDE SLOPE:2",
                "NAV HAS GLIDE SLOPE:3",
                "NAV HAS GLIDE SLOPE:4",
                "NAV HAS LOCALIZER:1",
                "NAV HAS LOCALIZER:2",
                "NAV HAS LOCALIZER:3",
                "NAV HAS LOCALIZER:4",
                "AUTOPILOT MASTER",
                "AUTOPILOT NAV1 LOCK",
                "AUTOPILOT HEADING LOCK",
                "AUTOPILOT ALTITUDE LOCK",
                "AUTOPILOT ATTITUDE HOLD",
                "AUTOPILOT APPROACH HOLD",
                "AUTOPILOT BACKCOURSE HOLD",
                "AUTOPILOT FLIGHT LEVEL CHANGE",
                "AUTOPILOT AIRSPEED HOLD",
                "AUTOPILOT MACH HOLD",
                "AUTOPILOT VS HOLD",
                "AUTOPILOT YAW DAMPER",
                "AUTOPILOT MACH NUMBER",
                "AUTOPILOT AIRSPEED HOLD VAR",
                "AUTOPILOT HEADING LOCK DIR",
                "AUTOPILOT ALTITUDE LOCK VAR",
                "AUTOPILOT VERTICAL HOLD VAR",
                "AUTOPILOT GLIDESLOPE HOLD",
                "AUTOPILOT BACKCOURSE ACTIVE",
                "AUTOPILOT PITCH HOLD",
                "AUTOPILOT WING LEVELER",
                "AUTOTHROTTLE ACTIVE",
                "AUTOTHROTTLE ARM",
                "AUTOPILOT AVAILABLE",
                "AVIONICS MASTER SWITCH",
                "AVIONICS SWITCH PANEL",
                "AVIONICS MASTER SET",
                "BRAKE PARKING POSITION",
                "BRAKE LEFT POSITION",
                "BRAKE RIGHT POSITION",
                "BRAKE INDICATOR",
                "BRAKE ALTERNATE INDICATOR",
                "SPOILERS HANDLE POSITION",
                "SPOILERS ARMED",
                "SPOILERS LEFT POSITION",
                "SPOILERS RIGHT POSITION",
                "FLAPS HANDLE INDEX",
                "FLAPS HANDLE POSITION",
                "FLAPS NUM HANDLE POSITIONS",
                "GEAR HANDLE POSITION",
                "GEAR TOTAL PCT EXTENDED",
                "GEAR CENTER POSITION",
                "GEAR LEFT POSITION",
                "GEAR RIGHT POSITION",
                "GEAR ANIMATION POSITION",
                "TAILWHEEL LOCK ON",
                "RUDDER POSITION",
                "AILERON POSITION",
                "ELEVATOR POSITION",
                "ELEVATOR TRIM POSITION",
                "AILERON TRIM POSITION",
                "RUDDER TRIM POSITION",
                "CABIN ALTITUDE",
                "CABIN PRESSURE DIFFERENTIAL",
                "CABIN TEMPERATURE",
                "CABIN LIGHTS ON",
                "CABIN SEATBELTS ALERT SWITCH",
                "CABIN NO SMOKING ALERT SWITCH",
                "CABIN PRESSURE DUMP SWITCH",
                "CABIN DOOR OPEN",
                "LIGHT NAV ON",
                "LIGHT BEACON ON",
                "LIGHT LANDING ON",
                "LIGHT TAXI ON",
                "LIGHT STROBE ON",
                "LIGHT PANEL ON",
                "LIGHT CABIN ON",
                "LIGHT LOGO ON",
                "LIGHT WING ON",
                "LIGHT RECOGNITION ON",
                "LIGHT GLARESHIELD ON",
                "LIGHT PEDESTRAL ON",
                "PITOT HEAT ON",
                "STRUCTURAL DEICE SWITCH",
                "STRUCTURAL ICE PCT",
                "STRUCTURAL DEICE AVAILABLE",
                "ENG ANTI ICE AVAILABLE",
                "ENG ANTI ICE:1",
                "ENG ANTI ICE:2",
                "ENG ANTI ICE:3",
                "ENG ANTI ICE:4",
                "ENG ANTI ICE:5",
                "ENG ANTI ICE:6",
                "ENG ANTI ICE:7",
                "ENG ANTI ICE:8",
                "ELECTRICAL MASTER BATTERY",
                "ELECTRICAL MASTER ALTERNATOR",
                "ELECTRICAL BATTERY LOAD",
                "ELECTRICAL MAIN BUS VOLTAGE",
                "ELECTRICAL MAIN BUS AMPS",
                "ELECTRICAL BATTERY VOLTAGE",
                "ELECTRICAL BATTERY AMPS",
                "ELECTRICAL BUS VOLTAGE:1",
                "ELECTRICAL BUS VOLTAGE:2",
                "ELECTRICAL BUS VOLTAGE:3",
                "ELECTRICAL BUS VOLTAGE:4",
                "ELECTRICAL BUS AMPS:1",
                "ELECTRICAL BUS AMPS:2",
                "ELECTRICAL BUS AMPS:3",
                "ELECTRICAL BUS AMPS:4",
                "ELECTRICAL GEN ALT BUS VOLTAGE",
                "ELECTRICAL GEN ALT BUS AMPS",
                "APU GENERATOR ACTIVE",
                "APU GENERATOR SWITCH",
                "APU RPM",
                "APU PCT RPM",
                "APU EXHAUST GAS TEMPERATURE",
                "APU AVAILABLE",
                "APU VOLTAGE",
                "APU FREQUENCY",
                "APU STARTER SWITCH",
                "APU BLEED AIR",
                "HYDRAULIC PRESSURE",
                "HYDRAULIC RESERVOIR QUANTITY",
                "HYDRAULIC SYSTEM PRESSURE",
                "HYDRAULIC SYSTEM LEAK",
                "PRESSURE ALTITUDE",
                "BAROMETER PRESSURE",
                "STATIC PRESSURE",
                "AMBIENT PRESSURE",
                "AMBIENT TEMPERATURE",
                "AMBIENT WIND VELOCITY",
                "AMBIENT WIND DIRECTION",
                "AMBIENT WIND X",
                "AMBIENT WIND Y",
                "AMBIENT WIND Z",
                "AMBIENT VISIBILITY",
                "TOTAL AIR TEMPERATURE",
                "DEW POINT",
                "SEA LEVEL PRESSURE",
                "RELATIVE HUMIDITY",
                "SUN POSITION ALTITUDE",
                "SUN POSITION AZIMUTH",
                "WORLD VERTICAL WIND",
                "WORLD ORIENTATION YAW",
                "WORLD ORIENTATION PITCH",
                "WORLD ORIENTATION ROLL",
                "SIM RATE",
                "REALISM",
                "SIMULATION PAUSED",
                "PAUSED",
                "FRAME RATE",
                "FRAME TIME",
                "ZULU TIME",
                "LOCAL TIME",
                "CRASH FLAG",
                "CRASH SEQUENCE",
                "ATC ID",
                "ATC AIRLINE",
                "ATC FLIGHT NUMBER",
                "ATC MODEL",
                "ATC TYPE",
                "ATC HEAVY",
                "TRANSPONDER CODE",
                "TRANSPONDER STATE",
                "TRANSPONDER AVAILABLE",
                "COM ACTIVE FREQUENCY:1",
                "COM STANDBY FREQUENCY:1",
                "COM TRANSMIT:1",
                "COM ACTIVE FREQUENCY:2",
                "COM STANDBY FREQUENCY:2",
                "COM TRANSMIT:2",
                "COM RECEIVE ALL",
                "MARKER BEACON STATE",
                "DME SIGNAL AVAILABLE",
                "ADF SIGNAL",
                "ADF EXT ANT SWITCH",
                "ADF LOOP ANT SWITCH",
                "ADF VOLUME",
                "ADF IDENT SWITCH",
                "PANEL BRIGHTNESS CONTROL",
                "PANEL ANNUNCIATOR TEST",
                "PANEL AUTO BRIGHTNESS",
                "OXYGEN PRESSURE",
                "OXYGEN FLOW RATE",
                "OXYGEN SUPPLY REMAINING",
                "FIRE BOTTLE DISCHARGED:1",
                "FIRE BOTTLE DISCHARGED:2",
                "FIRE WARNING",
                "ENGINE FIRE WARNING",
                "AP MASTER STATUS",
                "FLIGHT DIRECTOR ACTIVE",
                "FLIGHT DIRECTOR PITCH",
                "FLIGHT DIRECTOR BANK",
                "YAW DAMPER AVAILABLE",
                "STANDBY VACUUM",
                "GYRO DRIFT ERROR",
                "GYRO DRIFT CORRECTION",
                "TURN COORDINATOR BALL",
                "TACHOMETER",
                "PROP RPM",
                "PROP SYNC ACTIVE",
                "PROP FEATHERED",
                "PROP BETA",
                "ICE DETECTED",
                "WINDSHIELD DEICE SWITCH",
                "ENGINE REVERSE THRUST",
                "ENGINE REVERSE THRUST DEPLOYED",
                "THROTTLE LOWER LIMIT",
                "THROTTLE UPPER LIMIT",
                "ENGINE FUEL FLOW",
                "ENGINE FUEL VALVE",
                "ENGINE FUEL PUMP",
                "ENGINE BOOST PUMP",
                "ENGINE PRIMER",
                "ENGINE MAGNETO POSITION",
                "ENGINE IGNITION SWITCH",
                "ENGINE OIL PRESSURE",
                "ENGINE OIL TEMPERATURE",
                "ENGINE OIL QUANTITY",
                "ENGINE TORQUE",
                "ENGINE TURBINE TEMPERATURE",
                "ENGINE TURBINE MAX TEMP",
                "ENGINE TURBINE CORRECTED N1",
                "ENGINE TURBINE CORRECTED N2",
                "ENGINE TURBINE PRESSURE RATIO",
                "ENGINE TURBINE AVAILABLE",
                "ENGINE TURBINE AFTERBURNER",
                "ENGINE TURBINE IGNITION SWITCH",
                "ENGINE COOLANT TEMPERATURE",
                "ENGINE COOLANT RESERVOIR",
                "ENGINE VACUUM",
                "ENGINE CARBURETOR TEMPERATURE",
                "ENGINE CARBURETOR ICE PCT",
                "ENGINE PRIMER LOCKED",
                "ENGINE BLEED AIR",
                "ENGINE GENERATOR ACTIVE",
                "ENGINE GENERATOR VOLTAGE",
                "ENGINE GENERATOR AMPS",
                "ENGINE GENERATOR LOAD",
                "ENGINE INLET ANTI ICE",
                "ENGINE BLEED AIR PRESSURE",
                "ENGINE COOLING FLAPS",
                "ENGINE COWL FLAPS",
                "ENGINE ANTI ICE VALVE",
                "ENGINE INTERCOOLER FLAP",
                "ENGINE TURBINE N1",
                "ENGINE TURBINE N2",
                "ENGINE COMPRESSOR STALL",
                "ENGINE AUGMENTER",
                "ENGINE FADEC ACTIVE",
                "THROTTLE LEVER DETENT",
                "THROTTLE LEVER POSITION",
                "MIXTURE LEVER POSITION",
                "PROP LEVER POSITION",
                "PROP FEATHER SWITCH",
                "PROP SYNC SWITCH",
                "PROP DEICE SWITCH",
                "IGNITION SWITCH SET",
                "MAGNETO SWITCH SET",
                "MIXTURE RATIO SET",
                "THROTTLE LOWER LIMIT EX1",
                "THROTTLE UPPER LIMIT EX1",
                "SIM DISABLED",
                "PILOT TRANSLATION X",
                "PILOT TRANSLATION Y",
                "PILOT TRANSLATION Z",
                "PILOT ROTATION X",
                "PILOT ROTATION Y",
                "PILOT ROTATION Z",
                "VR HEADSET ACTIVE",
                "VR CAMERA AVAILABLE",
                "CAMERA STATE",
                "CAMERA SUBSTATE",
                "CAMERA RECORDING",
                "VIEW CAMERA LOCATION",
                "VIEW CAMERA ORIENTATION",
                "SIMDIS CONNECTED",
                "SIMDIS STREAMING",
                "SIMDIS LATENCY",
            });

            AddIndexed(new[]
            {
                "GENERAL ENG RPM",
                "GENERAL ENG THROTTLE LEVER POSITION",
                "GENERAL ENG MIXTURE LEVER POSITION",
                "GENERAL ENG PROP LEVER POSITION",
                "GENERAL ENG OIL TEMPERATURE",
                "GENERAL ENG OIL PRESSURE",
                "GENERAL ENG MANIFOLD PRESSURE",
                "GENERAL ENG FUEL FLOW",
                "GENERAL ENG FUEL PRESSURE",
                "GENERAL ENG COMBUSTION",
                "GENERAL ENG STARTER",
                "GENERAL ENG N1",
                "GENERAL ENG N2",
                "GENERAL ENG ELAPSED TIME",
                "ENG COMBUSTION",
                "ENG FUEL FLOW",
                "ENG FUEL PRESSURE",
                "ENG FUEL VALVE",
                "ENG FUEL TEMPERATURE",
                "ENG FUEL FLOW PPH",
                "ENG FUEL USED SINCE START",
                "ENG FUEL PUMP SWITCH",
                "ENG FUEL PUMP ON",
                "ENG OIL PRESSURE",
                "ENG OIL TEMPERATURE",
                "ENG OIL QUANTITY",
                "ENG OIL CONSUMPTION",
                "ENG OIL LEAK",
                "ENG CYLINDER HEAD TEMPERATURE",
                "ENG TURBINE TEMPERATURE",
                "ENG TURBINE CORRECTED N1",
                "ENG TURBINE CORRECTED N2",
                "ENG TURBINE PRESSURE RATIO",
                "ENG TURBINE MAX TORQUE",
                "ENG TORQUE",
                "ENG HORSEPOWER",
                "ENG MECHANICAL POWER",
                "ENG N1 RPM",
                "ENG N2 RPM",
                "ENG BLEED AIR",
                "ENG BLEED AIR PRESSURE",
                "ENG BLEED AIR VALVE",
                "ENG BLEED AIR SOURCE",
                "ENG ANTI ICE",
                "ENG INLET ICE",
                "ENG INLET ANTI ICE",
                "ENG ANTI ICE SWITCH",
                "ENG COWL FLAP POSITION",
                "ENG INTERCOOLER FLAP POSITION",
                "ENG COOLANT LEVEL",
                "ENG COOLANT TEMPERATURE",
                "ENG COOLANT PRESSURE",
                "ENG COOLANT VALVE",
                "ENG ELECTRICAL LOAD",
                "ENG GENERATOR SWITCH",
                "ENG GENERATOR ACTIVE",
                "ENG GENERATOR AMPS",
                "ENG GENERATOR VOLTAGE",
                "ENG GENERATOR LOAD",
                "ENG ALTERNATOR SWITCH",
                "ENG ALTERNATOR ACTIVE",
                "ENG ALTERNATOR AMPS",
                "ENG ALTERNATOR VOLTAGE",
                "ENG FIRE DETECTED",
                "ENG FIRE BOTTLE SWITCH",
                "ENG FIRE BOTTLE DISCHARGED",
                "ENG MAGNETO SWITCH",
                "ENG IGNITION SWITCH",
                "ENG IGNITION MODE",
                "ENG BOOST PRESSURE",
                "ENG TURBO PRESSURE",
                "ENG TORQUE PERCENT",
                "ENG POWER RATIO",
                "ENG EGT",
                "ENG ITT",
                "ENG TIT",
                "ENG FUEL USED SINCE TAKEOFF",
                "ENG FUEL USED SINCE LAST REFUEL",
                "ENG VIBRATION",
                "ENG STARTER OUTPUT",
                "ENG STARTER TORQUE",
                "ENG STARTER ACTIVE",
                "ENG PRIMER",
                "ENG PRIMER SWITCH",
                "ENG PRIMER PUMP",
                "ENG AIR START SWITCH",
                "ENG IGNITION ON",
                "ENG AUTO IGNITION",
                "ENG STARTER PROPELLER RPM",
                "ENG EXHAUST GAS TEMPERATURE",
                "ENG EXHAUST GAS TEMPERATURE LIMIT",
                "ENG EXHAUST GAS TEMPERATURE NORMAL",
                "ENG FUEL MIXTURE RATIO",
                "ENG FUEL VALVE SWITCH",
                "ENG BLEED AIR SOURCE SWITCH",
                "ENG THROTTLE CONTROL",
                "ENG REVERSE THRUST",
                "ENG REVERSE ACTIVE",
                "ENG REVERSE THRUST DEPLOYED",
                "ENG PROPELLER BETA",
                "ENG PROPELLER FEATHERED",
                "ENG PROPELLER FEATHER SWITCH",
                "ENG PROPELLER GOVERNOR ACTIVE",
                "ENG PROPELLER GOVERNOR MODE",
                "ENG PROP SYNC ACTIVE",
                "ENG PROP SYNC SWITCH",
                "ENG PROP SYNC LOCK",
            }, 1, 8);

            AddIndexed(new[]
            {
                "COM ACTIVE FREQUENCY",
                "COM STANDBY FREQUENCY",
                "COM TRANSMIT",
                "NAV ACTIVE FREQUENCY",
                "NAV STANDBY FREQUENCY",
                "NAV HAS GLIDE SLOPE",
                "NAV HAS LOCALIZER",
                "NAV MAGVAR",
                "NAV DME",
                "NAV GLIDE SLOPE NEEDLE",
                "NAV LOCALIZER NEEDLE",
                "NAV SIGNAL",
                "ADF ACTIVE FREQUENCY",
                "ADF STANDBY FREQUENCY",
                "ADF SIGNAL",
                "ADF VOLUME",
                "ADF IDENT SWITCH",
            }, 1, 4);

            AddIndexed(new[]
            {
                "PRESSURIZATION CABIN ALTITUDE",
                "PRESSURIZATION DIFFERENTIAL PRESSURE",
                "PRESSURIZATION DUMP SWITCH",
                "PRESSURIZATION SAFETY VALVE",
                "PRESSURIZATION MANUAL VALVE",
                "PRESSURIZATION AUTO FLOW",
                "PRESSURIZATION SYSTEM FAULT",
            }, 1, 2);

            AddIndexed(new[]
            {
                "ANTI ICE SWITCH",
                "DEICE BOOT SWITCH",
                "WINDSHIELD DEICE SWITCH",
                "PROP DEICE SWITCH",
                "WING LIGHT SWITCH",
                "LOGO LIGHT SWITCH",
                "TAXI LIGHT SWITCH",
                "LANDING LIGHT SWITCH",
            }, 1, 4);

            if (SimVarCatalogGenerator.TryGetCatalog(out var catalog))
            {
                foreach (var descriptor in catalog.SimVars)
                {
                    var key = descriptor.Index.HasValue
                        ? $"{descriptor.Name}:{descriptor.Index.Value}"
                        : descriptor.Name;
                    if (!string.IsNullOrWhiteSpace(key))
                        keys.Add(key);
                }
            }

            var ordered = keys.ToList();
            ordered.Sort(StringComparer.OrdinalIgnoreCase);
            return ordered;
        }
    }
}
