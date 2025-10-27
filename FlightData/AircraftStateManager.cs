using System;
using SharedCockpitClient.FlightData;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Administra el estado del avión (controles y sistemas).
    /// </summary>
    public sealed class AircraftStateManager
    {
        // ─────────────── EVENTOS ───────────────
        public event Action<SimStateSnapshot>? OnSnapshot;
        public event Action<string, object>? OnStateChanged;

        // ─────────────── ESTADO DE VUELO ───────────────
        public double Throttle { get; private set; }
        public double Flaps { get; private set; }
        public bool GearDown { get; private set; }
        public bool ParkingBrake { get; private set; }

        public bool LightsOn { get; private set; }
        public bool DoorOpen { get; private set; }
        public bool AvionicsOn { get; private set; }

        // ─────────────── MÉTODOS ───────────────

        /// <summary>
        /// Aplica un cambio remoto a una variable específica.
        /// </summary>
        public void ApplyRemoteChange(string variable, System.Text.Json.JsonElement value)
        {
            try
            {
                switch (variable)
                {
                    case nameof(Throttle):
                        Throttle = value.GetDouble();
                        break;
                    case nameof(Flaps):
                        Flaps = value.GetDouble();
                        break;
                    case nameof(GearDown):
                        GearDown = value.GetBoolean();
                        break;
                    case nameof(ParkingBrake):
                        ParkingBrake = value.GetBoolean();
                        break;
                    case nameof(LightsOn):
                        LightsOn = value.GetBoolean();
                        break;
                    case nameof(DoorOpen):
                        DoorOpen = value.GetBoolean();
                        break;
                    case nameof(AvionicsOn):
                        AvionicsOn = value.GetBoolean();
                        break;
                    default:
                        Console.WriteLine($"[AircraftStateManager] ⚠️ Variable desconocida: {variable}");
                        return;
                }

                // Notificar cambio puntual
                OnStateChanged?.Invoke(variable, GetValue(variable));

                // Emitir snapshot completo actualizado
                EmitSnapshot();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AircraftStateManager] ⚠️ Error aplicando cambio: {ex.Message}");
            }
        }

        private object GetValue(string variable) => variable switch
        {
            nameof(Throttle) => Throttle,
            nameof(Flaps) => Flaps,
            nameof(GearDown) => GearDown,
            nameof(ParkingBrake) => ParkingBrake,
            nameof(LightsOn) => LightsOn,
            nameof(DoorOpen) => DoorOpen,
            nameof(AvionicsOn) => AvionicsOn,
            _ => "?"
        };

        /// <summary>
        /// Aplica un snapshot completo (por ejemplo, recibido desde red).
        /// </summary>
        public void ApplySnapshot(SimStateSnapshot snapshot)
        {
            try
            {
                Throttle = snapshot.Controls.Throttle;
                Flaps = snapshot.Controls.Flaps;
                GearDown = snapshot.Controls.GearDown;
                ParkingBrake = snapshot.Controls.ParkingBrake;

                LightsOn = snapshot.Systems.LightsOn;
                DoorOpen = snapshot.Systems.DoorOpen;
                AvionicsOn = snapshot.Systems.AvionicsOn;

                OnStateChanged?.Invoke("Snapshot", snapshot);
                OnSnapshot?.Invoke(snapshot);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AircraftStateManager] ⚠️ Error aplicando snapshot: {ex.Message}");
            }
        }

        /// <summary>
        /// Emite un snapshot actual del estado.
        /// </summary>
        public void EmitSnapshot()
        {
            var snapshot = new SimStateSnapshot
            {
                Controls = new ControlsStruct
                {
                    Throttle = Throttle,
                    Flaps = Flaps,
                    GearDown = GearDown,
                    ParkingBrake = ParkingBrake
                },
                Systems = new SystemsStruct
                {
                    LightsOn = LightsOn,
                    DoorOpen = DoorOpen,
                    AvionicsOn = AvionicsOn
                }
            };

            OnSnapshot?.Invoke(snapshot);
        }
    }
}
