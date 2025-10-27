using System;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Estructuras base del estado del simulador: controles, sistemas, cabina y entorno.
    /// </summary>

    // -------------------------------
    // CONTROLES DE VUELO Y SUPERFICIES
    // -------------------------------
    public struct ControlsStruct
    {
        public double Throttle;        // 0.0 – 1.0
        public double Flaps;           // 0 – 40 grados
        public bool GearDown;          // tren extendido
        public bool ParkingBrake;      // freno de parqueo
        public double Elevator;        // eje de profundidad
        public double Aileron;         // alabeo
        public double Rudder;          // guiñada
        public double Spoilers;        // spoilers extendidos 0–1
    }

    // -------------------------------
    // SISTEMAS ELÉCTRICOS / AVIÓNICOS
    // -------------------------------
    public struct SystemsStruct
    {
        public bool LightsOn;          // luces generales
        public bool DoorOpen;          // puerta abierta
        public bool AvionicsOn;        // aviónica energizada
    }

    // -------------------------------
    // CABINA Y SISTEMAS INTERNOS
    // -------------------------------
    public struct CabinStruct
    {
        public bool LandingGearDown;
        public double SpoilersDeployed;
        public bool AutopilotOn;
        public double AutopilotAltitude;
        public double AutopilotHeading;
    }

    // -------------------------------
    // ENTORNO / ATMÓSFERA
    // -------------------------------
    public struct EnvironmentStruct
    {
        public double AmbientTemperature;
        public double BarometricPressure;
        public double WindVelocity;
        public double WindDirection;
    }

    // -------------------------------
    // SNAPSHOT GLOBAL
    // -------------------------------
    public partial class SimStateSnapshot
    {
        public ControlsStruct Controls { get; set; }
        public SystemsStruct Systems { get; set; }
        public CabinStruct Cabin { get; set; }
        public EnvironmentStruct Environment { get; set; }

        public SimStateSnapshot Clone()
        {
            return new SimStateSnapshot
            {
                Controls = Controls,
                Systems = Systems,
                Cabin = Cabin,
                Environment = Environment
            };
        }
    }
}
