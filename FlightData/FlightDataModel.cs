using System;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AttitudeStruct
{
    [JsonPropertyName("Pitch")]
    public double Pitch;

    [JsonPropertyName("Bank")]
    public double Bank;

    [JsonPropertyName("Heading")]
    public double Heading;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PositionStruct
{
    [JsonPropertyName("Latitude")]
    public double Latitude;

    [JsonPropertyName("Longitude")]
    public double Longitude;

    [JsonPropertyName("Altitude")]
    public double Altitude;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SpeedStruct
{
    [JsonPropertyName("IndicatedAirspeed")]
    public double IndicatedAirspeed;

    [JsonPropertyName("VerticalSpeed")]
    public double VerticalSpeed;

    [JsonPropertyName("GroundSpeed")]
    public double GroundSpeed;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ControlsStruct
{
    [JsonPropertyName("Throttle")]
    public double Throttle;

    [JsonPropertyName("Flaps")]
    public double Flaps;

    [JsonPropertyName("Elevator")]
    public double Elevator;

    [JsonPropertyName("Aileron")]
    public double Aileron;

    [JsonPropertyName("Rudder")]
    public double Rudder;

    [JsonPropertyName("ParkingBrake")]
    public double ParkingBrake;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CabinStruct
{
    [JsonPropertyName("LandingGearDown")]
    public bool LandingGearDown;

    [JsonPropertyName("SpoilersDeployed")]
    public bool SpoilersDeployed;

    [JsonPropertyName("AutopilotOn")]
    public bool AutopilotOn;

    [JsonPropertyName("AutopilotAltitude")]
    public double AutopilotAltitude;

    [JsonPropertyName("AutopilotHeading")]
    public double AutopilotHeading;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DoorsStruct
{
    [JsonPropertyName("DoorLeftOpen")]
    public bool DoorLeftOpen;

    [JsonPropertyName("DoorRightOpen")]
    public bool DoorRightOpen;

    [JsonPropertyName("CargoDoorOpen")]
    public bool CargoDoorOpen;

    [JsonPropertyName("RampOpen")]
    public bool RampOpen;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct GroundSupportStruct
{
    [JsonPropertyName("CateringTruckPresent")]
    public bool CateringTruckPresent;

    [JsonPropertyName("BaggageCartsPresent")]
    public bool BaggageCartsPresent;

    [JsonPropertyName("FuelTruckPresent")]
    public bool FuelTruckPresent;
}

public class FlightSnapshot
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = null,
        PropertyNameCaseInsensitive = true,
        IncludeFields = true
    };

    [JsonPropertyName("attitude")]
    public AttitudeStruct Attitude { get; set; }

    [JsonPropertyName("position")]
    public PositionStruct Position { get; set; }

    [JsonPropertyName("speed")]
    public SpeedStruct Speed { get; set; }

    [JsonPropertyName("controls")]
    public ControlsStruct Controls { get; set; }

    [JsonPropertyName("cabin")]
    public CabinStruct Cabin { get; set; }

    [JsonPropertyName("doors")]
    public DoorsStruct Doors { get; set; }

    [JsonPropertyName("ground")]
    public GroundSupportStruct Ground { get; set; }

    [JsonIgnore]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public bool HasPrimaryFlightValues()
    {
        return Math.Abs(Speed.IndicatedAirspeed) > 0.01 ||
               Math.Abs(Speed.VerticalSpeed) > 0.01 ||
               Math.Abs(Speed.GroundSpeed) > 0.01 ||
               Math.Abs(Position.Altitude) > 0.01 ||
               Math.Abs(Position.Latitude) > 0.01 ||
               Math.Abs(Position.Longitude) > 0.01 ||
               Math.Abs(Attitude.Heading) > 0.01;
    }

    public string ToJson() => JsonSerializer.Serialize(this, SerializerOptions);

    public static bool TryFromJson(string payload, out FlightSnapshot? snapshot)
    {
        snapshot = null;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        try
        {
            snapshot = JsonSerializer.Deserialize<FlightSnapshot>(payload, SerializerOptions);
            if (snapshot != null)
            {
                snapshot.Timestamp = DateTime.UtcNow;
            }

            return snapshot != null;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public bool IsMeaningfullyDifferent(FlightSnapshot? other, double tolerance = 0.01)
    {
        if (other == null)
        {
            return true;
        }

        var o = other!;
        return Attitude.Pitch.IsDifferent(o.Attitude.Pitch, tolerance) ||
               Attitude.Bank.IsDifferent(o.Attitude.Bank, tolerance) ||
               Attitude.Heading.IsDifferent(o.Attitude.Heading, tolerance) ||
               Position.Latitude.IsDifferent(o.Position.Latitude, tolerance) ||
               Position.Longitude.IsDifferent(o.Position.Longitude, tolerance) ||
               Position.Altitude.IsDifferent(o.Position.Altitude, tolerance) ||
               Speed.IndicatedAirspeed.IsDifferent(o.Speed.IndicatedAirspeed, tolerance) ||
               Speed.VerticalSpeed.IsDifferent(o.Speed.VerticalSpeed, tolerance) ||
               Speed.GroundSpeed.IsDifferent(o.Speed.GroundSpeed, tolerance) ||
               Controls.Throttle.IsDifferent(o.Controls.Throttle, tolerance) ||
               Controls.Flaps.IsDifferent(o.Controls.Flaps, tolerance) ||
               Controls.Elevator.IsDifferent(o.Controls.Elevator, tolerance) ||
               Controls.Aileron.IsDifferent(o.Controls.Aileron, tolerance) ||
               Controls.Rudder.IsDifferent(o.Controls.Rudder, tolerance) ||
               Controls.ParkingBrake.IsDifferent(o.Controls.ParkingBrake, tolerance) ||
               Cabin.AutopilotAltitude.IsDifferent(o.Cabin.AutopilotAltitude, tolerance) ||
               Cabin.AutopilotHeading.IsDifferent(o.Cabin.AutopilotHeading, tolerance) ||
               Cabin.AutopilotOn != o.Cabin.AutopilotOn ||
               Cabin.LandingGearDown != o.Cabin.LandingGearDown ||
               Cabin.SpoilersDeployed != o.Cabin.SpoilersDeployed ||
               Doors.DoorLeftOpen != o.Doors.DoorLeftOpen ||
               Doors.DoorRightOpen != o.Doors.DoorRightOpen ||
               Doors.CargoDoorOpen != o.Doors.CargoDoorOpen ||
               Doors.RampOpen != o.Doors.RampOpen ||
               Ground.BaggageCartsPresent != o.Ground.BaggageCartsPresent ||
               Ground.CateringTruckPresent != o.Ground.CateringTruckPresent ||
               Ground.FuelTruckPresent != o.Ground.FuelTruckPresent;
    }
}
