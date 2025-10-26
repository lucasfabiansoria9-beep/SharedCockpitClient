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
        public double Spoilers;
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

    // ──────────────────────────────
    // ✈️ SNAPSHOT PRINCIPAL DE ESTADO
    // ──────────────────────────────
    public class SimStateSnapshot
    {
        public AttitudeStruct Attitude;
        public PositionStruct Position;
        public SpeedStruct Speed;
        public ControlsStruct Controls;
        public CabinStruct Cabin;
        public SystemsStruct Systems;
        public DoorsStruct Doors;
        public GroundSupportStruct Ground;
        public EnvironmentStruct Environment;
        public AvionicsStruct Avionics;

        public SimStateSnapshot Clone() => (SimStateSnapshot)MemberwiseClone();

        public Dictionary<string, object?> ToDictionary()
        {
            return new Dictionary<string, object?>
            {
                ["controls"] = new Dictionary<string, object?>
                {
                    ["throttle"] = Controls.Throttle,
                    ["flaps"] = Controls.Flaps,
                    ["elevator"] = Controls.Elevator,
                    ["aileron"] = Controls.Aileron,
                    ["rudder"] = Controls.Rudder,
                    ["parkingBrake"] = Controls.ParkingBrake,
                    ["spoilers"] = Controls.Spoilers
                },
                ["cabin"] = new Dictionary<string, object?>
                {
                    ["landingGearDown"] = Cabin.LandingGearDown,
                    ["spoilersDeployed"] = Cabin.SpoilersDeployed,
                    ["autopilotOn"] = Cabin.AutopilotOn,
                    ["autopilotAltitude"] = Cabin.AutopilotAltitude,
                    ["autopilotHeading"] = Cabin.AutopilotHeading
                }
            };
        }

        public bool TryApplyChange(string key, object? value)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var parts = key.Split('.', 2);
            if (parts.Length != 2) return false;

            switch (parts[0])
            {
                case "controls": return ApplyControls(parts[1], value);
                case "cabin": return ApplyCabin(parts[1], value);
                default: return false;
            }
        }

        private bool ApplyControls(string field, object? value)
        {
            if (!TryGetDouble(value, out var n)) return false;
            switch (field)
            {
                case "throttle": Controls.Throttle = n; break;
                case "flaps": Controls.Flaps = n; break;
                case "elevator": Controls.Elevator = n; break;
                case "aileron": Controls.Aileron = n; break;
                case "rudder": Controls.Rudder = n; break;
                case "parkingBrake": Controls.ParkingBrake = n; break;
                case "spoilers": Controls.Spoilers = n; break;
            }
            return true;
        }

        private bool ApplyCabin(string field, object? value)
        {
            if (!TryGetDouble(value, out var n)) return false;
            switch (field)
            {
                case "spoilersDeployed": Cabin.SpoilersDeployed = n; break;
                case "autopilotAltitude": Cabin.AutopilotAltitude = n; break;
                case "autopilotHeading": Cabin.AutopilotHeading = n; break;
            }
            return true;
        }

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
