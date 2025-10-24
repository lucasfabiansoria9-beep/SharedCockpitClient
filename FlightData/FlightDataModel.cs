using System;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient.FlightData;

public readonly struct AttitudeStruct
{
    public readonly double Pitch;
    public readonly double Bank;
    public readonly double Heading;

    public AttitudeStruct(double pitch = 0, double bank = 0, double heading = 0)
    {
        Pitch = pitch;
        Bank = bank;
        Heading = heading;
    }
}

public readonly struct PositionStruct
{
    public readonly double Latitude;
    public readonly double Longitude;
    public readonly double Altitude;

    public PositionStruct(double lat = 0, double lon = 0, double alt = 0)
    {
        Latitude = lat;
        Longitude = lon;
        Altitude = alt;
    }
}

public readonly struct SpeedStruct
{
    public readonly double IndicatedAirspeed;
    public readonly double VerticalSpeed;
    public readonly double GroundSpeed;

    public SpeedStruct(double ias = 0, double vs = 0, double gs = 0)
    {
        IndicatedAirspeed = ias;
        VerticalSpeed = vs;
        GroundSpeed = gs;
    }
}

public readonly struct ControlsStruct
{
    public readonly double Throttle;
    public readonly double Flaps;
    public readonly double Elevator;
    public readonly double Aileron;
    public readonly double Rudder;
    public readonly double ParkingBrake;

    public ControlsStruct(double throttle = 0, double flaps = 0, double elevator = 0,
                          double aileron = 0, double rudder = 0, double parkingBrake = 0)
    {
        Throttle = throttle;
        Flaps = flaps;
        Elevator = elevator;
        Aileron = aileron;
        Rudder = rudder;
        ParkingBrake = parkingBrake;
    }
}

public readonly struct CabinStruct
{
    public readonly bool LandingGearDown;
    public readonly bool SpoilersDeployed;
    public readonly bool AutopilotOn;
    public readonly double AutopilotAltitude;
    public readonly double AutopilotHeading;

    public CabinStruct(bool gear = false, bool spoilers = false, bool autopilot = false,
                       double alt = 0, double heading = 0)
    {
        LandingGearDown = gear;
        SpoilersDeployed = spoilers;
        AutopilotOn = autopilot;
        AutopilotAltitude = alt;
        AutopilotHeading = heading;
    }
}

public readonly struct DoorsStruct
{
    public readonly bool DoorLeftOpen;
    public readonly bool DoorRightOpen;
    public readonly bool CargoDoorOpen;
    public readonly bool RampOpen;

    public DoorsStruct(bool left = false, bool right = false, bool cargo = false, bool ramp = false)
    {
        DoorLeftOpen = left;
        DoorRightOpen = right;
        CargoDoorOpen = cargo;
        RampOpen = ramp;
    }
}

public readonly struct DoorsRawStruct
{
    public readonly double Exit0;
    public readonly double Exit1;
    public readonly double Exit2;
    public readonly double Exit3;

    public DoorsRawStruct(double exit0 = 0, double exit1 = 0, double exit2 = 0, double exit3 = 0)
    {
        Exit0 = exit0;
        Exit1 = exit1;
        Exit2 = exit2;
        Exit3 = exit3;
    }
}

public readonly struct GroundSupportStruct
{
    public readonly bool CateringTruckPresent;
    public readonly bool BaggageCartsPresent;
    public readonly bool FuelTruckPresent;

    public GroundSupportStruct(bool catering = false, bool baggage = false, bool fuel = false)
    {
        CateringTruckPresent = catering;
        BaggageCartsPresent = baggage;
        FuelTruckPresent = fuel;
    }
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
