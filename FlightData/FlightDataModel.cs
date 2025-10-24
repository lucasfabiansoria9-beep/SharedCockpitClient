using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData
{
    // ──────────────────────────────
    // 📦 ESTRUCTURAS DE DATOS BASE
    // ──────────────────────────────
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AttitudeStruct
    {
        public double Pitch;
        public double Bank;
        public double Heading;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct PositionStruct
    {
        public double Latitude;
        public double Longitude;
        public double Altitude;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SpeedStruct
    {
        public double IndicatedAirspeed;
        public double VerticalSpeed;
        public double GroundSpeed;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ControlsStruct
    {
        public double Throttle;
        public double Flaps;
        public double Elevator;
        public double Aileron;
        public double Rudder;
        public double ParkingBrake;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CabinStruct
    {
        public int LandingGearDown;
        public double SpoilersDeployed;
        public int AutopilotOn;
        public double AutopilotAltitude;
        public double AutopilotHeading;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct SystemsStruct
    {
        public int LandingLight;
        public int BeaconLight;
        public int NavLight;
        public int StrobeLight;
        public int TaxiLight;
        public int BatteryMaster;
        public int Alternator;
        public int AvionicsMaster;
        public int FuelPump;
        public int PitotHeat;
        public int AntiIce;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DoorsStruct
    {
        public int DoorLeftOpen;
        public int DoorRightOpen;
        public int CargoDoorOpen;
        public int RampOpen;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct DoorsRawStruct
    {
        public double Exit0;
        public double Exit1;
        public double Exit2;
        public double Exit3;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GroundSupportStruct
    {
        public int CateringTruckPresent;
        public int BaggageCartsPresent;
        public int FuelTruckPresent;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EnvironmentStruct
    {
        public double AmbientTemperature;
        public double TotalAirTemperature;
        public double BarometricPressure;
        public double WindVelocity;
        public double WindDirection;
        public double PrecipitationRate;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct AvionicsStruct
    {
        public double Com1Active;
        public double Com1Standby;
        public double Nav1Active;
        public double Nav1Standby;
        public double TransponderCode;
        public double AutopilotNavLock;
    }

    public readonly struct SimVarDefinition
    {
        public readonly string Variable;
        public readonly string Units;
        public readonly SIMCONNECT_DATATYPE DataType;
        public readonly float Epsilon;

        public SimVarDefinition(string variable, string units, SIMCONNECT_DATATYPE dataType, float epsilon = 0f)
        {
            Variable = variable;
            Units = units;
            DataType = dataType;
            Epsilon = epsilon;
        }
    }

    // ──────────────────────────────
    // ✈️ CLASE PRINCIPAL DE ESTADO
    // ──────────────────────────────
    public sealed class SimStateSnapshot
    {
        public AttitudeStruct Attitude { get; set; }
        public PositionStruct Position { get; set; }
        public SpeedStruct Speed { get; set; }
        public ControlsStruct Controls { get; set; }
        public CabinStruct Cabin { get; set; }
        public SystemsStruct Systems { get; set; }
        public DoorsStruct Doors { get; set; }
        public GroundSupportStruct Ground { get; set; }
        public EnvironmentStruct Environment { get; set; }
        public AvionicsStruct Avionics { get; set; }

        public SimStateSnapshot Clone() => new()
        {
            Attitude = Attitude,
            Position = Position,
            Speed = Speed,
            Controls = Controls,
            Cabin = Cabin,
            Systems = Systems,
            Doors = Doors,
            Ground = Ground,
            Environment = Environment,
            Avionics = Avionics
        };

        // 🔒 Métodos seguros de actualización
        public void UpdateControls(Action<ControlsStruct> update)
        {
            var tmp = Controls; update(tmp); Controls = tmp;
        }
        public void UpdateSystems(Action<SystemsStruct> update)
        {
            var tmp = Systems; update(tmp); Systems = tmp;
        }
        public void UpdateCabin(Action<CabinStruct> update)
        {
            var tmp = Cabin; update(tmp); Cabin = tmp;
        }
        public void UpdateEnvironment(Action<EnvironmentStruct> update)
        {
            var tmp = Environment; update(tmp); Environment = tmp;
        }
        public void UpdateAvionics(Action<AvionicsStruct> update)
        {
            var tmp = Avionics; update(tmp); Avionics = tmp;
        }
        public void UpdateDoors(Action<DoorsStruct> update)
        {
            var tmp = Doors; update(tmp); Doors = tmp;
        }
        public void UpdateGround(Action<GroundSupportStruct> update)
        {
            var tmp = Ground; update(tmp); Ground = tmp;
        }

        // 🔁 Convierte el estado a un diccionario (para red)
        public Dictionary<string, object?> ToDictionary()
        {
            return new Dictionary<string, object?>
            {
                ["attitude"] = new Dictionary<string, object?>
                {
                    ["pitch"] = Attitude.Pitch,
                    ["bank"] = Attitude.Bank,
                    ["heading"] = Attitude.Heading
                },
                ["position"] = new Dictionary<string, object?>
                {
                    ["latitude"] = Position.Latitude,
                    ["longitude"] = Position.Longitude,
                    ["altitude"] = Position.Altitude
                },
                ["speed"] = new Dictionary<string, object?>
                {
                    ["indicatedAirspeed"] = Speed.IndicatedAirspeed,
                    ["verticalSpeed"] = Speed.VerticalSpeed,
                    ["groundSpeed"] = Speed.GroundSpeed
                },
                ["controls"] = new Dictionary<string, object?>
                {
                    ["throttleLever"] = Controls.Throttle,
                    ["flapsHandlePercent"] = Controls.Flaps,
                    ["elevator"] = Controls.Elevator,
                    ["aileron"] = Controls.Aileron,
                    ["rudder"] = Controls.Rudder,
                    ["parkingBrake"] = Controls.ParkingBrake
                },
                ["cabin"] = new Dictionary<string, object?>
                {
                    ["landingGearDown"] = Cabin.LandingGearDown != 0,
                    ["spoilersHandle"] = Cabin.SpoilersDeployed,
                    ["autopilotMaster"] = Cabin.AutopilotOn != 0,
                    ["autopilotAltitude"] = Cabin.AutopilotAltitude,
                    ["autopilotHeading"] = Cabin.AutopilotHeading
                },
                ["systems"] = new Dictionary<string, object?>
                {
                    ["landingLight"] = Systems.LandingLight != 0,
                    ["beaconLight"] = Systems.BeaconLight != 0,
                    ["navLight"] = Systems.NavLight != 0,
                    ["strobeLight"] = Systems.StrobeLight != 0,
                    ["taxiLight"] = Systems.TaxiLight != 0,
                    ["batteryMaster"] = Systems.BatteryMaster != 0,
                    ["alternator"] = Systems.Alternator != 0,
                    ["avionicsMaster"] = Systems.AvionicsMaster != 0,
                    ["fuelPump"] = Systems.FuelPump != 0,
                    ["pitotHeat"] = Systems.PitotHeat != 0,
                    ["antiIce"] = Systems.AntiIce != 0
                },
                ["doors"] = new Dictionary<string, object?>
                {
                    ["main"] = Doors.DoorLeftOpen != 0,
                    ["service"] = Doors.DoorRightOpen != 0,
                    ["cargo"] = Doors.CargoDoorOpen != 0,
                    ["ramp"] = Doors.RampOpen != 0
                },
                ["ground"] = new Dictionary<string, object?>
                {
                    ["cateringTruck"] = Ground.CateringTruckPresent != 0,
                    ["baggageCarts"] = Ground.BaggageCartsPresent != 0,
                    ["fuelTruck"] = Ground.FuelTruckPresent != 0
                },
                ["environment"] = new Dictionary<string, object?>
                {
                    ["ambientTemperatureC"] = Environment.AmbientTemperature,
                    ["totalAirTemperatureC"] = Environment.TotalAirTemperature,
                    ["barometricPressureInHg"] = Environment.BarometricPressure,
                    ["windSpeedKnots"] = Environment.WindVelocity,
                    ["windDirectionDegrees"] = Environment.WindDirection,
                    ["precipitationRate"] = Environment.PrecipitationRate
                },
                ["avionics"] = new Dictionary<string, object?>
                {
                    ["com1Active"] = Avionics.Com1Active,
                    ["com1Standby"] = Avionics.Com1Standby,
                    ["nav1Active"] = Avionics.Nav1Active,
                    ["nav1Standby"] = Avionics.Nav1Standby,
                    ["transponderCode"] = Avionics.TransponderCode,
                    ["autopilotNavHold"] = Avionics.AutopilotNavLock != 0
                }
            };
        }

        // 🔧 Aplica un cambio remoto (desde red/copiloto)
        public bool TryApplyChange(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var parts = key.Split('.', 2);
            if (parts.Length != 2) return false;

            return parts[0] switch
            {
                "controls" => ApplyControls(parts[1], value),
                "systems" => ApplySystems(parts[1], value),
                "cabin" => ApplyCabin(parts[1], value),
                "environment" => ApplyEnvironment(parts[1], value),
                "avionics" => ApplyAvionics(parts[1], value),
                "doors" => ApplyDoors(parts[1], value),
                "ground" => ApplyGround(parts[1], value),
                _ => false
            };
        }

        // ✅ Aplicadores de cambios con actualización segura
        private bool ApplyControls(string field, object? value)
        {
            if (!TryGetDouble(value, out var number)) return false;
            UpdateControls(c =>
            {
                switch (field)
                {
                    case "throttleLever": c.Throttle = number; break;
                    case "flapsHandlePercent": c.Flaps = number; break;
                    case "elevator": c.Elevator = number; break;
                    case "aileron": c.Aileron = number; break;
                    case "rudder": c.Rudder = number; break;
                    case "parkingBrake": c.ParkingBrake = number; break;
                }
            });
            return true;
        }

        private bool ApplySystems(string field, object? value)
        {
            if (!TryGetBool(value, out var b)) return false;
            UpdateSystems(s =>
            {
                switch (field)
                {
                    case "landingLight": s.LandingLight = b ? 1 : 0; break;
                    case "beaconLight": s.BeaconLight = b ? 1 : 0; break;
                    case "navLight": s.NavLight = b ? 1 : 0; break;
                    case "strobeLight": s.StrobeLight = b ? 1 : 0; break;
                    case "taxiLight": s.TaxiLight = b ? 1 : 0; break;
                    case "batteryMaster": s.BatteryMaster = b ? 1 : 0; break;
                    case "alternator": s.Alternator = b ? 1 : 0; break;
                    case "avionicsMaster": s.AvionicsMaster = b ? 1 : 0; break;
                    case "fuelPump": s.FuelPump = b ? 1 : 0; break;
                    case "pitotHeat": s.PitotHeat = b ? 1 : 0; break;
                    case "antiIce": s.AntiIce = b ? 1 : 0; break;
                }
            });
            return true;
        }

        private bool ApplyCabin(string field, object? value)
        {
            UpdateCabin(c =>
            {
                switch (field)
                {
                    case "landingGearDown": if (TryGetBool(value, out var gear)) c.LandingGearDown = gear ? 1 : 0; break;
                    case "spoilersHandle": if (TryGetDouble(value, out var spoilers)) c.SpoilersDeployed = spoilers; break;
                    case "autopilotMaster": if (TryGetBool(value, out var ap)) c.AutopilotOn = ap ? 1 : 0; break;
                    case "autopilotAltitude": if (TryGetDouble(value, out var alt)) c.AutopilotAltitude = alt; break;
                    case "autopilotHeading": if (TryGetDouble(value, out var hdg)) c.AutopilotHeading = hdg; break;
                }
            });
            return true;
        }

        private bool ApplyEnvironment(string field, object? value)
        {
            if (!TryGetDouble(value, out var n)) return false;
            UpdateEnvironment(e =>
            {
                switch (field)
                {
                    case "ambientTemperatureC": e.AmbientTemperature = n; break;
                    case "totalAirTemperatureC": e.TotalAirTemperature = n; break;
                    case "barometricPressureInHg": e.BarometricPressure = n; break;
                    case "windSpeedKnots": e.WindVelocity = n; break;
                    case "windDirectionDegrees": e.WindDirection = n; break;
                    case "precipitationRate": e.PrecipitationRate = n; break;
                }
            });
            return true;
        }

        private bool ApplyAvionics(string field, object? value)
        {
            UpdateAvionics(a =>
            {
                switch (field)
                {
                    case "com1Active": if (TryGetDouble(value, out var c1a)) a.Com1Active = c1a; break;
                    case "com1Standby": if (TryGetDouble(value, out var c1s)) a.Com1Standby = c1s; break;
                    case "nav1Active": if (TryGetDouble(value, out var n1a)) a.Nav1Active = n1a; break;
                    case "nav1Standby": if (TryGetDouble(value, out var n1s)) a.Nav1Standby = n1s; break;
                    case "transponderCode": if (TryGetDouble(value, out var t)) a.TransponderCode = t; break;
                    case "autopilotNavHold": if (TryGetBool(value, out var nav)) a.AutopilotNavLock = nav ? 1 : 0; break;
                }
            });
            return true;
        }

        private bool ApplyDoors(string field, object? value)
        {
            if (!TryGetBool(value, out var b)) return false;
            UpdateDoors(d =>
            {
                switch (field)
                {
                    case "main": d.DoorLeftOpen = b ? 1 : 0; break;
                    case "service": d.DoorRightOpen = b ? 1 : 0; break;
                    case "cargo": d.CargoDoorOpen = b ? 1 : 0; break;
                    case "ramp": d.RampOpen = b ? 1 : 0; break;
                }
            });
            return true;
        }

        private bool ApplyGround(string field, object? value)
        {
            if (!TryGetBool(value, out var b)) return false;
            UpdateGround(g =>
            {
                switch (field)
                {
                    case "cateringTruck": g.CateringTruckPresent = b ? 1 : 0; break;
                    case "baggageCarts": g.BaggageCartsPresent = b ? 1 : 0; break;
                    case "fuelTruck": g.FuelTruckPresent = b ? 1 : 0; break;
                }
            });
            return true;
        }

        // 🧮 Conversión de tipos
        private static bool TryGetDouble(object? value, out double number)
        {
            switch (value)
            {
                case null: number = 0; return false;
                case double d: number = d; return true;
                case float f: number = f; return true;
                case int i: number = i; return true;
                case long l: number = l; return true;
                case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed): number = parsed; return true;
                default: number = 0; return false;
            }
        }

        private static bool TryGetBool(object? value, out bool boolean)
        {
            switch (value)
            {
                case null: boolean = false; return false;
                case bool b: boolean = b; return true;
                case int i: boolean = i != 0; return true;
                case long l: boolean = l != 0; return true;
                case double d: boolean = Math.Abs(d) > double.Epsilon; return true;
                case string s when bool.TryParse(s, out var parsedBool): boolean = parsedBool; return true;
                case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt): boolean = parsedInt != 0; return true;
                default: boolean = false; return false;
            }
        }
    }

    // ──────────────────────────────
    // ENUM DE DEFINICIONES SIMCONNECT
    // ──────────────────────────────
    public enum SimDataDefinition
    {
        Attitude,
        Position,
        Speed,
        Controls,
        Cabin,
        Systems,
        Doors,
        Ground,
        Environment,
        Avionics
    }
}
