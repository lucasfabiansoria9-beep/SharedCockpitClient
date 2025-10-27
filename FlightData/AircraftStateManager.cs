using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Administra el estado del avión (controles y sistemas).
    /// </summary>
    public sealed class AircraftStateManager
    {
        private readonly Dictionary<string, CancellationTokenSource> animationTokens = new(StringComparer.OrdinalIgnoreCase);
        private readonly object animationLock = new();
        private const double NumericTolerance = 0.01;
        private static readonly TimeSpan AnimationStep = TimeSpan.FromMilliseconds(30);

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
                        AnimateNumericChange(variable, value.GetDouble());
                        return;
                    case nameof(Flaps):
                        AnimateNumericChange(variable, value.GetDouble());
                        return;
                    case nameof(GearDown):
                        GearDown = value.GetBoolean();
                        NotifyStateChanged(variable, GearDown);
                        return;
                    case nameof(ParkingBrake):
                        ParkingBrake = value.GetBoolean();
                        NotifyStateChanged(variable, ParkingBrake);
                        return;
                    case nameof(LightsOn):
                        LightsOn = value.GetBoolean();
                        NotifyStateChanged(variable, LightsOn);
                        return;
                    case nameof(DoorOpen):
                        DoorOpen = value.GetBoolean();
                        NotifyStateChanged(variable, DoorOpen);
                        return;
                    case nameof(AvionicsOn):
                        AvionicsOn = value.GetBoolean();
                        NotifyStateChanged(variable, AvionicsOn);
                        return;
                    default:
                        Logger.Warn($"[AircraftStateManager] ⚠️ Variable desconocida: {variable}");
                        return;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[AircraftStateManager] ⚠️ Error aplicando cambio: {ex.Message}");
            }
        }

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

        private void NotifyStateChanged(string variable, object newValue)
        {
            OnStateChanged?.Invoke(variable, newValue);
            EmitSnapshot();
        }

        private void AnimateNumericChange(string variable, double target)
        {
            double start = GetNumericValue(variable);
            if (double.IsNaN(start))
            {
                start = 0;
            }

            var delta = target - start;
            if (Math.Abs(delta) <= NumericTolerance)
            {
                SetNumericValue(variable, target);
                NotifyStateChanged(variable, target);
                return;
            }

            CancelExistingAnimation(variable);

            var tokenSource = new CancellationTokenSource();
            RegisterAnimation(variable, tokenSource);

            Logger.Info($"[AnimStart] {variable} {start.ToString("0.00", CultureInfo.InvariantCulture)} → {target.ToString("0.00", CultureInfo.InvariantCulture)}");

            _ = Task.Run(async () =>
            {
                var token = tokenSource.Token;
                try
                {
                    var steps = Math.Max(3, (int)Math.Ceiling(Math.Abs(delta) / 0.02));

                    for (int i = 1; i <= steps; i++)
                    {
                        token.ThrowIfCancellationRequested();

                        var progress = i / (double)steps;
                        var nextValue = start + (delta * progress);

                        SetNumericValue(variable, nextValue);
                        OnStateChanged?.Invoke(variable, nextValue);
                        EmitSnapshot();

                        if (i < steps)
                        {
                            await Task.Delay(AnimationStep, token).ConfigureAwait(false);
                        }
                    }

                    Logger.Info($"[AnimEnd]   {variable} completado");
                }
                catch (OperationCanceledException)
                {
                    // Reemplazado por una nueva animación
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[AircraftStateManager] ⚠️ Error en animación de {variable}: {ex.Message}");
                }
                finally
                {
                    tokenSource.Dispose();
                    RemoveAnimation(variable, tokenSource);
                }
            });
        }

        private void CancelExistingAnimation(string variable)
        {
            lock (animationLock)
            {
                if (animationTokens.TryGetValue(variable, out var existing))
                {
                    existing.Cancel();
                    animationTokens.Remove(variable);
                    existing.Dispose();
                }
            }
        }

        private void RegisterAnimation(string variable, CancellationTokenSource cts)
        {
            lock (animationLock)
            {
                animationTokens[variable] = cts;
            }
        }

        private void RemoveAnimation(string variable, CancellationTokenSource cts)
        {
            lock (animationLock)
            {
                if (animationTokens.TryGetValue(variable, out var existing) && existing == cts)
                {
                    animationTokens.Remove(variable);
                }
            }
        }

        private double GetNumericValue(string variable) => variable switch
        {
            nameof(Throttle) => Throttle,
            nameof(Flaps) => Flaps,
            _ => double.NaN
        };

        private void SetNumericValue(string variable, double value)
        {
            switch (variable)
            {
                case nameof(Throttle):
                    Throttle = value;
                    break;
                case nameof(Flaps):
                    Flaps = value;
                    break;
            }
        }
    }
}
