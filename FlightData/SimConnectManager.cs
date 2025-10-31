#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
#if SIMCONNECT_STUB
using SIMCONNECT_DATA_DEFINITION_ID = System.UInt32;
using SIMCONNECT_CLIENT_EVENT_ID = System.UInt32;
#endif
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    /// <summary>
    /// Orquesta la interacción con SimConnect real, generando snapshots y aplicando comandos remotos.
    /// </summary>
    public sealed class SimConnectManager : IDisposable
    {
        private readonly AircraftStateManager _aircraftState;
        private readonly object _snapshotLock = new();
        private SimStateSnapshot _lastSnapshot = new();
        private readonly SimDataCollector _collector;
        private readonly SimCommandApplier _commandApplier;
        private bool _started;
        private SimConnect? _simConnect;
        private IntPtr _windowHandle = IntPtr.Zero;
        private CancellationTokenSource? _offlineCts;
        private bool _initialSnapshotQueued;
        private readonly Dictionary<string, object?> lastValues = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, SimVarDescriptor> _requestToDescriptor = new();
        private readonly Dictionary<string, uint> _definitionByKey = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, SimEventDescriptor> _clientEventById = new();
        private readonly Dictionary<string, uint> _eventClientByName = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<uint, DateTime> _pendingEventEcho = new();
        private readonly Guid _localInstanceId = Guid.NewGuid();
        private readonly string _localInstanceKey;
        private long _commandSequence;
        private long _snapshotSequence;

        private const uint DATA_DEFINITION_BASE = 1000;
        private const uint DATA_REQUEST_BASE = 2000;
        private const uint EVENT_ID_BASE = 3000;
        private const uint GROUP_COMMANDS = 42;

        public event Action<SimStateSnapshot, bool>? OnSnapshot;
        public event Action<SimCommandMessage>? OnCommand;

        public bool IsConnected { get; private set; }

        public double LastFps { get; private set; } = -1;

        public Guid LocalInstanceId => _localInstanceId;

        public string LocalInstanceKey => _localInstanceKey;

        public SimConnectManager(AircraftStateManager aircraftState)
        {
            _aircraftState = aircraftState ?? throw new ArgumentNullException(nameof(aircraftState));

            _localInstanceKey = _localInstanceId.ToString("N");

#if SIMCONNECT_STUB
            Logger.Warn("[SimConnect] ⚠️ SimConnect.dll no encontrado. Usando stub administrado para compilación. La sincronización en vivo requiere la DLL real.");
#endif

            Logger.Info("[SimConnect] 🚀 Inicializando conexión real con MSFS2024...");
            InitializeSimConnect();
            _collector = new SimDataCollector(ct => ReadSnapshotFlatAsync(ct));

            _collector.OnSnapshot += HandleSnapshot;

            _commandApplier = new SimCommandApplier(
                ApplySimVarChangeAsync,
                TriggerSimEventAsync,
                MirrorState);
        }

        // ------------------------------------------------------------------
        // 🧩 Integración real con MSFS2024 usando catálogo embebido
        // ------------------------------------------------------------------
        private void InitializeSimConnect()
        {
            try
            {
                _simConnect = new SimConnect("SharedCockpitClient", _windowHandle, 0, null, 0);
                OnSimConnectOpened();

                _simConnect.OnRecvOpen += HandleSimConnectOpen;
                _simConnect.OnRecvQuit += HandleSimConnectQuit;
                _simConnect.OnRecvException += HandleSimConnectException;
                _simConnect.OnRecvSimobjectData += HandleSimConnectSimobjectData;
                _simConnect.OnRecvEvent += HandleSimConnectEvent;

                var registeredVars = RegisterSimVarSubscriptions();
                var registeredEvents = RegisterSimEvents();

                Logger.Info($"[SimConnect] 📘 Catálogo cargado: {registeredVars} variables, {registeredEvents} eventos.");
            }
            catch (Exception ex)
            {
                Logger.Error($"[SimConnect] ❌ Error al conectar: {ex.Message}");
                _simConnect = null;
                IsConnected = false;
                throw;
            }
        }

        private void OnSimConnectOpened()
        {
            IsConnected = true;
            StopOfflineLoop();
            Logger.Info("[SimConnect] ✅ Conexión establecida correctamente.");
            lastValues.Clear();

            if (!_initialSnapshotQueued)
            {
                _initialSnapshotQueued = true;
                _ = EmitInitialSnapshotWithDelayAsync();
            }
        }

        private int RegisterSimVarSubscriptions()
        {
            if (_simConnect == null)
                return 0;

            _requestToDescriptor.Clear();
            _definitionByKey.Clear();

            var registered = 0;
            var definitionId = DATA_DEFINITION_BASE;
            var requestId = DATA_REQUEST_BASE;

            foreach (var descriptor in SimDataDefinition.AllSimVars)
            {
                try
                {
                    var defId = (SIMCONNECT_DATA_DEFINITION_ID)definitionId++;
                    var reqId = (SIMCONNECT_DATA_REQUEST_ID)requestId++;
                    var simType = descriptor.DataType switch
                    {
                        SimDataType.Float32 => SIMCONNECT_DATATYPE.FLOAT32,
                        SimDataType.Int32 => SIMCONNECT_DATATYPE.INT32,
                        SimDataType.Bool => SIMCONNECT_DATATYPE.INT32,
                        SimDataType.String256 => SIMCONNECT_DATATYPE.STRING256,
                        _ => SIMCONNECT_DATATYPE.FLOAT64
                    };

                    _simConnect.AddToDataDefinition(defId, descriptor.Name, descriptor.Units ?? string.Empty, simType, 0.0f, SimConnect.SIMCONNECT_UNUSED);
                    RegisterStructForDescriptor(defId, descriptor.DataType);

                    var key = descriptor.DefinitionKey;
                    _definitionByKey[key] = (uint)defId;
                    _definitionByKey[descriptor.Name] = (uint)defId;
                    _definitionByKey[BuildSimVarKey(descriptor)] = (uint)defId;

                    _requestToDescriptor[(uint)reqId] = descriptor;

                    _simConnect.RequestDataOnSimObject(reqId, defId, SimConnect.SIMCONNECT_OBJECT_ID_USER,
                        SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.CHANGED, 0, 0, 0);
                    _simConnect.RequestDataOnSimObjectType(reqId, defId, 0, SIMCONNECT_SIMOBJECT_TYPE.USER);
                    registered++;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[SimConnect] ⚠️ No se pudo registrar SimVar {descriptor.Name}: {ex.Message}");
                }
            }

            return registered;
        }

        private int RegisterSimEvents()
        {
            if (_simConnect == null)
                return 0;

            _clientEventById.Clear();
            _eventClientByName.Clear();

            var groupId = (SIMCONNECT_NOTIFICATION_GROUP_ID)GROUP_COMMANDS;
            var eventId = EVENT_ID_BASE;
            var registered = 0;

            foreach (var descriptor in SimDataDefinition.AllSimEvents)
            {
                try
                {
                    var clientId = (SIMCONNECT_CLIENT_EVENT_ID)eventId++;
                    _simConnect.MapClientEventToSimEvent(clientId, descriptor.EventName);
                    _simConnect.AddClientEventToNotificationGroup(groupId, clientId, false);

                    _clientEventById[(uint)clientId] = descriptor;

                    var normalized = SimDataDefinition.NormalizeEventName(descriptor.EventName);
                    if (!string.IsNullOrWhiteSpace(normalized))
                        _eventClientByName[normalized] = (uint)clientId;
                    if (!string.IsNullOrWhiteSpace(descriptor.Path))
                        _eventClientByName[descriptor.Path] = (uint)clientId;

                    registered++;
                }
                catch (Exception ex)
                {
                    Logger.Warn($"[SimConnect] ⚠️ No se pudo mapear evento {descriptor.EventName}: {ex.Message}");
                }
            }

            _simConnect.SetNotificationGroupPriority(groupId, (uint)SIMCONNECT_GROUP_PRIORITY.HIGHEST);
            return registered;
        }

        private void HandleSimConnectOpen(object? sender, EventArgs e)
        {
            OnSimConnectOpened();
        }

        private void HandleSimConnectQuit(object? sender, EventArgs e)
        {
            Logger.Error("[SimConnect] ❌ Sesión cerrada.");
            _simConnect?.Dispose();
            _simConnect = null;
            IsConnected = false;
            _initialSnapshotQueued = false;
        }

        private void HandleSimConnectException(object? sender, SIMCONNECT_RECV_EXCEPTION data)
        {
            Logger.Warn($"[SimConnect] ⚠️ Excepción: {data.dwException}");
        }

        private void HandleSimConnectSimobjectData(object? sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            try
            {
                ProcessSimobjectData(data);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error procesando snapshot: {ex.Message}");
            }
        }

        private void HandleSimConnectEvent(object? sender, SIMCONNECT_RECV_EVENT data)
        {
            if (!_clientEventById.TryGetValue((uint)data.uEventID, out var descriptor))
                return;

            lock (_pendingEventEcho)
            {
                if (_pendingEventEcho.TryGetValue((uint)data.uEventID, out var expiry))
                {
                    if (DateTime.UtcNow <= expiry)
                    {
                        _pendingEventEcho.Remove((uint)data.uEventID);
                        return;
                    }

                    _pendingEventEcho.Remove((uint)data.uEventID);
                }
            }

            var normalized = SimDataDefinition.NormalizeEventName(descriptor.EventName);
            var sequence = Interlocked.Increment(ref _commandSequence);
            var message = new SimCommandMessage
            {
                Command = normalized,
                OriginId = _localInstanceKey,
                Sequence = sequence,
                ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Target = descriptor.Path,
                Value = data.dwData
            };

            Logger.Debug($"[RealtimeSync] 🛠 Control recibido: {normalized} (local)");
            OnCommand?.Invoke(message);
        }

        private void ProcessSimobjectData(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwRequestID == 0 || !_requestToDescriptor.TryGetValue(data.dwRequestID, out var descriptor))
                return;

            var payload = ExtractSimDataPayload(data);

            if (!TryReadIncomingValue(descriptor, payload, out var stateValue))
                return;

            var key = BuildSimVarKey(descriptor);

            if (lastValues.TryGetValue(key, out var previous) && Equals(previous, stateValue))
                return;

            lastValues[key] = stateValue;

            var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                [key] = stateValue
            };

            var sequence = Interlocked.Increment(ref _snapshotSequence);
            var snapshot = new SimStateSnapshot(diff, DateTime.UtcNow, true, sequence);

            lock (_snapshotLock)
                _lastSnapshot = _lastSnapshot.MergeDiff(snapshot);

            _aircraftState.ApplySnapshot(snapshot.ToFlatDictionary());
        }

        private void RegisterStructForDescriptor(SIMCONNECT_DATA_DEFINITION_ID definitionId, SimDataType type)
        {
            if (_simConnect == null)
                return;

            try
            {
                switch (type)
                {
                    case SimDataType.Float32:
                    case SimDataType.Float64:
                        _simConnect.RegisterDataDefineStruct<double>(definitionId);
                        break;
                    case SimDataType.Int32:
                    case SimDataType.Bool:
                        _simConnect.RegisterDataDefineStruct<int>(definitionId);
                        break;
                    case SimDataType.String256:
                        _simConnect.RegisterDataDefineStruct<string>(definitionId);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error registrando estructura para {definitionId}: {ex.Message}");
            }
        }

        private static string BuildSimVarKey(SimVarDescriptor descriptor)
            => $"SimVars.{descriptor.Name}";

        private static object?[]? ExtractSimDataPayload(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
#if SIMCONNECT_STUB
            return data.dwData;
#else
            return data.dwData switch
            {
                object[] array => array,
                object value => new object?[] { value },
                _ => null
            };
#endif
        }

        private bool TryReadIncomingValue(SimVarDescriptor descriptor, IReadOnlyList<object?>? rawData, out object? value)
        {
            value = null;
            if (rawData == null || rawData.Count == 0)
                return false;

            var raw = rawData[0];

            try
            {
                switch (descriptor.DataType)
                {
                    case SimDataType.Float64:
                    case SimDataType.Float32:
                        value = Convert.ToDouble(raw);
                        return true;
                    case SimDataType.Int32:
                        value = Convert.ToInt32(raw);
                        return true;
                    case SimDataType.Bool:
                        value = raw switch
                        {
                            bool b => b,
                            int i => i != 0,
                            uint ui => ui != 0,
                            long l => l != 0,
                            double d => Math.Abs(d) > double.Epsilon,
                            string s when bool.TryParse(s, out var parsedBool) => parsedBool,
                            string s when int.TryParse(s, out var parsedInt) => parsedInt != 0,
                            _ => Convert.ToInt32(raw) != 0
                        };
                        return true;
                    case SimDataType.String256:
                        value = Convert.ToString(raw) ?? string.Empty;
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error leyendo {descriptor.Name}: {ex.Message}");
            }

            return false;
        }

        private bool TryGetDefinitionId(SimVarDescriptor descriptor, out SIMCONNECT_DATA_DEFINITION_ID definitionId)
        {
            if (_definitionByKey.TryGetValue(descriptor.DefinitionKey, out var id) ||
                _definitionByKey.TryGetValue(descriptor.Name, out id) ||
                _definitionByKey.TryGetValue(BuildSimVarKey(descriptor), out id))
            {
                definitionId = (SIMCONNECT_DATA_DEFINITION_ID)id;
                return true;
            }

            definitionId = 0;
            return false;
        }

        private bool TryConvertOutgoingValue(SimVarDescriptor descriptor, object? value, out object? normalized, out object? payload)
        {
            normalized = null;
            payload = null;

            try
            {
                switch (descriptor.DataType)
                {
                    case SimDataType.Float32:
                    case SimDataType.Float64:
                        var dbl = Convert.ToDouble(value ?? 0);
                        normalized = dbl;
                        payload = dbl;
                        return true;
                    case SimDataType.Int32:
                        var intVal = Convert.ToInt32(value ?? 0);
                        normalized = intVal;
                        payload = intVal;
                        return true;
                    case SimDataType.Bool:
                        var boolVal = value switch
                        {
                            bool b => b,
                            int i => i != 0,
                            long l => l != 0,
                            double d => Math.Abs(d) > double.Epsilon,
                            string s when bool.TryParse(s, out var parsedBool) => parsedBool,
                            string s when int.TryParse(s, out var parsedInt) => parsedInt != 0,
                            _ => Convert.ToInt32(value ?? 0) != 0
                        };
                        normalized = boolVal;
                        payload = boolVal ? 1 : 0;
                        return true;
                    case SimDataType.String256:
                        var strVal = Convert.ToString(value) ?? string.Empty;
                        normalized = strVal;
                        payload = strVal;
                        return true;
                }
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error convirtiendo valor para {descriptor.Name}: {ex.Message}");
            }

            return false;
        }

        private bool TryGetClientEventId(SimEventDescriptor descriptor, out SIMCONNECT_CLIENT_EVENT_ID clientEventId)
        {
            if (_eventClientByName.TryGetValue(descriptor.Path, out var id) ||
                _eventClientByName.TryGetValue(SimDataDefinition.NormalizeEventName(descriptor.EventName), out id))
            {
                clientEventId = (SIMCONNECT_CLIENT_EVENT_ID)id;
                return true;
            }

            clientEventId = 0;
            return false;
        }

        private async Task EmitInitialSnapshotWithDelayAsync()
        {
            Logger.Info("[SimConnect] 🕐 Esperando inicialización de variables...");
            await Task.Delay(1500).ConfigureAwait(false);
            EmitInitialSnapshot();
        }

        private void EmitInitialSnapshot()
        {
            var snapshot = _aircraftState.ExportFullSnapshot();
            if (snapshot.Values != null && snapshot.Values.Count > 0)
            {
                OnSnapshot?.Invoke(snapshot, false);
                Logger.Info($"[SimConnect] 📤 Snapshot inicial emitido con {snapshot.Values.Count} variables.");
            }
            else
            {
                Logger.Warn("[SimConnect] ⚠️ Snapshot inicial omitido (sin datos)");
            }
        }

        // ------------------------------------------------------------------
        // Métodos base de snapshot y sincronización
        // ------------------------------------------------------------------
        private Task<IDictionary<string, object?>> ReadSnapshotFlatAsync(CancellationToken ct)
        {
            Dictionary<string, object?> copy;
            lock (_snapshotLock)
            {
                copy = new Dictionary<string, object?>(_lastSnapshot.Values, StringComparer.OrdinalIgnoreCase);
            }

            return Task.FromResult<IDictionary<string, object?>>(copy);
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _collector.Start();
        }

        public async Task WaitForCockpitReadyAsync(int timeoutMs = 15000)
        {
            var start = DateTime.UtcNow;

            while (!IsConnected)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    break;

                Logger.Info("[SimConnect] ⏳ Esperando MSFS...");
                await Task.Delay(1000).ConfigureAwait(false);
            }

            double altFeet;
            do
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    break;

                altFeet = 0;
                if (TryGet("PLANE ALTITUDE", out altFeet) && altFeet > 1)
                    break;

                await Task.Delay(500).ConfigureAwait(false);
            }
            while (true);

            Logger.Info("[SimConnect] 🛫 Cabina lista.");
        }

        public Task StopAsync() => _collector.StopAsync();

        public Task<bool> ApplyRemoteChangeAsync(string path, object? value, CancellationToken ct)
            => _commandApplier.ApplyAsync(path, value, ct);

        private Task<bool> ApplySimVarChangeAsync(SimVarDescriptor descriptor, object? value, CancellationToken ct)
        {
            if (_simConnect == null)
                return Task.FromResult(false);

            if (!TryGetDefinitionId(descriptor, out var definitionId))
                return Task.FromResult(false);

            if (!TryConvertOutgoingValue(descriptor, value, out var normalized, out var payload))
                return Task.FromResult(false);

            try
            {
                _simConnect.SetDataOnSimObject(definitionId, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, payload!);
                var key = BuildSimVarKey(descriptor);
                MirrorState(key, normalized);
                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error aplicando cambio {descriptor.Name}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private Task<bool> TriggerSimEventAsync(SimEventDescriptor descriptor, object? value, CancellationToken ct)
        {
            if (_simConnect == null)
                return Task.FromResult(false);

            if (!TryGetClientEventId(descriptor, out var clientEvent))
                return Task.FromResult(false);

            uint eventData = value switch
            {
                uint ui => ui,
                int i => (uint)i,
                long l => (uint)l,
                bool b => b ? 1u : 0u,
                double d => (uint)d,
                _ => 0u
            };

            try
            {
                lock (_pendingEventEcho)
                    _pendingEventEcho[(uint)clientEvent] = DateTime.UtcNow.AddMilliseconds(500);

                _simConnect.TransmitClientEvent(
                    SimConnect.SIMCONNECT_OBJECT_ID_USER,
                    clientEvent,
                    eventData,
                    (SIMCONNECT_NOTIFICATION_GROUP_ID)GROUP_COMMANDS,
                    SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);

                return Task.FromResult(true);
            }
            catch (Exception ex)
            {
                Logger.Warn($"[SimConnect] ⚠️ Error disparando evento {descriptor.EventName}: {ex.Message}");
                return Task.FromResult(false);
            }
        }

        private void HandleSnapshot(SimStateSnapshot snapshot, bool isDiff)
        {
            lock (_snapshotLock)
            {
                if (_lastSnapshot.Values.Count == 0 || !snapshot.IsDiff)
                    _lastSnapshot = snapshot.Clone();
                else
                    _lastSnapshot = _lastSnapshot.MergeDiff(snapshot);
            }

            if (snapshot.TryGetDouble("FRAME RATE", out var fps) && fps >= 0)
                LastFps = fps;
            else if (snapshot.TryGetDouble("FRAME RATE VAR", out var fpsVar) && fpsVar >= 0)
                LastFps = fpsVar;
            else if (!IsConnected)
                LastFps = -1;

            var flat = snapshot.ToFlatDictionary();
            _aircraftState.ApplySnapshot(flat);

            var count = snapshot.Values?.Count ?? 0;
            if (count == 0 && snapshot.IsDiff)
                return;

            if (count > 0)
            {
                var description = snapshot.IsDiff
                    ? $"📤 Snapshot enviado: {count} variables modificadas."
                    : $"📤 Snapshot enviado: {count} variables iniciales.";
                Logger.Info($"[SimConnect] {description}");
            }

            OnSnapshot?.Invoke(snapshot, isDiff);
        }

        private void MirrorState(string path, object? value)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            string canonicalPath = path;
            object? normalized = value;

            if (SimDataDefinition.TryGetVar(path, out var descriptor) ||
                SimDataDefinition.TryGetVarBySimVarKey(path, out descriptor))
            {
                canonicalPath = BuildSimVarKey(descriptor);
                if (TryConvertOutgoingValue(descriptor, value, out var normalizedValue, out _))
                    normalized = normalizedValue;
                lastValues[canonicalPath] = normalized;
            }
            else if (path.StartsWith("SimVars.", StringComparison.OrdinalIgnoreCase))
            {
                lastValues[path] = normalized;
            }

            lock (_snapshotLock)
                _lastSnapshot.Set(canonicalPath, normalized);
            _aircraftState.Set(canonicalPath, normalized);
        }

        public SimStateSnapshot GetLastSnapshot()
        {
            lock (_snapshotLock)
                return _lastSnapshot.Clone();
        }

        public bool TryGet(string path, out double value)
        {
            lock (_snapshotLock)
            {
                if (_lastSnapshot.TryGetDouble(path, out var result))
                {
                    value = result;
                    return true;
                }
            }

            value = 0;
            return false;
        }

        public void ApplySimVar(string path, object? value)
        {
            if (string.IsNullOrWhiteSpace(path) || value is null)
                return;

            if (SimDataDefinition.TryGetVar(path, out var descriptor) ||
                SimDataDefinition.TryGetVarBySimVarKey(path, out descriptor))
            {
                _ = _commandApplier.ApplyAsync(descriptor.Path, value, CancellationToken.None);
                return;
            }

            if (!SimStateSnapshot.LooksLikeSimVar(path))
                return;

            MirrorState(path, value);
        }

        public void Dispose()
        {
            _collector.Dispose();
            _simConnect?.Dispose();
            StopOfflineLoop();
        }

        private void StartOfflineLoop()
        {
            StopOfflineLoop();
            IsConnected = false;
            Logger.Warn("[SimConnect] ⚪ Offline. Requiere reinicio del cliente para reconectar.");
        }

        private void StopOfflineLoop()
        {
            _offlineCts?.Dispose();
            _offlineCts = null;
        }
    }
}
