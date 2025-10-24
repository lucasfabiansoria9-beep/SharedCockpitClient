using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimConnectManager : IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;
    private const GROUP_PRIORITY GROUP_PRIORITY_HIGHEST = GROUP_PRIORITY.HIGHEST;

    private static readonly Dictionary<uint, string> SimConnectExceptionDescriptions = new()
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

    private SimConnect? _simconnect;
    private IntPtr _windowHandle = IntPtr.Zero;

    private bool _attitudeRegistered;
    private bool _positionRegistered;
    private bool _speedRegistered;
    private bool _controlsRegistered;
    private bool _cabinRegistered;
    private bool _doorsRegistered;
    private bool _groundSupportRegistered;

    private AttitudeStruct _lastAttitude = new();
    private PositionStruct _lastPosition = new();
    private SpeedStruct _lastSpeed = new();
    private ControlsStruct _lastControls = new();
    private CabinStruct _lastCabin = new();
    private DoorsStruct _lastDoors = new();
    private GroundSupportStruct _lastGround = new();

    private string? _currentRole;
    private string? _pendingCameraRole;

    public event Action<string>? OnFlightDataReady;

    public bool Initialize()
    {
        _windowHandle = CreateHiddenSimConnectWindow();

        try
        {
            _simconnect = new SimConnect("SharedCockpitClient", _windowHandle, WM_USER_SIMCONNECT, null, 0);
            _simconnect.OnRecvOpen += OnSimConnectOpen;
            _simconnect.OnRecvQuit += OnSimConnectQuit;
            _simconnect.OnRecvException += OnSimConnectException;
            _simconnect.OnRecvSimobjectData += OnSimData;

            Logger.Info("✅ SimConnect inicializado correctamente.");

            _attitudeRegistered = AddAttitudeDefinition();
            _positionRegistered = AddPositionDefinition();
            _speedRegistered = AddSpeedDefinition();
            _controlsRegistered = AddControlsDefinition();
            _cabinRegistered = AddCabinDefinition();
            _doorsRegistered = AddDoorsDefinition();
            _groundSupportRegistered = AddGroundSupportDefinition();

            LogDefinitionSummary();

            RegisterCameraEvents();

            if (!string.IsNullOrEmpty(_pendingCameraRole))
            {
                ApplyCameraRole(_pendingCameraRole);
                _pendingCameraRole = null;
            }

            RequestData();

            Logger.Info("🛰️ Enviando datos reales de vuelo al copiloto...");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"❌ No se pudo conectar a SimConnect: {ex.Message}");
            return false;
        }
    }

    public void ReceiveMessage()
    {
        _simconnect?.ReceiveMessage();
    }

    public void Dispose()
    {
        _simconnect?.Dispose();
        _simconnect = null;
    }

    public void SetUserRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        var normalizedRole = role.ToUpperInvariant();

        if (string.Equals(_currentRole, normalizedRole, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        _currentRole = normalizedRole;

        if (_simconnect == null)
        {
            _pendingCameraRole = normalizedRole;
            Logger.Info("⌛ Rol recibido. Ajustaremos la cámara en cuanto SimConnect esté listo.");
            return;
        }

        ApplyCameraRole(normalizedRole);
    }

    private void RequestData()
    {
        if (_simconnect == null)
        {
            return;
        }

        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_ATTITUDE, DEFINITIONS.Attitude, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_POSITION, DEFINITIONS.Position, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_SPEED, DEFINITIONS.Speed, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CONTROLS, DEFINITIONS.Controls, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_CABIN, DEFINITIONS.Cabin, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_DOORS, DEFINITIONS.Doors, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
        _simconnect.RequestDataOnSimObject(DATA_REQUESTS.REQUEST_GROUNDSUPPORT, DEFINITIONS.GroundSupport, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
    }

    private void OnSimConnectOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
    {
        Logger.Info($"🟢 Conectado correctamente con {data.szApplicationName}.");
    }

    private void OnSimConnectQuit(SimConnect sender, SIMCONNECT_RECV data)
    {
        Logger.Warn("🔴 MSFS se cerró. SimConnect desconectado.");
    }

    private void OnSimConnectException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
    {
        var description = GetSimConnectExceptionDescription(data.dwException);
        Logger.Warn($"⚠️ Excepción SimConnect: {data.dwException} ({description}).");
    }

    private void OnSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        bool send = false;

        switch ((DATA_REQUESTS)data.dwRequestID)
        {
            case DATA_REQUESTS.REQUEST_ATTITUDE:
                var att = (AttitudeStruct)data.dwData[0];
                if (Math.Abs(att.Pitch - _lastAttitude.Pitch) > 0.1 ||
                    Math.Abs(att.Bank - _lastAttitude.Bank) > 0.1 ||
                    Math.Abs(att.Heading - _lastAttitude.Heading) > 0.1)
                {
                    _lastAttitude = att;
                    send = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_POSITION:
                var pos = (PositionStruct)data.dwData[0];
                if (Math.Abs(pos.Latitude - _lastPosition.Latitude) > 0.0001 ||
                    Math.Abs(pos.Longitude - _lastPosition.Longitude) > 0.0001 ||
                    Math.Abs(pos.Altitude - _lastPosition.Altitude) > 0.1)
                {
                    _lastPosition = pos;
                    send = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_SPEED:
                var spd = (SpeedStruct)data.dwData[0];
                if (Math.Abs(spd.IndicatedAirspeed - _lastSpeed.IndicatedAirspeed) > 0.1 ||
                    Math.Abs(spd.VerticalSpeed - _lastSpeed.VerticalSpeed) > 1 ||
                    Math.Abs(spd.GroundSpeed - _lastSpeed.GroundSpeed) > 0.1)
                {
                    _lastSpeed = spd;
                    send = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_CONTROLS:
                var ctl = (ControlsStruct)data.dwData[0];
                if (Math.Abs(ctl.Throttle - _lastControls.Throttle) > 0.5 ||
                    Math.Abs(ctl.Flaps - _lastControls.Flaps) > 0.1 ||
                    Math.Abs(ctl.Elevator - _lastControls.Elevator) > 0.1 ||
                    Math.Abs(ctl.Aileron - _lastControls.Aileron) > 0.1 ||
                    Math.Abs(ctl.Rudder - _lastControls.Rudder) > 0.1 ||
                    Math.Abs(ctl.ParkingBrake - _lastControls.ParkingBrake) > 0.01)
                {
                    _lastControls = ctl;
                    send = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_CABIN:
                var cab = (CabinStruct)data.dwData[0];
                if (cab.LandingGearDown != _lastCabin.LandingGearDown ||
                    cab.SpoilersDeployed != _lastCabin.SpoilersDeployed ||
                    cab.AutopilotOn != _lastCabin.AutopilotOn ||
                    Math.Abs(cab.AutopilotAltitude - _lastCabin.AutopilotAltitude) > 1 ||
                    Math.Abs(cab.AutopilotHeading - _lastCabin.AutopilotHeading) > 1)
                {
                    _lastCabin = cab;
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
                if (door.DoorLeftOpen != _lastDoors.DoorLeftOpen ||
                    door.DoorRightOpen != _lastDoors.DoorRightOpen ||
                    door.CargoDoorOpen != _lastDoors.CargoDoorOpen ||
                    door.RampOpen != _lastDoors.RampOpen)
                {
                    _lastDoors = door;
                    send = true;
                }
                break;

            case DATA_REQUESTS.REQUEST_GROUNDSUPPORT:
                var ground = (GroundSupportStruct)data.dwData[0];
                if (ground.CateringTruckPresent != _lastGround.CateringTruckPresent ||
                    ground.BaggageCartsPresent != _lastGround.BaggageCartsPresent ||
                    ground.FuelTruckPresent != _lastGround.FuelTruckPresent)
                {
                    _lastGround = ground;
                    send = true;
                }
                break;
        }

        if (send)
        {
            var json = JsonSerializer.Serialize(new
            {
                attitude = _lastAttitude,
                position = _lastPosition,
                speed = _lastSpeed,
                controls = _lastControls,
                cabin = _lastCabin,
                doors = _lastDoors,
                ground = _lastGround
            });

            OnFlightDataReady?.Invoke(json);
        }
    }

    private static bool IsExitOpen(double value) => value > 0.5;

    private void LogDefinitionSummary()
    {
        if (_attitudeRegistered && _positionRegistered && _speedRegistered && _controlsRegistered && _cabinRegistered && _doorsRegistered)
        {
            Logger.Info("✅ Todas las variables registradas con éxito.");
        }
        else
        {
            Logger.Warn("⚠️ Algunas variables no se pudieron registrar. Revisá los mensajes anteriores para más detalles.");
        }
    }

    private bool AddAttitudeDefinition() => RegisterDefinitionGroup<AttitudeStruct>(
        DEFINITIONS.Attitude,
        "Actitud",
        new SimVarDefinition("PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

    private bool AddPositionDefinition() => RegisterDefinitionGroup<PositionStruct>(
        DEFINITIONS.Position,
        "Posición",
        new SimVarDefinition("PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("PLANE ALTITUDE", "meters", SIMCONNECT_DATATYPE.FLOAT64));

    private bool AddSpeedDefinition() => RegisterDefinitionGroup<SpeedStruct>(
        DEFINITIONS.Speed,
        "Velocidades",
        new SimVarDefinition("AIRSPEED INDICATED", "knots", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("VERTICAL SPEED", "feet per minute", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("GROUND VELOCITY", "knots", SIMCONNECT_DATATYPE.FLOAT64));

    private bool AddControlsDefinition() => RegisterDefinitionGroup<ControlsStruct>(
        DEFINITIONS.Controls,
        "Controles",
        new SimVarDefinition("THROTTLE LEVER POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("FLAPS HANDLE INDEX", "number", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("ELEVATOR POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("AILERON POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("RUDDER POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("PARKING BRAKE POSITION", "bool", SIMCONNECT_DATATYPE.INT32));

    private bool AddCabinDefinition() => RegisterDefinitionGroup<CabinStruct>(
        DEFINITIONS.Cabin,
        "Cabina",
        new SimVarDefinition("GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32),
        new SimVarDefinition("SPOILERS HANDLE POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32),
        new SimVarDefinition("AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

    private bool AddDoorsDefinition() => RegisterDefinitionGroup<DoorsRawStruct>(
        DEFINITIONS.Doors,
        "Puertas",
        new SimVarDefinition("EXIT OPEN:0", "percent", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("EXIT OPEN:1", "percent", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("EXIT OPEN:2", "percent", SIMCONNECT_DATATYPE.FLOAT64),
        new SimVarDefinition("EXIT OPEN:3", "percent", SIMCONNECT_DATATYPE.FLOAT64));

    private bool AddGroundSupportDefinition() => RegisterDefinitionGroup<GroundSupportStruct>(
        DEFINITIONS.GroundSupport,
        "Soporte en tierra",
        new SimVarDefinition("CATERING TRUCK STATE", "bool", SIMCONNECT_DATATYPE.INT32),
        new SimVarDefinition("BAGGAGE LOADER STATE", "bool", SIMCONNECT_DATATYPE.INT32),
        new SimVarDefinition("FUEL TRUCK STATE", "bool", SIMCONNECT_DATATYPE.INT32));

    private bool RegisterDefinitionGroup<T>(DEFINITIONS definition, string groupName, params SimVarDefinition[] variables) where T : struct
    {
        if (_simconnect == null)
        {
            Logger.Warn($"⚠️ No se pudo registrar el grupo '{groupName}' porque la conexión SimConnect no está disponible.");
            return false;
        }

        bool success = true;

        foreach (var variable in variables)
        {
            success = TryAddSimVar(definition, variable, groupName) && success;
        }

        if (!success)
        {
            Logger.Warn($"⚠️ Grupo '{groupName}' omitido: una o más variables no están disponibles en MSFS2024.");
            return false;
        }

        try
        {
            _simconnect.RegisterDataDefineStruct<T>(definition);
            Logger.Info($"✅ Grupo '{groupName}' registrado correctamente.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo registrar el grupo '{groupName}': {ex.Message}");
            return false;
        }
    }

    private void RegisterCameraEvents()
    {
        if (_simconnect == null)
        {
            return;
        }

        try
        {
            _simconnect.MapClientEventToSimEvent(EVENT_ID.CAMERA_SELECT_PILOT, "VIEW_CAMERA_SELECT_1");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo mapear el evento de cámara del piloto: {ex.Message}");
        }

        try
        {
            _simconnect.MapClientEventToSimEvent(EVENT_ID.CAMERA_SELECT_COPILOT, "VIEW_CAMERA_SELECT_2");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo mapear el evento de cámara del copiloto: {ex.Message}");
        }
    }

    private void ApplyCameraRole(string normalizedRole)
    {
        if (_simconnect == null)
        {
            _pendingCameraRole = normalizedRole;
            return;
        }

        if (normalizedRole == "PILOT")
        {
            Logger.Info("📸 Ajustando cámara a posición de Piloto...");
            _simconnect.TransmitClientEvent(0, EVENT_ID.CAMERA_SELECT_PILOT, 0, GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
        else if (normalizedRole == "COPILOT")
        {
            Logger.Info("📸 Ajustando cámara a posición de Copiloto...");
            _simconnect.TransmitClientEvent(0, EVENT_ID.CAMERA_SELECT_COPILOT, 0, GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
        else
        {
            Logger.Warn($"⚠️ Rol desconocido recibido para ajuste de cámara: {normalizedRole}");
        }
    }

    private bool TryAddSimVar(DEFINITIONS definition, SimVarDefinition simVar, string groupName)
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
            Logger.Warn($"⚠️ Variable ignorada por MSFS2024: {simVar.Variable} ({groupName}). {ex.Message}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ Error al registrar la variable {simVar.Variable} ({groupName}): {ex.Message}");
        }

        return false;
    }

    private static string GetSimConnectExceptionDescription(uint code)
    {
        return SimConnectExceptionDescriptions.TryGetValue(code, out var description)
            ? description
            : "Error no documentado";
    }

    private static IntPtr CreateHiddenSimConnectWindow()
    {
        var handle = CreateWindowEx(0, "Static", "SharedCockpitClient_SimConnect", 0,
            0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (handle == IntPtr.Zero)
        {
            Logger.Warn($"⚠️ No se pudo crear la ventana oculta de SimConnect (Error {Marshal.GetLastWin32Error()}).");
        }

        return handle;
    }

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

    private enum EVENT_ID
    {
        CAMERA_SELECT_PILOT,
        CAMERA_SELECT_COPILOT
    }

    private enum GROUP_PRIORITY : uint
    {
        HIGHEST = 1
    }
}
