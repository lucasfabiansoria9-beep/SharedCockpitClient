using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimDataCollector : IDisposable
{
    private readonly SimConnect _simconnect;
    private readonly System.Threading.Timer _pollTimer; // ✅ especificamos namespace completo
    private readonly object _lock = new();
    private readonly HashSet<SimDataDefinition> _registeredDefinitions = new();
    private readonly SimStateSnapshot _snapshot = new();

    private bool _disposed;

    public SimDataCollector(SimConnect simconnect)
    {
        _simconnect = simconnect ?? throw new ArgumentNullException(nameof(simconnect));

        _simconnect.OnRecvSimobjectData += OnSimData;

        _pollTimer = new System.Threading.Timer(OnPoll, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // ✅ igual aquí

        RegisterDefinitions();
    }

    public event Action<SimStateSnapshot>? OnSnapshot;

    public SimStateSnapshot CurrentSnapshot
    {
        get
        {
            lock (_lock)
            {
                return _snapshot.Clone();
            }
        }
    }

    public void Start()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SimDataCollector));

        Logger.Info("🛰️ Iniciando recolección periódica de datos del simulador...");
        RequestAll();
        _pollTimer.Change(TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
    }

    public void Stop()
    {
        if (_disposed)
            return;

        _pollTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _pollTimer.Dispose();
        _simconnect.OnRecvSimobjectData -= OnSimData;
    }

    private void OnPoll(object? state)
    {
        if (_disposed)
            return;

        try
        {
            RequestAll();
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ Error al solicitar datos a SimConnect: {ex.Message}");
        }
    }

    private void RegisterDefinitions()
    {
        RegisterDefinitionGroup<AttitudeStruct>(SimDataDefinition.Attitude, "Actitud",
            new SimVarDefinition("PLANE PITCH DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE BANK DEGREES", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE HEADING DEGREES TRUE", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<PositionStruct>(SimDataDefinition.Position, "Posición",
            new SimVarDefinition("PLANE LATITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE LONGITUDE", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PLANE ALTITUDE", "feet", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<SpeedStruct>(SimDataDefinition.Speed, "Velocidades",
            new SimVarDefinition("AIRSPEED INDICATED", "knots", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("VERTICAL SPEED", "feet per minute", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("GROUND VELOCITY", "knots", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<ControlsStruct>(SimDataDefinition.Controls, "Controles",
            new SimVarDefinition("THROTTLE LEVER POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("FLAPS HANDLE PERCENT", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("ELEVATOR POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AILERON POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("RUDDER POSITION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PARKING BRAKE POSITION", "bool", SIMCONNECT_DATATYPE.INT32));

        RegisterDefinitionGroup<CabinStruct>(SimDataDefinition.Cabin, "Cabina",
            new SimVarDefinition("GEAR HANDLE POSITION", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("SPOILERS HANDLE POSITION", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT MASTER", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("AUTOPILOT ALTITUDE LOCK VAR", "feet", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT HEADING LOCK DIR", "degrees", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<SystemsStruct>(SimDataDefinition.Systems, "Sistemas",
            new SimVarDefinition("LIGHT LANDING", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("LIGHT BEACON", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("LIGHT NAV", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("LIGHT STROBE", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("LIGHT TAXI", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("ELECTRICAL MASTER BATTERY", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("ELECTRICAL MASTER ALTERNATOR", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("AVIONICS MASTER SWITCH", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("GENERAL ENG FUEL PUMP SWITCH:1", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("PITOT HEAT", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("STRUCTURAL DEICE SWITCH", "bool", SIMCONNECT_DATATYPE.INT32));

        RegisterDefinitionGroup<DoorsRawStruct>(SimDataDefinition.Doors, "Puertas",
            new SimVarDefinition("EXIT OPEN:0", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:1", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:2", "percent", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("EXIT OPEN:3", "percent", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<GroundSupportStruct>(SimDataDefinition.Ground, "Soporte en tierra",
            new SimVarDefinition("CATERING TRUCK STATE", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("BAGGAGE LOADER STATE", "bool", SIMCONNECT_DATATYPE.INT32),
            new SimVarDefinition("FUEL TRUCK STATE", "bool", SIMCONNECT_DATATYPE.INT32));

        RegisterDefinitionGroup<EnvironmentStruct>(SimDataDefinition.Environment, "Ambiente",
            new SimVarDefinition("AMBIENT TEMPERATURE", "celsius", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("TOTAL AIR TEMPERATURE", "celsius", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("BAROMETER PRESSURE", "inHg", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AMBIENT WIND VELOCITY", "knots", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AMBIENT WIND DIRECTION", "degrees", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("PRECIP RATE", "unknown", SIMCONNECT_DATATYPE.FLOAT64));

        RegisterDefinitionGroup<AvionicsStruct>(SimDataDefinition.Avionics, "Aviónica",
            new SimVarDefinition("COM ACTIVE FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("COM STANDBY FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("NAV ACTIVE FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("NAV STANDBY FREQUENCY:1", "Hz", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("TRANSPONDER CODE:1", "number", SIMCONNECT_DATATYPE.FLOAT64),
            new SimVarDefinition("AUTOPILOT NAV1 LOCK", "bool", SIMCONNECT_DATATYPE.INT32));
    }

    private void RegisterDefinitionGroup<T>(SimDataDefinition definition, string groupName, params SimVarDefinition[] variables) where T : struct
    {
        try
        {
            foreach (var variable in variables)
                _simconnect.AddToDataDefinition(definition, variable.Variable, variable.Units, variable.DataType, variable.Epsilon, SimConnect.SIMCONNECT_UNUSED);

            _simconnect.RegisterDataDefineStruct<T>(definition);
            _registeredDefinitions.Add(definition);
            Logger.Info($"✅ Grupo '{groupName}' registrado con SimConnect.");
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo registrar el grupo '{groupName}': {ex.Message}");
        }
    }

    private void RequestAll()
    {
        foreach (SimDataDefinition definition in Enum.GetValues(typeof(SimDataDefinition)))
        {
            if (!_registeredDefinitions.Contains(definition))
                continue;

            try
            {
                var requestId = (SimDataRequest)(int)definition;
                _simconnect.RequestDataOnSimObject(requestId, definition, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    SIMCONNECT_PERIOD.NEVER, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ Error solicitando datos de '{definition}': {ex.Message}");
            }
        }
    }

    private void OnSimData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
    {
        var request = (SimDataRequest)data.dwRequestID;

        lock (_lock)
        {
            switch (request)
            {
                case SimDataRequest.Attitude:
                    _snapshot.Attitude = (AttitudeStruct)data.dwData[0];
                    break;
                case SimDataRequest.Position:
                    _snapshot.Position = (PositionStruct)data.dwData[0];
                    break;
                case SimDataRequest.Speed:
                    _snapshot.Speed = (SpeedStruct)data.dwData[0];
                    break;
                case SimDataRequest.Controls:
                    _snapshot.Controls = (ControlsStruct)data.dwData[0];
                    break;
                case SimDataRequest.Cabin:
                    _snapshot.Cabin = (CabinStruct)data.dwData[0];
                    break;
                case SimDataRequest.Systems:
                    _snapshot.Systems = (SystemsStruct)data.dwData[0];
                    break;
                case SimDataRequest.Doors:
                    UpdateDoors((DoorsRawStruct)data.dwData[0]);
                    break;
                case SimDataRequest.Ground:
                    _snapshot.Ground = (GroundSupportStruct)data.dwData[0];
                    break;
                case SimDataRequest.Environment:
                    _snapshot.Environment = (EnvironmentStruct)data.dwData[0];
                    break;
                case SimDataRequest.Avionics:
                    _snapshot.Avionics = (AvionicsStruct)data.dwData[0];
                    break;
            }
        }

        PublishSnapshot();
    }

    private void UpdateDoors(DoorsRawStruct raw)
    {
        _snapshot.Doors = new DoorsStruct
        {
            DoorLeftOpen = raw.Exit0 > 0.5 ? 1 : 0,
            DoorRightOpen = raw.Exit1 > 0.5 ? 1 : 0,
            CargoDoorOpen = raw.Exit2 > 0.5 ? 1 : 0,
            RampOpen = raw.Exit3 > 0.5 ? 1 : 0
        };
    }

    private void PublishSnapshot()
    {
        SimStateSnapshot clone;
        lock (_lock)
        {
            clone = _snapshot.Clone();
        }

        OnSnapshot?.Invoke(clone);
    }

    private enum SimDataRequest
    {
        Attitude,
        Position,
        Speed,
        Controls,
        Cabin,
        Systems,
        Doors,
        Ground,
        Environment,
        Avionics
    }
}
