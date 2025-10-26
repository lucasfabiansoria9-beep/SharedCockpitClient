using System;
using System.Threading;

namespace SharedCockpitClient.FlightData
{
    public class SimDataMock
    {
        private readonly SimConnectManager manager;
        private readonly Random rng = new();
        private Thread? worker;
        private bool running;

        public SimDataMock(SimConnectManager manager)
        {
            this.manager = manager ?? throw new ArgumentNullException(nameof(manager));
        }

        public void Start()
        {
            if (running) return;
            running = true;

            worker = new Thread(() =>
            {
                while (running)
                {
                    var snapshot = new SimStateSnapshot
                    {
                        Controls = new ControlsStruct
                        {
                            Throttle = rng.NextDouble(),
                            Flaps = rng.NextDouble(),
                            Elevator = rng.NextDouble() * 2 - 1,
                            Aileron = rng.NextDouble() * 2 - 1,
                            Rudder = rng.NextDouble() * 2 - 1,
                            ParkingBrake = rng.Next(0, 2),
                            Spoilers = rng.NextDouble()
                        },
                        Cabin = new CabinStruct
                        {
                            LandingGearDown = rng.Next(0, 2),
                            SpoilersDeployed = rng.NextDouble(),
                            AutopilotOn = rng.Next(0, 2),
                            AutopilotAltitude = 1000 + rng.NextDouble() * 5000,
                            AutopilotHeading = rng.NextDouble() * 360
                        },
                        Environment = new EnvironmentStruct
                        {
                            AmbientTemperature = 20 + rng.NextDouble() * 15,
                            BarometricPressure = 29.92,
                            WindVelocity = rng.NextDouble() * 30,
                            WindDirection = rng.NextDouble() * 360
                        }
                    };

                    // ✅ Enviar snapshot mediante método público (no invocar evento directo)
                    manager.InjectSnapshot(snapshot);
                    Thread.Sleep(1000);
                }
            })
            { IsBackground = true };
            worker.Start();
        }

        public void Stop()
        {
            running = false;
            worker?.Join();
        }
    }
}
