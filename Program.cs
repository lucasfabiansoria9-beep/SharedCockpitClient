using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;

namespace SharedCockpitClient
{
    class Program
    {
        // Estado general de red
        static bool isHost = true;
        static string webSocketUrl = "ws://localhost:8081";
        static bool warnedNoConnection = false;

        // WebSocket y SimConnect
        static WebSocketManager? ws;
        static WebSocketHost? hostServer;
        static SimConnect? simconnect;

        enum DEFINITIONS
        {
            Attitude, Position, Speed, Controls, Cabin, Doors, GroundSupport
        }

        enum DATA_REQUESTS
        {
            REQUEST_ATTITUDE, REQUEST_POSITION, REQUEST_SPEED, REQUEST_CONTROLS, REQUEST_CABIN, REQUEST_DOORS, REQUEST_GROUNDSUPPORT
        }

        // ---- ESTRUCTURAS DE DATOS ----
        readonly struct AttitudeStruct
        {
            public readonly double Pitch;
            public readonly double Bank;
            public readonly double Heading;

            public AttitudeStruct(double pitch = 0, double bank = 0, double heading = 0)
            {
                Pitch = pitch;
                Bank = bank;
                Heading = heading;
            }
        }

        readonly struct PositionStruct
        {
            public readonly double Latitude;
            public readonly double Longitude;
            public readonly double Altitude;

            public PositionStruct(double lat = 0, double lon = 0, double alt = 0)
            {
                Latitude = lat;
                Longitude = lon;
                Altitude = alt;
            }
        }

        readonly struct SpeedStruct
        {
            public readonly double IndicatedAirspeed;
            public readonly double VerticalSpeed;
            public readonly double GroundSpeed;

            public SpeedStruct(double ias = 0, double vs = 0, double gs = 0)
            {
                IndicatedAirspeed = ias;
                VerticalSpeed = vs;
                GroundSpeed = gs;
            }
        }

        readonly struct ControlsStruct
        {
            public readonly double Throttle;
            public readonly double Flaps;
            public readonly double Elevator;
            public readonly double Aileron;
            public readonly double Rudder;
            public readonly double ParkingBrake;

            public ControlsStruct(double throttle = 0, double flaps = 0, double elevator = 0,
                                  double aileron = 0, double rudder = 0, double parkingBrake = 0)
            {
                Throttle = throttle;
                Flaps = flaps;
                Elevator = elevator;
                Aileron = aileron;
                Rudder = rudder;
                ParkingBrake = parkingBrake;
            }
        }

        readonly struct CabinStruct
        {
            public readonly bool LandingGearDown;
            public readonly bool SpoilersDeployed;
            public readonly bool AutopilotOn;
            public readonly double AutopilotAltitude;
            public readonly double AutopilotHeading;

            public CabinStruct(bool gear = false, bool spoilers = false, bool autopilot = false,
                               double alt = 0, double heading = 0)
            {
                LandingGearDown = gear;
                SpoilersDeployed = spoilers;
                AutopilotOn = autopilot;
                AutopilotAltitude = alt;
                AutopilotHeading = heading;
            }
        }

        readonly struct DoorsStruct
        {
            public readonly bool DoorLeftOpen;
            public readonly bool DoorRightOpen;
            public readonly bool CargoDoorOpen;
            public readonly bool RampOpen;

            public DoorsStruct(bool left = false, bool right = false, bool cargo = false, bool ramp = false)
            {
                DoorLeftOpen = left;
                DoorRightOpen = right;
                CargoDoorOpen = cargo;
                RampOpen = ramp;
            }
        }

        readonly struct GroundSupportStruct
        {
            public readonly bool CateringTruckPresent;
            public readonly bool BaggageCartsPresent;
            public readonly bool FuelTruckPresent;

            public GroundSupportStruct(bool catering = false, bool baggage = false, bool fuel = false)
            {
                CateringTruckPresent = catering;
                BaggageCartsPresent = baggage;
                FuelTruckPresent = fuel;
            }
        }

        // ---- ÚLTIMO VALOR PARA DELTA ----
        static AttitudeStruct lastAttitude = new();
        static PositionStruct lastPosition = new();
        static SpeedStruct lastSpeed = new();
        static ControlsStruct lastControls = new();
        static CabinStruct lastCabin = new();
        static DoorsStruct lastDoors = new();
        static GroundSupportStruct lastGround = new();

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            ConfigureMode();
            SetupWebSocket();
            SetupShutdownHandlers();

            // Conexión SimConnect
            simconnect = new SimConnect("SharedCockpitClient", IntPtr.Zero, 0, null, 0);
            simconnect.OnRecvOpen += (s, e) => Console.WriteLine($"🟢 Conectado con {e.szApplicationName}");
            simconnect.OnRecvSimobjectData += OnSimData;

            // ---- DEFINICIONES ----
            AddAttitudeDefinition();
            AddPositionDefinition();
            AddSpeedDefinition();
            AddControlsDefinition();
            AddCabinDefinition();
            AddDoorsDefinition();
            AddGroundSupportDefinition();

            // ---- SOLICITAR DATOS ----
            RequestData();

            // ---- BUCLE PRINCIPAL ----
            while (true)
            {
                simconnect?.ReceiveMessage();
                Thread.Sleep(100); // 10Hz
            }
        }

        static void ConfigureMode()
        {
            Console.WriteLine("Selecciona modo de operación:");
            Console.WriteLine("1) Host (piloto principal)");
            Console.WriteLine("2) Cliente (copiloto)");
            Console.Write("Opción [1]: ");

            var option = Console.ReadLine();
            isHost = option?.Trim() == "2" ? false : true;

            if (isHost)
            {
                Console.WriteLine("➡️  Modo HOST seleccionado. Este equipo iniciará el servidor WebSocket en el puerto 8081.");
                Console.WriteLine("   Pide al copiloto que se conecte a ws://<tu-ip>:8081");
            }
            else
            {
                Console.Write("IP o hostname del piloto principal [localhost]: ");
                var hostInput = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(hostInput))
                {
                    hostInput = "localhost";
                }

                webSocketUrl = $"ws://{hostInput.Trim()}:8081";
                Console.WriteLine($"➡️  Modo CLIENTE seleccionado. Intentando conectar a {webSocketUrl}");
            }

            Console.WriteLine();
        }

        static void SetupWebSocket()
        {
            if (isHost)
            {
                hostServer = new WebSocketHost(8081);
                hostServer.OnClientConnected += () =>
                {
                    Console.WriteLine("✅ Copiloto conectado. Comenzaremos a enviar los datos de vuelo en cuanto cambien.");
                    warnedNoConnection = false;
                };
                hostServer.OnClientDisconnected += () => Console.WriteLine("ℹ️ Copiloto desconectado del servidor.");
                hostServer.OnMessage += OnWebSocketMessage;
                hostServer.Start();
            }
            else
            {
                ws = new WebSocketManager(webSocketUrl);
                ws.OnOpen += () =>
                {
                    Console.WriteLine("🌐 Conectado al piloto principal.");
                    warnedNoConnection = false;
                };
                ws.OnError += (msg) => Console.WriteLine("⚠️ Error WebSocket: " + msg);
                ws.OnClose += () => Console.WriteLine("🔌 Conexión WebSocket cerrada");
                ws.OnMessage += OnWebSocketMessage;
                ws.Connect();
            }

            Console.WriteLine("Presiona Ctrl+C para cerrar la aplicación.");
        }

        static void SetupShutdownHandlers()
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                Shutdown();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
        }

        static void Shutdown()
        {
            ws?.Close();
            hostServer?.Stop();
            simconnect?.Dispose();
        }

        static void OnWebSocketMessage(string message)
        {
            Console.WriteLine($"📨 Datos recibidos por WebSocket: {message}");
        }

        // ---- DEFINICIONES DE SIMCONNECT ----
        static void AddAttitudeDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Attitude, "PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Attitude, "PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Attitude, "PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<AttitudeStruct>(DEFINITIONS.Attitude);
        }

        static void AddPositionDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Position, "PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Position, "PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Position, "PLANE ALTITUDE", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<PositionStruct>(DEFINITIONS.Position);
        }

        static void AddSpeedDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Speed, "AIRSPEED INDICATED", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Speed, "VERTICAL SPEED", "feet per minute", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Speed, "GROUND VELOCITY", "knots", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<SpeedStruct>(DEFINITIONS.Speed);
        }

        static void AddControlsDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "THROTTLE LEVER POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "FLAPS HANDLE INDEX", "number", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "ELEVATOR POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "AILERON POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "RUDDER POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Controls, "PARKING BRAKE POSITION", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<ControlsStruct>(DEFINITIONS.Controls);
        }

        static void AddCabinDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Cabin, "GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Cabin, "SPOILERS HANDLE POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Cabin, "AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Cabin, "AUTOPILOT ALTITUDE LOCK VAR", "meters", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Cabin, "AUTOPILOT HEADING LOCK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<CabinStruct>(DEFINITIONS.Cabin);
        }

        static void AddDoorsDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.Doors, "DOOR LEFT OPEN", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Doors, "DOOR RIGHT OPEN", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Doors, "CARGO DOOR OPEN", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.Doors, "RAMP POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<DoorsStruct>(DEFINITIONS.Doors);
        }

        static void AddGroundSupportDefinition()
        {
            simconnect?.AddToDataDefinition(DEFINITIONS.GroundSupport, "CATERING TRUCK PRESENT", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.GroundSupport, "BAGGAGE CARTS PRESENT", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.AddToDataDefinition(DEFINITIONS.GroundSupport, "FUEL TRUCK PRESENT", "bool", SIMCONNECT_DATATYPE.INT32, 0f, SimConnect.SIMCONNECT_UNUSED);
            simconnect?.RegisterDataDefineStruct<GroundSupportStruct>(DEFINITIONS.GroundSupport);
        }

        // ---- SOLICITAR DATOS ----
        static void RequestData()
        {
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_ATTITUDE, DEFINITIONS.Attitude, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_POSITION, DEFINITIONS.Position, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_SPEED, DEFINITIONS.Speed, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CONTROLS, DEFINITIONS.Controls, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CABIN, DEFINITIONS.Cabin, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_DOORS, DEFINITIONS.Doors, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_GROUNDSUPPORT, DEFINITIONS.GroundSupport, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        }

        // ---- RECEPCIÓN DE DATOS ----
        static void OnSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            bool send = false;

            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_ATTITUDE:
                    var att = (AttitudeStruct)data.dwData[0];
                    if (Math.Abs(att.Pitch - lastAttitude.Pitch) > 0.01 ||
                        Math.Abs(att.Bank - lastAttitude.Bank) > 0.01 ||
                        Math.Abs(att.Heading - lastAttitude.Heading) > 0.01)
                    {
                        lastAttitude = att;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_POSITION:
                    var pos = (PositionStruct)data.dwData[0];
                    if (Math.Abs(pos.Latitude - lastPosition.Latitude) > 0.00001 ||
                        Math.Abs(pos.Longitude - lastPosition.Longitude) > 0.00001 ||
                        Math.Abs(pos.Altitude - lastPosition.Altitude) > 0.1)
                    {
                        lastPosition = pos;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_SPEED:
                    var spd = (SpeedStruct)data.dwData[0];
                    if (Math.Abs(spd.IndicatedAirspeed - lastSpeed.IndicatedAirspeed) > 0.1 ||
                        Math.Abs(spd.VerticalSpeed - lastSpeed.VerticalSpeed) > 1 ||
                        Math.Abs(spd.GroundSpeed - lastSpeed.GroundSpeed) > 0.1)
                    {
                        lastSpeed = spd;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_CONTROLS:
                    var ctl = (ControlsStruct)data.dwData[0];
                    if (Math.Abs(ctl.Throttle - lastControls.Throttle) > 0.5 ||
                        Math.Abs(ctl.Flaps - lastControls.Flaps) > 0.1 ||
                        Math.Abs(ctl.Elevator - lastControls.Elevator) > 0.1 ||
                        Math.Abs(ctl.Aileron - lastControls.Aileron) > 0.1 ||
                        Math.Abs(ctl.Rudder - lastControls.Rudder) > 0.1 ||
                        Math.Abs(ctl.ParkingBrake - lastControls.ParkingBrake) > 0.01)
                    {
                        lastControls = ctl;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_CABIN:
                    var cab = (CabinStruct)data.dwData[0];
                    if (cab.LandingGearDown != lastCabin.LandingGearDown ||
                        cab.SpoilersDeployed != lastCabin.SpoilersDeployed ||
                        cab.AutopilotOn != lastCabin.AutopilotOn ||
                        Math.Abs(cab.AutopilotAltitude - lastCabin.AutopilotAltitude) > 1 ||
                        Math.Abs(cab.AutopilotHeading - lastCabin.AutopilotHeading) > 1)
                    {
                        lastCabin = cab;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_DOORS:
                    var door = (DoorsStruct)data.dwData[0];
                    if (door.DoorLeftOpen != lastDoors.DoorLeftOpen ||
                        door.DoorRightOpen != lastDoors.DoorRightOpen ||
                        door.CargoDoorOpen != lastDoors.CargoDoorOpen ||
                        door.RampOpen != lastDoors.RampOpen)
                    {
                        lastDoors = door;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_GROUNDSUPPORT:
                    var ground = (GroundSupportStruct)data.dwData[0];
                    if (ground.CateringTruckPresent != lastGround.CateringTruckPresent ||
                        ground.BaggageCartsPresent != lastGround.BaggageCartsPresent ||
                        ground.FuelTruckPresent != lastGround.FuelTruckPresent)
                    {
                        lastGround = ground;
                        send = true;
                    }
                    break;
            }

            if (send)
            {
                var json = JsonSerializer.Serialize(new
                {
                    attitude = lastAttitude,
                    position = lastPosition,
                    speed = lastSpeed,
                    controls = lastControls,
                    cabin = lastCabin,
                    doors = lastDoors,
                    ground = lastGround
                });

                SendToPeers(json);
            }
        }

        static void SendToPeers(string payload)
        {
            if (isHost)
            {
                if (hostServer != null && hostServer.HasClients)
                {
                    hostServer.Broadcast(payload);
                    Console.WriteLine("[C#] Datos enviados al/los copiloto(s): " + payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Console.WriteLine("⌛ Aún no hay copilotos conectados. Los datos se enviarán automáticamente en cuanto se unan.");
                    warnedNoConnection = true;
                }
            }
            else
            {
                if (ws != null)
                {
                    ws.Send(payload);
                    Console.WriteLine("[C#] Datos enviados al host: " + payload);
                    warnedNoConnection = false;
                }
                else if (!warnedNoConnection)
                {
                    Console.WriteLine("⚠️ No se puede enviar datos porque el WebSocket no está conectado.");
                    warnedNoConnection = true;
                }
            }
        }
    }
}
