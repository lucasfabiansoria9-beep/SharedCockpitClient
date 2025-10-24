using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData;

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

    public bool TryApplyChange(string key, object? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var segments = key.Split('.', 2);
        if (segments.Length != 2)
        {
            return false;
        }

        var group = segments[0];
        var field = segments[1];

        switch (group)
        {
            case "controls":
                return ApplyControls(field, value);
            case "systems":
                return ApplySystems(field, value);
            case "cabin":
                return ApplyCabin(field, value);
            case "environment":
                return ApplyEnvironment(field, value);
            case "avionics":
                return ApplyAvionics(field, value);
            case "doors":
                return ApplyDoors(field, value);
            case "ground":
                return ApplyGround(field, value);
            default:
                return false;
        }
    }

    public object GetStructForDefinition(SimDataDefinition definition) => definition switch
    {
        SimDataDefinition.Attitude => Attitude,
        SimDataDefinition.Position => Position,
        SimDataDefinition.Speed => Speed,
        SimDataDefinition.Controls => Controls,
        SimDataDefinition.Cabin => Cabin,
        SimDataDefinition.Systems => Systems,
        SimDataDefinition.Doors => Doors,
        SimDataDefinition.Ground => Ground,
        SimDataDefinition.Environment => Environment,
        SimDataDefinition.Avionics => Avionics,
        _ => throw new ArgumentOutOfRangeException(nameof(definition), definition, null)
    };

    private bool ApplyControls(string field, object? value)
    {
        if (!TryGetDouble(value, out var number))
        {
            return false;
        }

        switch (field)
        {
            case "throttleLever":
                Controls.Throttle = number;
                return true;
            case "flapsHandlePercent":
                Controls.Flaps = number;
                return true;
            case "elevator":
                Controls.Elevator = number;
                return true;
            case "aileron":
                Controls.Aileron = number;
                return true;
            case "rudder":
                Controls.Rudder = number;
                return true;
            case "parkingBrake":
                Controls.ParkingBrake = number;
                return true;
            default:
                return false;
        }
    }

    private bool ApplySystems(string field, object? value)
    {
        if (!TryGetBool(value, out var boolean))
        {
            return false;
        }

        switch (field)
        {
            case "landingLight":
                Systems.LandingLight = boolean ? 1 : 0;
                return true;
            case "beaconLight":
                Systems.BeaconLight = boolean ? 1 : 0;
                return true;
            case "navLight":
                Systems.NavLight = boolean ? 1 : 0;
                return true;
            case "strobeLight":
                Systems.StrobeLight = boolean ? 1 : 0;
                return true;
            case "taxiLight":
                Systems.TaxiLight = boolean ? 1 : 0;
                return true;
            case "batteryMaster":
                Systems.BatteryMaster = boolean ? 1 : 0;
                return true;
            case "alternator":
                Systems.Alternator = boolean ? 1 : 0;
                return true;
            case "avionicsMaster":
                Systems.AvionicsMaster = boolean ? 1 : 0;
                return true;
            case "fuelPump":
                Systems.FuelPump = boolean ? 1 : 0;
                return true;
            case "pitotHeat":
                Systems.PitotHeat = boolean ? 1 : 0;
                return true;
            case "antiIce":
                Systems.AntiIce = boolean ? 1 : 0;
                return true;
            default:
                return false;
        }
    }

    private bool ApplyCabin(string field, object? value)
    {
        switch (field)
        {
            case "landingGearDown":
                if (!TryGetBool(value, out var gear))
                {
                    return false;
                }
                Cabin.LandingGearDown = gear ? 1 : 0;
                return true;
            case "spoilersHandle":
                if (!TryGetDouble(value, out var spoilers))
                {
                    return false;
                }
                Cabin.SpoilersDeployed = spoilers;
                return true;
            case "autopilotMaster":
                if (!TryGetBool(value, out var ap))
                {
                    return false;
                }
                Cabin.AutopilotOn = ap ? 1 : 0;
                return true;
            case "autopilotAltitude":
                if (!TryGetDouble(value, out var apAlt))
                {
                    return false;
                }
                Cabin.AutopilotAltitude = apAlt;
                return true;
            case "autopilotHeading":
                if (!TryGetDouble(value, out var apHdg))
                {
                    return false;
                }
                Cabin.AutopilotHeading = apHdg;
                return true;
            default:
                return false;
        }
    }

    private bool ApplyEnvironment(string field, object? value)
    {
        if (!TryGetDouble(value, out var number))
        {
            return false;
        }

        switch (field)
        {
            case "ambientTemperatureC":
                Environment.AmbientTemperature = number;
                return true;
            case "totalAirTemperatureC":
                Environment.TotalAirTemperature = number;
                return true;
            case "barometricPressureInHg":
                Environment.BarometricPressure = number;
                return true;
            case "windSpeedKnots":
                Environment.WindVelocity = number;
                return true;
            case "windDirectionDegrees":
                Environment.WindDirection = number;
                return true;
            case "precipitationRate":
                Environment.PrecipitationRate = number;
                return true;
            default:
                return false;
        }
    }

    private bool ApplyAvionics(string field, object? value)
    {
        switch (field)
        {
            case "com1Active":
                return TryAssignDouble(ref Avionics.Com1Active, value);
            case "com1Standby":
                return TryAssignDouble(ref Avionics.Com1Standby, value);
            case "nav1Active":
                return TryAssignDouble(ref Avionics.Nav1Active, value);
            case "nav1Standby":
                return TryAssignDouble(ref Avionics.Nav1Standby, value);
            case "transponderCode":
                return TryAssignDouble(ref Avionics.TransponderCode, value);
            case "autopilotNavHold":
                if (!TryGetBool(value, out var navHold))
                {
                    return false;
                }
                Avionics.AutopilotNavLock = navHold ? 1 : 0;
                return true;
            default:
                return false;
        }
    }

    private bool ApplyDoors(string field, object? value)
    {
        if (!TryGetBool(value, out var boolean))
        {
            return false;
        }

        switch (field)
        {
            case "main":
                Doors.DoorLeftOpen = boolean ? 1 : 0;
                return true;
            case "service":
                Doors.DoorRightOpen = boolean ? 1 : 0;
                return true;
            case "cargo":
                Doors.CargoDoorOpen = boolean ? 1 : 0;
                return true;
            case "ramp":
                Doors.RampOpen = boolean ? 1 : 0;
                return true;
            default:
                return false;
        }
    }

    private bool ApplyGround(string field, object? value)
    {
        if (!TryGetBool(value, out var boolean))
        {
            return false;
        }

        switch (field)
        {
            case "cateringTruck":
                Ground.CateringTruckPresent = boolean ? 1 : 0;
                return true;
            case "baggageCarts":
                Ground.BaggageCartsPresent = boolean ? 1 : 0;
                return true;
            case "fuelTruck":
                Ground.FuelTruckPresent = boolean ? 1 : 0;
                return true;
            default:
                return false;
        }
    }

    private static bool TryGetDouble(object? value, out double number)
    {
        switch (value)
        {
            case null:
                number = 0;
                return false;
            case double d:
                number = d;
                return true;
            case float f:
                number = f;
                return true;
            case int i:
                number = i;
                return true;
            case long l:
                number = l;
                return true;
            case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                number = parsed;
                return true;
            default:
                number = 0;
                return false;
        }
    }

    private static bool TryGetBool(object? value, out bool boolean)
    {
        switch (value)
        {
            case null:
                boolean = false;
                return false;
            case bool b:
                boolean = b;
                return true;
            case int i:
                boolean = i != 0;
                return true;
            case long l:
                boolean = l != 0;
                return true;
            case double d:
                boolean = Math.Abs(d) > double.Epsilon;
                return true;
            case string s when bool.TryParse(s, out var parsedBool):
                boolean = parsedBool;
                return true;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt):
                boolean = parsedInt != 0;
                return true;
            default:
                boolean = false;
                return false;
        }
    }

    private static bool TryAssignDouble(ref double field, object? value)
    {
        if (!TryGetDouble(value, out var number))
        {
            return false;
        }

        field = number;
        return true;
    }
}

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
