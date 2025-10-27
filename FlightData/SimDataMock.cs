using System;
using System.Threading;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Genera datos simulados del vuelo (modo Mock) para pruebas sin MSFS2024.
    /// </summary>
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
                            Flaps = rng.Next(0, 40),
                            GearDown = rng.Next(0, 2) == 1,
                            ParkingBrake = rng.Next(0, 2) == 1
                        },
                        Systems = new SystemsStruct
                        {
                            LightsOn = rng.Next(0, 2) == 1,
                            DoorOpen = rng.Next(0, 2) == 1,
                            AvionicsOn = rng.Next(0, 2) == 1
                        }
                    };

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
