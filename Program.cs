using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
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
        static IntPtr simconnectWindowHandle = IntPtr.Zero;

        const int WM_USER_SIMCONNECT = 0x0402;

        static readonly Dictionary<uint, string> SimConnectExceptionDescriptions = new()
        {
            { 0u, "Sin error" },
            { 1u, "Error genérico" },
            { 2u, "Tamaño de datos incompatible" },
            { 3u, "Identificador no reconocido" },
            { 4u, "Conexión no inicializada" },
            { 5u, "Versión incompatible" },
            { 6u, "Demasiados grupos registrados" },
            { 7u, "Variable inválida o no disponible" },
            { 8u, "Demasiados nombres de evento" },
            { 9u, "Identificador de evento duplicado" },
            { 10u, "Demasiados mapeos registrados" },
            { 11u, "Demasiados objetos" },
            { 12u, "Demasiadas peticiones simultáneas" },
            { 13u, "Error de peso y balance" },
            { 14u, "Error interno de SimConnect" },
            { 15u, "Operación no soportada" },
            { 16u, "Objeto fuera de la burbuja de realidad" },
            { 17u, "Error en contenedor de objeto" },
            { 18u, "Error con objeto de IA" },
            { 19u, "Error de ATC" },
            { 20u, "Error de sesión compartida" },
            { 21u, "Identificador de dato inválido" },
            { 22u, "Buffer demasiado pequeño" },
            { 23u, "Desbordamiento de buffer" },
            { 24u, "Tipo de dato inválido" },
            { 25u, "Operación ilegal" },
            { 26u, "Funcionalidad de misiones obsoleta" }
        };

        static bool attitudeRegistered;
        static bool positionRegistered;
        static bool speedRegistered;
        static bool controlsRegistered;
        static bool cabinRegistered;
        static bool doorsRegistered;
        static bool groundSupportRegistered;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        enum DEFINITIONS
        {
            Attitude, Position, Speed, Controls, Cabin, Doors, GroundSupport
        }

        enum DATA_REQUESTS
        {
            REQUEST_ATTITUDE, REQUEST_POSITION, REQUEST_SPEED, REQUEST_CONTROLS, REQUEST_CABIN, REQUEST_DOORS, REQUEST_GROUNDSUPPORT
        }

        enum DisplayState
        {
            WaitingConnection,
            Synced,
            NoData
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

        readonly struct DoorsRawStruct
        {
            public readonly double Exit0;
            public readonly double Exit1;
            public readonly double Exit2;
            public readonly double Exit3;

            public DoorsRawStruct(double exit0 = 0, double exit1 = 0, double exit2 = 0, double exit3 = 0)
            {
                Exit0 = exit0;
                Exit1 = exit1;
                Exit2 = exit2;
                Exit3 = exit3;
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

        readonly struct SimVarDefinition
        {
            public readonly string Variable;
            public readonly string Units;
            public readonly SIMCONNECT_DATATYPE DataType;
            public readonly float Epsilon;

            public SimVarDefinition(string variable, string units, SIMCONNECT_DATATYPE dataType, float epsilon = 0f)
            {
                Variable = variable;
                Units = units;
                DataType = dataType;
                Epsilon = epsilon;
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

        static bool hasAttitudeData;
        static bool hasPositionData;
        static bool hasSpeedData;

        static readonly TimeSpan displayUpdateInterval = TimeSpan.FromMilliseconds(100);
        static readonly TimeSpan displayTimeout = TimeSpan.FromSeconds(3);
        static readonly TimeSpan broadcastInterval = TimeSpan.FromMilliseconds(100);

        static DateTime lastFlightDataUtc = DateTime.MinValue;
        static DateTime lastDisplayRenderUtc = DateTime.MinValue;
        static DateTime lastBroadcastUtc = DateTime.MinValue;
        static DateTime suppressBroadcastUntilUtc = DateTime.MinValue;

        static string? pendingBroadcastPayload;

        static readonly object broadcastLock = new();
        static readonly object displayLock = new();
        static int dataDisplayLine = -1;
        static string lastDisplayedLine = string.Empty;
        static ConsoleColor lastDisplayColor = ConsoleColor.Gray;
        static DisplayState lastDisplayState = DisplayState.WaitingConnection;

        static readonly object remoteDataLock = new();
        static AttitudeStruct remoteAttitude = new();
        static PositionStruct remotePosition = new();
        static SpeedStruct remoteSpeed = new();
        static ControlsStruct remoteControls = new();
        static CabinStruct remoteCabin = new();
        static DoorsStruct remoteDoors = new();
        static GroundSupportStruct remoteGround = new();

        static readonly JsonSerializerOptions flightDataJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            simconnectWindowHandle = CreateHiddenSimConnectWindow();
            ConfigureMode();
            SetupWebSocket();
            SetupShutdownHandlers();
            ShowWaitingForData();

            try
            {
                // Conexión SimConnect
                simconnect = new SimConnect("SharedCockpitClient", simconnectWindowHandle, WM_USER_SIMCONNECT, null, 0);
                simconnect.OnRecvOpen += OnSimConnectOpen;
                simconnect.OnRecvQuit += OnSimConnectQuit;
                simconnect.OnRecvException += OnSimConnectException;
                simconnect.OnRecvSimobjectData += OnSimData;

                Console.WriteLine("✅ SimConnect inicializado correctamente.");

                // ---- DEFINICIONES ----
                attitudeRegistered = AddAttitudeDefinition();
                positionRegistered = AddPositionDefinition();
                speedRegistered = AddSpeedDefinition();
                controlsRegistered = AddControlsDefinition();
                cabinRegistered = AddCabinDefinition();
                doorsRegistered = AddDoorsDefinition();
                groundSupportRegistered = AddGroundSupportDefinition();

                LogDefinitionSummary();

                // ---- SOLICITAR DATOS ----
                RequestData();

                Console.WriteLine("🛰️ Enviando datos reales de vuelo al copiloto...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ No se pudo conectar a SimConnect: {ex.Message}");
            }

            // ---- BUCLE PRINCIPAL ----
            while (true)
            {
                simconnect?.ReceiveMessage();
                FlushPendingBroadcast();
                EnsureDisplayFreshness();
                Thread.Sleep(100); // 10Hz
            }
        }

        static IntPtr CreateHiddenSimConnectWindow()
        {
            var handle = CreateWindowEx(0, "Static", "SharedCockpitClient_SimConnect", 0,
                0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            if (handle == IntPtr.Zero)
            {
                Console.WriteLine($"⚠️ No se pudo crear la ventana oculta de SimConnect (Error {Marshal.GetLastWin32Error()}).");
            }

            return handle;
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
                hostServer.OnClientDisconnected += () =>
                {
                    Console.WriteLine("ℹ️ Copiloto desconectado del servidor.");
                };
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
                    ShowWaitingForData("⚠️ esperando datos del host...");
                };
                ws.OnError += (msg) =>
                {
                    Console.WriteLine("⚠️ Error WebSocket: " + msg);
                    ShowWaitingForConnection();
                };
                ws.OnClose += () =>
                {
                    Console.WriteLine("🔌 Conexión WebSocket cerrada");
                    ShowWaitingForConnection();
                };
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

        static void OnSimConnectOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
        {
            Console.WriteLine($"🟢 Conectado correctamente con {data.szApplicationName}.");
        }

        static void OnSimConnectQuit(SimConnect sender, SIMCONNECT_RECV data)
        {
            Console.WriteLine("🔴 MSFS se cerró. SimConnect desconectado.");
        }

        static void OnSimConnectException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            var description = GetSimConnectExceptionDescription(data.dwException);
            Console.WriteLine($"⚠️ Excepción SimConnect: {data.dwException} ({description}).");
        }

        static void OnWebSocketMessage(string message)
        {
            if (!TryParseFlightPayload(message, out var payload) || payload == null)
            {
                return;
            }

            if (payload.Position == null || payload.Speed == null || payload.Attitude == null)
            {
                return;
            }

            var attitude = new AttitudeStruct(
                payload.Attitude.Pitch,
                payload.Attitude.Bank,
                payload.Attitude.Heading);

            var position = new PositionStruct(
                payload.Position.Latitude,
                payload.Position.Longitude,
                payload.Position.Altitude);

            var speed = new SpeedStruct(
                payload.Speed.IndicatedAirspeed,
                payload.Speed.VerticalSpeed,
                payload.Speed.GroundSpeed);

            var controls = payload.Controls != null
                ? new ControlsStruct(
                    payload.Controls.Throttle,
                    payload.Controls.Flaps,
                    payload.Controls.Elevator,
                    payload.Controls.Aileron,
                    payload.Controls.Rudder,
                    payload.Controls.ParkingBrake)
                : remoteControls;

            var cabin = payload.Cabin != null
                ? new CabinStruct(
                    payload.Cabin.LandingGearDown,
                    payload.Cabin.SpoilersDeployed,
                    payload.Cabin.AutopilotOn,
                    payload.Cabin.AutopilotAltitude,
                    payload.Cabin.AutopilotHeading)
                : remoteCabin;

            var doors = payload.Doors != null
                ? new DoorsStruct(
                    payload.Doors.DoorLeftOpen,
                    payload.Doors.DoorRightOpen,
                    payload.Doors.CargoDoorOpen,
                    payload.Doors.RampOpen)
                : remoteDoors;

            var ground = payload.Ground != null
                ? new GroundSupportStruct(
                    payload.Ground.CateringTruckPresent,
                    payload.Ground.BaggageCartsPresent,
                    payload.Ground.FuelTruckPresent)
                : remoteGround;

            AttitudeStruct latestAttitude;
            PositionStruct latestPosition;
            SpeedStruct latestSpeed;
            ControlsStruct latestControls;
            CabinStruct latestCabin;
            DoorsStruct latestDoors;
            GroundSupportStruct latestGround;

            lock (remoteDataLock)
            {
                remoteAttitude = attitude;
                remotePosition = position;
                remoteSpeed = speed;
                remoteControls = controls;
                remoteCabin = cabin;
                remoteDoors = doors;
                remoteGround = ground;

                latestAttitude = remoteAttitude;
                latestPosition = remotePosition;
                latestSpeed = remoteSpeed;
                latestControls = remoteControls;
                latestCabin = remoteCabin;
                latestDoors = remoteDoors;
                latestGround = remoteGround;
            }

            if (HasMeaningfulFlightValues(
                latestSpeed.IndicatedAirspeed,
                latestSpeed.VerticalSpeed,
                latestSpeed.GroundSpeed,
                latestPosition.Altitude,
                latestAttitude.Heading,
                latestPosition.Latitude,
                latestPosition.Longitude))
            {
                UpdateFlightDisplay(
                    latestSpeed.IndicatedAirspeed,
                    latestSpeed.VerticalSpeed,
                    latestSpeed.GroundSpeed,
                    latestPosition.Altitude,
                    latestAttitude.Heading,
                    latestPosition.Latitude,
                    latestPosition.Longitude);

                if (isHost)
                {
                    var relayPayload = JsonSerializer.Serialize(new
                    {
                        attitude = latestAttitude,
                        position = latestPosition,
                        speed = latestSpeed,
                        controls = latestControls,
                        cabin = latestCabin,
                        doors = latestDoors,
                        ground = latestGround
                    });

                    QueueBroadcast(relayPayload);
                }

                lock (broadcastLock)
                {
                    suppressBroadcastUntilUtc = DateTime.UtcNow.AddMilliseconds(200);
                    pendingBroadcastPayload = null;
                }
            }
        }

        static string GetSimConnectExceptionDescription(uint code)
        {
            return SimConnectExceptionDescriptions.TryGetValue(code, out var description)
                ? description
                : "Error no documentado";
        }

        static void LogDefinitionSummary()
        {
            if (attitudeRegistered && positionRegistered && speedRegistered && controlsRegistered && cabinRegistered && doorsRegistered)
            {
                Console.WriteLine("✅ Todas las variables registradas con éxito.");
            }
            else
            {
                Console.WriteLine("⚠️ Algunas variables no se pudieron registrar. Revisá los mensajes anteriores para más detalles.");
            }
        }

        static bool RegisterDefinitionGroup<T>(DEFINITIONS definition, string groupName, params SimVarDefinition[] variables) where T : struct
        {
            if (simconnect == null)
            {
                Console.WriteLine($"⚠️ No se pudo registrar el grupo '{groupName}' porque la conexión SimConnect no está disponible.");
                return false;
            }

            bool success = true;

            foreach (var variable in variables)
            {
                success = TryAddSimVar(definition, variable, groupName) && success;
            }

            if (!success)
            {
                Console.WriteLine($"⚠️ Grupo '{groupName}' omitido: una o más variables no están disponibles en MSFS2024.");
                return false;
            }

            try
            {
                simconnect.RegisterDataDefineStruct<T>(definition);
                Console.WriteLine($"✅ Grupo '{groupName}' registrado correctamente.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ No se pudo registrar el grupo '{groupName}': {ex.Message}");
                return false;
            }
        }

        static bool TryAddSimVar(DEFINITIONS definition, SimVarDefinition simVar, string groupName)
        {
            if (simconnect == null)
            {
                return false;
            }

            try
            {
                simconnect.AddToDataDefinition(definition, simVar.Variable, simVar.Units, simVar.DataType, simVar.Epsilon, SimConnect.SIMCONNECT_UNUSED);
                return true;
            }
            catch (COMException ex)
            {
                Console.WriteLine($"⚠️ Variable ignorada por MSFS2024: {simVar.Variable} ({groupName}). {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error al registrar la variable {simVar.Variable} ({groupName}): {ex.Message}");
            }

            return false;
        }

        static bool IsExitOpen(double value) => value > 0.5;

        static void UpdateFlightDisplay(
            double indicatedAirspeed,
            double verticalSpeed,
            double groundSpeed,
            double altitudeMeters,
            double headingDegrees,
            double latitude,
            double longitude,
            DisplayState state = DisplayState.Synced)
        {
            var now = DateTime.UtcNow;

            if (state != DisplayState.Synced)
            {
                ShowStatus(state, state == DisplayState.NoData ? "🔴 sin datos" : "⚠️ esperando datos...", force: true);
                return;
            }

            lastFlightDataUtc = now;

            string line = string.Format(
                CultureInfo.InvariantCulture,
                "🟢 🛫 IAS: {0:F1} kts | VS: {1:F1} fpm | GS: {2:F1} kts | ALT: {3:F1} m | HDG: {4:F1}° | LAT: {5:F1} | LON: {6:F1}",
                indicatedAirspeed,
                verticalSpeed,
                groundSpeed,
                altitudeMeters,
                headingDegrees,
                latitude,
                longitude);

            var color = GetStateColor(DisplayState.Synced);

            bool needsRedraw = line != lastDisplayedLine || lastDisplayState != DisplayState.Synced || lastDisplayColor != color;
            if (!needsRedraw)
            {
                lastDisplayRenderUtc = now;
                return;
            }

            if (now - lastDisplayRenderUtc < displayUpdateInterval)
            {
                return;
            }

            RenderDisplayLine(line, DisplayState.Synced, color, now);
        }

        static void ShowWaitingForData(string message = "⚠️ esperando datos...", bool force = false)
        {
            if (!force && lastDisplayState == DisplayState.Synced)
            {
                return;
            }

            if (lastFlightDataUtc == DateTime.MinValue)
            {
                lastFlightDataUtc = DateTime.UtcNow;
            }

            ShowStatus(DisplayState.WaitingConnection, message, force);
        }

        static void ShowWaitingForConnection()
        {
            ShowStatus(DisplayState.WaitingConnection, "⚠️ esperando conexión...", force: true);
        }

        static void ShowNoData()
        {
            ShowStatus(DisplayState.NoData, "🔴 sin datos", force: true);
        }

        static void ShowStatus(DisplayState state, string message, bool force = false)
        {
            var now = DateTime.UtcNow;
            var color = GetStateColor(state);

            if (!force && lastDisplayState == DisplayState.Synced && state != DisplayState.Synced)
            {
                return;
            }

            if (!force && message == lastDisplayedLine && lastDisplayState == state && lastDisplayColor == color)
            {
                lastDisplayRenderUtc = now;
                if (state == DisplayState.NoData)
                {
                    lastFlightDataUtc = DateTime.MinValue;
                }
                return;
            }

            if (!force && now - lastDisplayRenderUtc < displayUpdateInterval && message == lastDisplayedLine)
            {
                return;
            }

            RenderDisplayLine(message, state, color, now);

            if (state == DisplayState.NoData)
            {
                lastFlightDataUtc = DateTime.MinValue;
            }
        }

        static void RenderDisplayLine(string text, DisplayState state, ConsoleColor color, DateTime timestamp)
        {
            lock (displayLock)
            {
                if (dataDisplayLine == -1 || dataDisplayLine >= SafeConsoleHeight())
                {
                    Console.WriteLine();
                    dataDisplayLine = Console.CursorTop - 1;
                }

                int previousLeft = Console.CursorLeft;
                int previousTop = Console.CursorTop;

                Console.SetCursorPosition(0, Math.Max(0, dataDisplayLine));

                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                int width = SafeConsoleWidth();
                if (width <= 1)
                {
                    width = text.Length + 1;
                }

                string output = text.Length >= width
                    ? text.Substring(0, Math.Max(0, width - 1))
                    : text.PadRight(width - 1);

                Console.Write(output);
                Console.ForegroundColor = originalColor;

                lastDisplayedLine = text;
                lastDisplayColor = color;
                lastDisplayState = state;
                lastDisplayRenderUtc = timestamp;

                if (previousTop != dataDisplayLine)
                {
                    Console.SetCursorPosition(previousLeft, previousTop);
                }
                else
                {
                    int nextLine = Math.Min(dataDisplayLine + 1, SafeConsoleHeight() - 1);
                    Console.SetCursorPosition(0, Math.Max(0, nextLine));
                }
            }
        }

        static ConsoleColor GetStateColor(DisplayState state) => state switch
        {
            DisplayState.Synced => ConsoleColor.Green,
            DisplayState.WaitingConnection => ConsoleColor.Yellow,
            DisplayState.NoData => ConsoleColor.DarkGray,
            _ => ConsoleColor.White
        };

        static void EnsureDisplayFreshness()
        {
            if (lastFlightDataUtc == DateTime.MinValue)
            {
                return;
            }

            if (lastDisplayState != DisplayState.NoData && DateTime.UtcNow - lastFlightDataUtc > displayTimeout)
            {
                ShowNoData();
            }
        }

        static bool HasMeaningfulFlightValues(params double[] values)
        {
            const double tolerance = 0.0001;

            foreach (double value in values)
            {
                if (Math.Abs(value) > tolerance)
                {
                    return true;
                }
            }

            return false;
        }

        static bool TryParseFlightPayload(string message, out FlightDataPayload? payload)
        {
            payload = null;

            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            string trimmed = message.Trim();
            if (!trimmed.StartsWith("{", StringComparison.Ordinal))
            {
                return false;
            }

            try
            {
                using var document = JsonDocument.Parse(trimmed);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                var enumerator = document.RootElement.EnumerateObject();
                if (!enumerator.MoveNext())
                {
                    return false;
                }

                payload = JsonSerializer.Deserialize<FlightDataPayload>(document.RootElement.GetRawText(), flightDataJsonOptions);
                return payload != null;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        static int SafeConsoleWidth()
        {
            try
            {
                return Console.BufferWidth;
            }
            catch (System.IO.IOException)
            {
                return 120;
            }
        }

        static int SafeConsoleHeight()
        {
            try
            {
                return Console.BufferHeight;
            }
            catch (System.IO.IOException)
            {
                return int.MaxValue;
            }
        }

        sealed class FlightDataPayload
        {
            public AttitudePayload? Attitude { get; set; }
            public PositionPayload? Position { get; set; }
            public SpeedPayload? Speed { get; set; }
            public ControlsPayload? Controls { get; set; }
            public CabinPayload? Cabin { get; set; }
            public DoorsPayload? Doors { get; set; }
            public GroundPayload? Ground { get; set; }
        }

        sealed class AttitudePayload
        {
            public double Pitch { get; set; }
            public double Bank { get; set; }
            public double Heading { get; set; }
        }

        sealed class PositionPayload
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public double Altitude { get; set; }
        }

        sealed class SpeedPayload
        {
            public double IndicatedAirspeed { get; set; }
            public double VerticalSpeed { get; set; }
            public double GroundSpeed { get; set; }
        }

        sealed class ControlsPayload
        {
            public double Throttle { get; set; }
            public double Flaps { get; set; }
            public double Elevator { get; set; }
            public double Aileron { get; set; }
            public double Rudder { get; set; }
            public double ParkingBrake { get; set; }
        }

        sealed class CabinPayload
        {
            public bool LandingGearDown { get; set; }
            public bool SpoilersDeployed { get; set; }
            public bool AutopilotOn { get; set; }
            public double AutopilotAltitude { get; set; }
            public double AutopilotHeading { get; set; }
        }

        sealed class DoorsPayload
        {
            public bool DoorLeftOpen { get; set; }
            public bool DoorRightOpen { get; set; }
            public bool CargoDoorOpen { get; set; }
            public bool RampOpen { get; set; }
        }

        sealed class GroundPayload
        {
            public bool CateringTruckPresent { get; set; }
            public bool BaggageCartsPresent { get; set; }
            public bool FuelTruckPresent { get; set; }
        }

        // ---- DEFINICIONES DE SIMCONNECT ----
        static bool AddAttitudeDefinition() => RegisterDefinitionGroup<AttitudeStruct>(
            DEFINITIONS.Attitude,
            "Actitud",
            new SimVarDefinition("PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

        static bool AddPositionDefinition() => RegisterDefinitionGroup<PositionStruct>(
            DEFINITIONS.Position,
            "Posición",
            new SimVarDefinition("PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE ALTITUDE", "meters", SIMCONNECT_DATATYPE.FLOAT64));

        static bool AddSpeedDefinition() => RegisterDefinitionGroup<SpeedStruct>(
            DEFINITIONS.Speed,
            "Velocidades",
            new SimVarDefinition("AIRSPEED INDICATED", "knots", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("VERTICAL SPEED", "feet per minute", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("GROUND VELOCITY", "knots", SIMCONNECT_DATATYPE.FLOAT64));

        static bool AddControlsDefinition() => RegisterDefinitionGroup<ControlsStruct>(
            DEFINITIONS.Controls,
            "Controles",
            new SimVarDefinition("THROTTLE LEVER POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("FLAPS HANDLE INDEX", "number", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("ELEVATOR POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AILERON POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("RUDDER POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PARKING BRAKE POSITION", "bool", SIMCONNECT_DATATYPE.INT32));

        static bool AddCabinDefinition() => RegisterDefinitionGroup<CabinStruct>(
            DEFINITIONS.Cabin,
            "Cabina",
            new SimVarDefinition("GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("SPOILERS HANDLE POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("AUTOPILOT ALTITUDE LOCK VAR", "meters", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT HEADING LOCK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

        static bool AddDoorsDefinition() => RegisterDefinitionGroup<DoorsRawStruct>(
            DEFINITIONS.Doors,
            "Puertas",
            new SimVarDefinition("EXIT OPEN:0", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:1", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:2", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:3", "percent", SIMCONNECT_DATATYPE.FLOAT64));

        static bool AddGroundSupportDefinition()
        {
            Console.WriteLine("ℹ️ Grupo 'Soporte en tierra' omitido: las variables de MSFS2020 no están disponibles en MSFS2024.");
            return false;
        }

        // ---- SOLICITAR DATOS ----
        static void RequestData()
        {
            if (attitudeRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_ATTITUDE, DEFINITIONS.Attitude, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (positionRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_POSITION, DEFINITIONS.Position, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (speedRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_SPEED, DEFINITIONS.Speed, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (controlsRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CONTROLS, DEFINITIONS.Controls, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (cabinRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CABIN, DEFINITIONS.Cabin, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (doorsRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_DOORS, DEFINITIONS.Doors, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }

            if (groundSupportRegistered)
            {
                simconnect?.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_GROUNDSUPPORT, DEFINITIONS.GroundSupport, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
        }

        // ---- RECEPCIÓN DE DATOS ----
        static void OnSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            bool send = false;

            switch ((DATA_REQUESTS)data.dwRequestID)
            {
                case DATA_REQUESTS.REQUEST_ATTITUDE:
                    var att = (AttitudeStruct)data.dwData[0];
                    bool updateAttitude = !hasAttitudeData ||
                        Math.Abs(att.Pitch - lastAttitude.Pitch) > 0.01 ||
                        Math.Abs(att.Bank - lastAttitude.Bank) > 0.01 ||
                        Math.Abs(att.Heading - lastAttitude.Heading) > 0.01;
                    if (updateAttitude)
                    {
                        lastAttitude = att;
                        hasAttitudeData = true;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_POSITION:
                    var pos = (PositionStruct)data.dwData[0];
                    bool updatePosition = !hasPositionData ||
                        Math.Abs(pos.Latitude - lastPosition.Latitude) > 0.00001 ||
                        Math.Abs(pos.Longitude - lastPosition.Longitude) > 0.00001 ||
                        Math.Abs(pos.Altitude - lastPosition.Altitude) > 0.1;
                    if (updatePosition)
                    {
                        lastPosition = pos;
                        hasPositionData = true;
                        send = true;
                    }
                    break;

                case DATA_REQUESTS.REQUEST_SPEED:
                    var spd = (SpeedStruct)data.dwData[0];
                    bool updateSpeed = !hasSpeedData ||
                        Math.Abs(spd.IndicatedAirspeed - lastSpeed.IndicatedAirspeed) > 0.1 ||
                        Math.Abs(spd.VerticalSpeed - lastSpeed.VerticalSpeed) > 1 ||
                        Math.Abs(spd.GroundSpeed - lastSpeed.GroundSpeed) > 0.1;
                    if (updateSpeed)
                    {
                        lastSpeed = spd;
                        hasSpeedData = true;
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
                    var rawDoor = (DoorsRawStruct)data.dwData[0];
                    var door = new DoorsStruct(
                        IsExitOpen(rawDoor.Exit0),
                        IsExitOpen(rawDoor.Exit1),
                        IsExitOpen(rawDoor.Exit2),
                        IsExitOpen(rawDoor.Exit3));
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

            bool hasPrimaryData = hasAttitudeData && hasPositionData && hasSpeedData;

            if (isHost && hasPrimaryData)
            {
                UpdateFlightDisplay(
                    lastSpeed.IndicatedAirspeed,
                    lastSpeed.VerticalSpeed,
                    lastSpeed.GroundSpeed,
                    lastPosition.Altitude,
                    lastAttitude.Heading,
                    lastPosition.Latitude,
                    lastPosition.Longitude);
            }

            if (send && hasPrimaryData)
            {
                if (isHost && hasAttitudeData && hasPositionData && hasSpeedData)
                {
                    UpdateFlightDisplay(
                        lastSpeed.IndicatedAirspeed,
                        lastSpeed.VerticalSpeed,
                        lastSpeed.GroundSpeed,
                        lastPosition.Altitude,
                        lastAttitude.Heading,
                        lastPosition.Latitude,
                        lastPosition.Longitude);
                }

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

                QueueBroadcast(json);
            }
        }

        static void QueueBroadcast(string payload)
        {
            string? immediatePayload = null;

            lock (broadcastLock)
            {
                var now = DateTime.UtcNow;

                if (now < suppressBroadcastUntilUtc)
                {
                    pendingBroadcastPayload = payload;
                    return;
                }

                if (now - lastBroadcastUtc < broadcastInterval)
                {
                    pendingBroadcastPayload = payload;
                    return;
                }

                pendingBroadcastPayload = null;
                lastBroadcastUtc = now;
                immediatePayload = payload;
            }

            if (immediatePayload != null)
            {
                SendToPeers(immediatePayload);
            }
        }

        static void FlushPendingBroadcast()
        {
            string? payloadToSend = null;

            lock (broadcastLock)
            {
                if (pendingBroadcastPayload == null)
                {
                    return;
                }

                var now = DateTime.UtcNow;
                if (now < suppressBroadcastUntilUtc)
                {
                    return;
                }

                if (now - lastBroadcastUtc < broadcastInterval)
                {
                    return;
                }

                payloadToSend = pendingBroadcastPayload;
                pendingBroadcastPayload = null;
                lastBroadcastUtc = now;
            }

            if (payloadToSend != null)
            {
                SendToPeers(payloadToSend);
            }
        }

        static void SendToPeers(string payload)
        {
            if (isHost)
            {
                if (hostServer != null && hostServer.HasClients)
                {
                    hostServer.Broadcast(payload);
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
