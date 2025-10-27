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

        public bool HasMeaningfulDifference(SimStateSnapshot other, double tolerance = 0.01)
        {
            static bool Different(double a, double b, double tol) => Math.Abs(a - b) > tol;

            var ctrl = Controls;
            var otherCtrl = other.Controls;
            if (Different(ctrl.Throttle, otherCtrl.Throttle, tolerance)) return true;
            if (Different(ctrl.Flaps, otherCtrl.Flaps, tolerance)) return true;
            if (Different(ctrl.Elevator, otherCtrl.Elevator, tolerance)) return true;
            if (Different(ctrl.Aileron, otherCtrl.Aileron, tolerance)) return true;
            if (Different(ctrl.Rudder, otherCtrl.Rudder, tolerance)) return true;
            if (Different(ctrl.Spoilers, otherCtrl.Spoilers, tolerance)) return true;
            if (ctrl.GearDown != otherCtrl.GearDown) return true;
            if (ctrl.ParkingBrake != otherCtrl.ParkingBrake) return true;

            var systems = Systems;
            var otherSystems = other.Systems;
            if (systems.LightsOn != otherSystems.LightsOn) return true;
            if (systems.DoorOpen != otherSystems.DoorOpen) return true;
            if (systems.AvionicsOn != otherSystems.AvionicsOn) return true;

            var cabin = Cabin;
            var otherCabin = other.Cabin;
            if (cabin.LandingGearDown != otherCabin.LandingGearDown) return true;
            if (Different(cabin.SpoilersDeployed, otherCabin.SpoilersDeployed, tolerance)) return true;
            if (cabin.AutopilotOn != otherCabin.AutopilotOn) return true;
            if (Different(cabin.AutopilotAltitude, otherCabin.AutopilotAltitude, tolerance)) return true;
            if (Different(cabin.AutopilotHeading, otherCabin.AutopilotHeading, tolerance)) return true;

            var env = Environment;
            var otherEnv = other.Environment;
            if (Different(env.AmbientTemperature, otherEnv.AmbientTemperature, tolerance)) return true;
            if (Different(env.BarometricPressure, otherEnv.BarometricPressure, tolerance)) return true;
            if (Different(env.WindVelocity, otherEnv.WindVelocity, tolerance)) return true;
            if (Different(env.WindDirection, otherEnv.WindDirection, tolerance)) return true;

            return false;
        }
    }
}
