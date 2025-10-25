using System;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Genera datos falsos de vuelo cuando no se usa SimConnect (modo mock).
    /// </summary>
    public sealed class SimDataMock
    {
        private readonly SimConnectManager simManager;
        private readonly Random random = new();
        private CancellationTokenSource? cts;

        public SimDataMock(SimConnectManager manager)
        {
            simManager = manager;
        }

        public void Start()
        {
            Stop();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            Task.Run(async () =>
            {
                double altitude = 1000;
                double heading = 90;
                double airspeed = 120;
                bool lights = false;

                Logger.Info("🧪 Generando datos simulados de vuelo...");

                while (!token.IsCancellationRequested)
                {
                    // Simula pequeños cambios constantes
                    altitude += random.NextDouble() * 10 - 5;
                    heading = (heading + random.NextDouble() * 2 - 1 + 360) % 360;
                    airspeed += random.NextDouble() * 0.5 - 0.25;
                    lights = random.NextDouble() > 0.97 ? !lights : lights;

                    var snapshot = new SimStateSnapshot
                    {
                        Position = new PositionStruct { Latitude = -34.9, Longitude = -57.9, Altitude = altitude },
                        Attitude = new AttitudeStruct { Pitch = 2, Bank = 0.5, Heading = heading },
                        Speed = new SpeedStruct { IndicatedAirspeed = airspeed, VerticalSpeed = 200, GroundSpeed = 115 },
                        Systems = new SystemsStruct { LandingLight = lights ? 1 : 0 },
                        Controls = new ControlsStruct { Throttle = 0.8, Flaps = 0.2 }
                    };

                    simManager.NotifyMockSnapshot(snapshot);
                    await Task.Delay(500, token);
                }
            }, token);
        }

        public void Stop()
        {
            try { cts?.Cancel(); } catch { }
        }
    }
}
