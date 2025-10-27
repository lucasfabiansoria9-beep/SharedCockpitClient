using System;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Representa los controles principales del avión (palanca de gases, flaps, tren, freno de estacionamiento).
    /// </summary>
    public struct ControlsStruct
    {
        public double Throttle { get; set; }      // Potencia de motor (0.0 - 1.0)
        public double Flaps { get; set; }         // Posición de flaps
        public bool GearDown { get; set; }        // Tren extendido
        public bool ParkingBrake { get; set; }    // Freno de estacionamiento
    }

    /// <summary>
    /// Representa los sistemas eléctricos y mecánicos del avión.
    /// </summary>
    public struct SystemsStruct
    {
        public bool LightsOn { get; set; }        // Luces encendidas
        public bool DoorOpen { get; set; }        // Puertas abiertas
        public bool AvionicsOn { get; set; }      // Aviónica encendida
        public bool EngineOn { get; set; }        // Estado del motor
    }

    /// <summary>
    /// Snapshot completo del estado del avión (controles + sistemas).
    /// </summary>
    public struct SimStateSnapshot
    {
        public ControlsStruct Controls { get; set; }
        public SystemsStruct Systems { get; set; }
    }
}
