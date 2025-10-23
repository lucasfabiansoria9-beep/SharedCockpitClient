using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimConnectManager : IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;

    private readonly object _stateLock = new();
    private SimConnect? _simconnect;
    private IntPtr _hiddenWindow = IntPtr.Zero;
    private bool _initialized;
    private bool _disposed;

    private AttitudeStruct _attitude;
    private PositionStruct _position;
    private SpeedStruct _speed;
    private ControlsStruct _controls;
    private CabinStruct _cabin;
    private DoorsStruct _doors;
    private GroundSupportStruct _ground;

    private bool _hasAttitude;
    private bool _hasPosition;
    private bool _hasSpeed;

    private FlightSnapshot? _lastPublished;

    public event Action<FlightSnapshot>? OnFlightDataUpdated;

    public bool IsConnected => _simconnect != null;

    public async Task StartListeningAsync(CancellationToken token)
    {
        EnsureInitialized();

        while (!token.IsCancellationRequested)
        {
            if (_simconnect == null)
            {
                try
                {
                    await Task.Delay(1000, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                EnsureInitialized();
                continue;
            }

            try
            {
                _simconnect?.ReceiveMessage();
            }
            catch (COMException ex)
            {
                Logger.Warn($"⚠️ Error al recibir datos de SimConnect: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ Fallo inesperado en SimConnect: {ex.Message}");
            }

            try
            {
                await Task.Delay(100, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_simconnect != null)
        {
            _simconnect.Dispose();
            _simconnect = null;
        }

        if (_hiddenWindow != IntPtr.Zero)
        {
            NativeMethods.DestroyWindow(_hiddenWindow);
            _hiddenWindow = IntPtr.Zero;
        }
    }

    private void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        lock (_stateLock)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                _hiddenWindow = NativeMethods.CreateWindow();
                if (_hiddenWindow == IntPtr.Zero)
                {
                    Logger.Warn("⚠️ No se pudo crear la ventana oculta para SimConnect.");
                }

                _simconnect = new SimConnect("SharedCockpitClient", _hiddenWindow, WM_USER_SIMCONNECT, null, 0);
                _simconnect.OnRecvOpen += OnSimConnectOpen;
                _simconnect.OnRecvQuit += OnSimConnectQuit;
                _simconnect.OnRecvException += OnSimConnectException;
                _simconnect.OnRecvSimobjectData += OnSimData;

                RegisterDefinitions();
                RequestData();

                _initialized = true;
                Logger.Info("✅ SimConnect inicializado correctamente.");
            }
            catch (COMException ex)
            {
                Logger.Error($"❌ Error de SimConnect: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Error($"❌ No se pudo inicializar SimConnect: {ex.Message}");
            }
        }
    }

    private void RegisterDefinitions()
    {
        RegisterDefinitionGroup<AttitudeStruct>(DEFINITIONS.Attitude, "Actitud",
            new SimVarDefinition("PLANE PITCH DEGREES", "Radians", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE BANK DEGREES", "Radians", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE HEADING DEGREES TRUE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<PositionStruct>(DEFINITIONS.Position, "Posición",
            new SimVarDefinition("PLANE LATITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE LONGITUDE", "Degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE ALTITUDE", "Meters", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<SpeedStruct>(DEFINITIONS.Speed, "Velocidades",
            new SimVarDefinition("AIRSPEED INDICATED", "Knots", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("VERTICAL SPEED", "Feet per minute", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("GROUND VELOCITY", "Knots", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<ControlsStruct>(DEFINITIONS.Controls, "Controles",
            new SimVarDefinition("GENERAL ENG THROTTLE LEVER POSITION:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("FLAPS HANDLE INDEX", "Number", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("ELEVATOR POSITION", "Position", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AILERON POSITION", "Position", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("RUDDER POSITION", "Position", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("BRAKE PARKING POSITION", "Bool", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<CabinStruct>(DEFINITIONS.Cabin, "Cabina",
            new SimVarDefinition("GEAR HANDLE POSITION", "Bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("SPOILERS ARMED", "Bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("AUTOPILOT MASTER", "Bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("AUTOPILOT ALTITUDE LOCK VAR", "Feet", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT HEADING LOCK DIR", "Degrees", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<DoorsRawStruct>(DEFINITIONS.Doors, "Puertas",
            new SimVarDefinition("EXIT OPEN:0", "Percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:1", "Percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:2", "Percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:3", "Percent", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<GroundSupportStruct>(DEFINITIONS.GroundSupport, "Soporte tierra",
            new SimVarDefinition("CATERING TRUCK IS ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("BAGGAGE LOADER ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("FUEL TRUCK ACTIVE", "Bool", SIMCONNECT_DATATYPE.INT32));
    }

    private void RequestData()
    {
        if (_simconnect == null)
        {
            return;
        }

        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_ATTITUDE, DEFINITIONS.Attitude,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_POSITION, DEFINITIONS.Position,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_SPEED, DEFINITIONS.Speed,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CONTROLS, DEFINITIONS.Controls,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CABIN, DEFINITIONS.Cabin,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_DOORS, DEFINITIONS.Doors,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_GROUNDSUPPORT, DEFINITIONS.GroundSupport,
            SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SECOND, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
    }

    private void RegisterDefinitionGroup<T>(DEFINITIONS definition, string label, params SimVarDefinition[] variables) where T : struct
    {
        if (_simconnect == null)
        {
            return;
        }

        bool success = true;
        foreach (var variable in variables)
        {
            success &= TryAddSimVar(definition, variable, label);
        }

        if (!success)
        {
            Logger.Warn($"⚠️ Algunas variables de '{label}' no se pudieron registrar.");
            return;
        }

        try
        {
            _simconnect.RegisterDataDefineStruct<T>(definition);
            Logger.Info($"✅ Grupo '{label}' registrado");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo registrar el grupo '{label}': {ex.Message}");
        }
    }

    private bool TryAddSimVar(DEFINITIONS definition, SimVarDefinition simVar, string label)
    {
        if (_simconnect == null)
        {
            return false;
        }

        try
        {
            _simconnect.AddToDataDefinition(definition, simVar.Variable, simVar.Units, simVar.DataType, simVar.Epsilon, SimConnect.SIMCONNECT_UNUSED);
            return true;
        }
        catch (COMException ex)
        {
            Logger.Warn($"⚠️ Variable ignorada {simVar.Variable} ({label}): {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ Error al agregar {simVar.Variable} ({label}): {ex.Message}");
        }

        return false;
    }

    private void OnSimConnectOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
    {
        Logger.Info($"🛫 Conectado a MSFS como {data.szApplicationName} (Versión {data.dwApplicationVersionMajor}.{data.dwApplicationVersionMinor}).");
    }

    private void OnSimConnectQuit(SimConnect sender, SIMCONNECT_RECV data)
    {
        Logger.Warn("⚠️ SimConnect cerró la sesión.");
    }

    private void OnSimConnectException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
    {
        Logger.Warn($"⚠️ Excepción SimConnect {data.dwException}: {GetExceptionDescription(data.dwException)}");
    }

    private void OnSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        bool shouldPublish = false;

        switch ((DATA_REQUESTS)data.dwRequestID)
        {
            case DATA_REQUESTS.REQUEST_ATTITUDE:
                var attitude = (AttitudeStruct)data.dwData[0];
                if (!_hasAttitude ||
                    attitude.Pitch.IsDifferent(_attitude.Pitch, 0.01) ||
                    attitude.Bank.IsDifferent(_attitude.Bank, 0.01) ||
                    attitude.Heading.IsDifferent(_attitude.Heading, 0.01))
                {
                    _attitude = attitude;
                    _hasAttitude = true;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_POSITION:
                var position = (PositionStruct)data.dwData[0];
                if (!_hasPosition || position.Latitude.IsDifferent(_position.Latitude, 0.00001) ||
                    position.Longitude.IsDifferent(_position.Longitude, 0.00001) ||
                    position.Altitude.IsDifferent(_position.Altitude, 0.1))
                {
                    _position = position;
                    _hasPosition = true;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_SPEED:
                var speed = (SpeedStruct)data.dwData[0];
                if (!_hasSpeed ||
                    speed.IndicatedAirspeed.IsDifferent(_speed.IndicatedAirspeed, 0.1) ||
                    speed.VerticalSpeed.IsDifferent(_speed.VerticalSpeed, 1) ||
                    speed.GroundSpeed.IsDifferent(_speed.GroundSpeed, 0.1))
                {
                    _speed = speed;
                    _hasSpeed = true;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_CONTROLS:
                var controls = (ControlsStruct)data.dwData[0];
                if (controls.Throttle.IsDifferent(_controls.Throttle, 0.5) ||
                    controls.Flaps.IsDifferent(_controls.Flaps, 0.1) ||
                    controls.Elevator.IsDifferent(_controls.Elevator, 0.05) ||
                    controls.Aileron.IsDifferent(_controls.Aileron, 0.05) ||
                    controls.Rudder.IsDifferent(_controls.Rudder, 0.05) ||
                    controls.ParkingBrake.IsDifferent(_controls.ParkingBrake, 0.01))
                {
                    _controls = controls;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_CABIN:
                var cabin = (CabinStruct)data.dwData[0];
                if (cabin.LandingGearDown != _cabin.LandingGearDown ||
                    cabin.SpoilersDeployed != _cabin.SpoilersDeployed ||
                    cabin.AutopilotOn != _cabin.AutopilotOn ||
                    cabin.AutopilotAltitude.IsDifferent(_cabin.AutopilotAltitude, 1) ||
                    cabin.AutopilotHeading.IsDifferent(_cabin.AutopilotHeading, 1))
                {
                    _cabin = cabin;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_DOORS:
                var doorRaw = (DoorsRawStruct)data.dwData[0];
                var doors = new DoorsStruct
                {
                    DoorLeftOpen = doorRaw.Exit0 > 0.5,
                    DoorRightOpen = doorRaw.Exit1 > 0.5,
                    CargoDoorOpen = doorRaw.Exit2 > 0.5,
                    RampOpen = doorRaw.Exit3 > 0.5
                };

                if (doors.DoorLeftOpen != _doors.DoorLeftOpen ||
                    doors.DoorRightOpen != _doors.DoorRightOpen ||
                    doors.CargoDoorOpen != _doors.CargoDoorOpen ||
                    doors.RampOpen != _doors.RampOpen)
                {
                    _doors = doors;
                    shouldPublish = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_GROUNDSUPPORT:
                var ground = (GroundSupportStruct)data.dwData[0];
                if (ground.CateringTruckPresent != _ground.CateringTruckPresent ||
                    ground.BaggageCartsPresent != _ground.BaggageCartsPresent ||
                    ground.FuelTruckPresent != _ground.FuelTruckPresent)
                {
                    _ground = ground;
                    shouldPublish = true;
                }
                break;
        }

        if (!_hasAttitude || !_hasPosition || !_hasSpeed)
        {
            return;
        }

        if (!shouldPublish)
        {
            return;
        }

        PublishSnapshot();
    }

    private void PublishSnapshot()
    {
        var snapshot = new FlightSnapshot
        {
            Attitude = _attitude,
            Position = _position,
            Speed = _speed,
            Controls = _controls,
            Cabin = _cabin,
            Doors = _doors,
            Ground = _ground,
            Timestamp = DateTime.UtcNow
        };

        if (!snapshot.IsMeaningfullyDifferent(_lastPublished, 0.005))
        {
            return;
        }

        _lastPublished = new FlightSnapshot
        {
            Attitude = snapshot.Attitude,
            Position = snapshot.Position,
            Speed = snapshot.Speed,
            Controls = snapshot.Controls,
            Cabin = snapshot.Cabin,
            Doors = snapshot.Doors,
            Ground = snapshot.Ground,
            Timestamp = snapshot.Timestamp
        };

        OnFlightDataUpdated?.Invoke(snapshot);
    }

    private string GetExceptionDescription(uint code)
    {
        return code switch
        {
            0u => "Sin error",
            1u => "Error genérico",
            2u => "Tamaño incompatible",
            3u => "Identificador no reconocido",
            4u => "Conexión no inicializada",
            5u => "Versión incompatible",
            6u => "Demasiados grupos registrados",
            7u => "Variable inválida",
            8u => "Demasiados nombres de evento",
            9u => "Evento duplicado",
            10u => "Demasiados mapeos",
            11u => "Demasiados objetos",
            12u => "Demasiadas peticiones",
            13u => "Error de peso y balance",
            14u => "Error interno",
            15u => "Operación no soportada",
            16u => "Objeto fuera de la burbuja",
            17u => "Error en contenedor",
            18u => "Error con objeto de IA",
            19u => "Error de ATC",
            20u => "Error de sesión compartida",
            21u => "Identificador de dato inválido",
            22u => "Buffer demasiado pequeño",
            23u => "Desbordamiento de buffer",
            24u => "Tipo de dato inválido",
            25u => "Operación ilegal",
            26u => "Funcionalidad obsoleta",
            _ => "Error no documentado"
        };
    }

    private enum DEFINITIONS
    {
        Attitude,
        Position,
        Speed,
        Controls,
        Cabin,
        Doors,
        GroundSupport
    }

    private enum DATA_REQUESTS
    {
        REQUEST_ATTITUDE,
        REQUEST_POSITION,
        REQUEST_SPEED,
        REQUEST_CONTROLS,
        REQUEST_CABIN,
        REQUEST_DOORS,
        REQUEST_GROUNDSUPPORT
    }

    private readonly struct SimVarDefinition
    {
        public SimVarDefinition(string variable, string units, SIMCONNECT_DATATYPE dataType, float epsilon = 0f)
        {
            Variable = variable;
            Units = units;
            DataType = dataType;
            Epsilon = epsilon;
        }

        public string Variable { get; }
        public string Units { get; }
        public SIMCONNECT_DATATYPE DataType { get; }
        public float Epsilon { get; }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct DoorsRawStruct
    {
        public double Exit0;
        public double Exit1;
        public double Exit2;
        public double Exit3;
    }

    private static class NativeMethods
    {
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateWindowEx(
            int dwExStyle,
            string lpClassName,
            string lpWindowName,
            int dwStyle,
            int x,
            int y,
            int nWidth,
            int nHeight,
            IntPtr hWndParent,
            IntPtr hMenu,
            IntPtr hInstance,
            IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyWindowNative(IntPtr hWnd);

        public static IntPtr CreateWindow()
        {
            return CreateWindowEx(0, "Static", "SharedCockpitClient_SimConnect", 0,
                0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        public static void DestroyWindow(IntPtr handle)
        {
            if (handle != IntPtr.Zero)
            {
                DestroyWindowNative(handle);
            }
        }
    }
}
