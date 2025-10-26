using System;
using System.Collections.Generic;

namespace SharedCockpitClient.FlightData
{
    public class SimDataCollector
    {
        private readonly SimConnectManager sim;

        public SimDataCollector(SimConnectManager sim)
        {
            this.sim = sim ?? throw new ArgumentNullException(nameof(sim));
        }

        public SimStateSnapshot Collect()
        {
            var snapshot = new SimStateSnapshot
            {
                Controls = new ControlsStruct
                {
                    Throttle = 0.8,
                    Flaps = 0.0,
                    Elevator = 0.1,
                    Aileron = -0.1,
                    Rudder = 0.0,
                    ParkingBrake = 0,
                    Spoilers = 0
                },
                Cabin = new CabinStruct
                {
                    LandingGearDown = 1,
                    SpoilersDeployed = 0,
                    AutopilotOn = 1,
                    AutopilotAltitude = 3000,
                    AutopilotHeading = 180
                },
                Doors = new DoorsStruct
                {
                    DoorLeftOpen = 0,
                    DoorRightOpen = 0,
                    CargoDoorOpen = 0,
                    RampOpen = 0
                }
            };
            return snapshot;
        }
    }
}
