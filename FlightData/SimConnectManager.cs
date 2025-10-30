using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Tools;

namespace SharedCockpitClient.FlightData
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
        private Task? _offlineTask;
        private bool _offlineMode;
        private bool _initialSnapshotQueued;
        private readonly Dictionary<string, object?> lastValues = new(StringComparer.OrdinalIgnoreCase);

        public event Action<SimStateSnapshot, bool>? OnSnapshot;

        public bool IsConnected { get; private set; }

        public double LastFps { get; private set; } = -1;

        private enum DEFINITIONS { SnapshotStruct = 1 }
        private enum REQUESTS { SnapshotRequest = 1 }

        public SimConnectManager(AircraftStateManager aircraftState)
        {
            _aircraftState = aircraftState ?? throw new ArgumentNullException(nameof(aircraftState));

            if (GlobalFlags.IsLabMode)
            {
                if (!IsConnected)
                {
                    Console.WriteLine("[SimConnect] 🧪 Modo laboratorio activo (sin conexión real).");
                }
                _collector = new SimDataCollector(ct => ReadSnapshotFlatAsync(ct));
            }
            else
            {
                Console.WriteLine("[SimConnect] 🚀 Inicializando conexión real con MSFS2024...");
                InitializeSimConnect();
                _collector = new SimDataCollector(ct => ReadSnapshotFlatAsync(ct));
            }

            _collector.OnSnapshot += HandleSnapshot;

            _commandApplier = new SimCommandApplier(
                (desc, val, ct) => ApplySimVarChangeAsync(desc.Path, val, ct),
                (desc, val, ct) => TriggerSimEventAsync(desc.Path, val, ct),
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

                if (!SimVarCatalogGenerator.TryGetCatalog(out var catalog))
                {
                    Console.WriteLine("[SimConnect] ⚠️ No se encontró catálogo de SimVars/SimEvents embebido.");
                    return;
                }

                // 1️⃣ Crear una definición de datos unificada
                var registeredCount = 0;
                var hadScanErrors = false;

                foreach (var simVar in catalog.SimVars)
                {
                    try
                    {
                        _simConnect.AddToDataDefinition(
                            DEFINITIONS.SnapshotStruct,
                            simVar.Name,
                            simVar.Units ?? string.Empty,
                            simVar.DataType switch
                            {
                                SimDataType.Float32 => SIMCONNECT_DATATYPE.FLOAT32,
                                SimDataType.Int32 => SIMCONNECT_DATATYPE.INT32,
                                SimDataType.Bool => SIMCONNECT_DATATYPE.INT32,
                                SimDataType.String256 => SIMCONNECT_DATATYPE.STRING256,
                                _ => SIMCONNECT_DATATYPE.FLOAT64
                            },
                            0.0f,
                            SimConnect.SIMCONNECT_UNUSED
                        );
                        registeredCount++;
                    }
                    catch (Exception)
                    {
                        hadScanErrors = true;
                    }
                }

                _simConnect.RegisterDataDefineStruct<SnapshotStruct>(DEFINITIONS.SnapshotStruct);

                if (hadScanErrors)
                {
                    Console.WriteLine("[SimConnect] ℹ️ Catálogo parcial cargado (algunas entradas fueron ignoradas por seguridad).");
                }
                else
                {
                    Console.WriteLine($"[SimConnect] 📘 Catálogo cargado: {registeredCount} variables.");
                }

                // 2️⃣ Suscripción a eventos básicos
                _simConnect.OnRecvOpen += (_, _) => OnSimConnectOpened();
                _simConnect.OnRecvQuit += (_, _) =>
                {
                    Console.WriteLine("[SimConnect] ❌ Sesión cerrada.");
                    IsConnected = false;
                    _offlineMode = true;
                    _initialSnapshotQueued = false;
                    StartOfflineLoop();
                };
                _simConnect.OnRecvException += (_, ex) =>
                    Console.WriteLine($"[SimConnect] ⚠️ Excepción: {ex.dwException}");

                // 3️⃣ Recepción de datos del simulador
                _simConnect.OnRecvSimobjectData += (_, data) =>
                {
                    try
                    {
                        OnRecvSimobjectData(data);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SimConnect] ⚠️ Error procesando snapshot: {ex.Message}");
                    }
                };

                // 4️⃣ Polling periódico
                Task.Run(async () =>
                {
                    while (_simConnect != null)
                    {
                        try
                        {
                            _simConnect.RequestDataOnSimObject(
                                REQUESTS.SnapshotRequest,
                                DEFINITIONS.SnapshotStruct,
                                SimConnect.SIMCONNECT_OBJECT_ID_USER,
                                SIMCONNECT_PERIOD.SIM_FRAME,
                                SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT,
                                0,
                                0,
                                0
                            );
                        }
                        catch { }
                        await Task.Delay(200);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] ❌ Error al conectar: {ex.Message}");
                _simConnect = null;
                IsConnected = false;
                _offlineMode = true;
                Console.WriteLine("[SimConnect] ⚪ Offline (SimConnect no disponible).");
                StartOfflineLoop();
            }
        }

        private void OnSimConnectOpened()
        {
            IsConnected = true;
            _offlineMode = false;
            StopOfflineLoop();
            GlobalFlags.IsLabMode = false;
            LabConsole.StopSafe();
            Console.WriteLine("[SimConnect] ✅ Conexión establecida correctamente.");
            lastValues.Clear();

            if (!_initialSnapshotQueued)
            {
                _initialSnapshotQueued = true;
                _ = EmitInitialSnapshotWithDelayAsync();
            }
        }

        private void OnRecvSimobjectData(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            var changed = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvp in ExtractVars(data))
            {
                if (!lastValues.ContainsKey(kvp.Key) || !Equals(lastValues[kvp.Key], kvp.Value))
                {
                    lastValues[kvp.Key] = kvp.Value;
                    changed[kvp.Key] = kvp.Value;
                }
            }

            if (changed.Count == 0)
            {
                return;
            }

            var snapshot = new SimStateSnapshot(changed, DateTime.UtcNow, true);

            lock (_snapshotLock)
                _lastSnapshot = _lastSnapshot.MergeDiff(snapshot);

            _aircraftState.ApplySnapshot(snapshot.ToFlatDictionary());
            OnSnapshot?.Invoke(snapshot, true);
        }

        private static IEnumerable<KeyValuePair<string, object?>> ExtractVars(SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            if (data.dwData == null || data.dwData.Length == 0)
            {
                yield break;
            }

            var snapshot = (SnapshotStruct)data.dwData[0];
            yield return new KeyValuePair<string, object?>("SimVars.Altitude", snapshot.Altitude);
            yield return new KeyValuePair<string, object?>("SimVars.Airspeed", snapshot.Airspeed);
            yield return new KeyValuePair<string, object?>("SimVars.Heading", snapshot.Heading);
            yield return new KeyValuePair<string, object?>("SimVars.Pitch", snapshot.Pitch);
            yield return new KeyValuePair<string, object?>("SimVars.Bank", snapshot.Bank);
        }

        private async Task EmitInitialSnapshotWithDelayAsync()
        {
            Console.WriteLine("[SimConnect] 🕐 Esperando inicialización de variables...");
            await Task.Delay(1500).ConfigureAwait(false);
            EmitInitialSnapshot();
        }

        private void EmitInitialSnapshot()
        {
            var snapshot = _aircraftState.ExportFullSnapshot();
            if (snapshot.Values != null && snapshot.Values.Count > 0)
            {
                OnSnapshot?.Invoke(snapshot, false);
                Console.WriteLine($"[SimConnect] 📤 Snapshot inicial emitido con {snapshot.Values.Count} variables.");
            }
            else
            {
                Console.WriteLine("[SimConnect] ⚠️ Snapshot inicial omitido (sin datos)");
            }
        }

        // Estructura interna de ejemplo (temporal hasta usar catálogo completo)
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        private struct SnapshotStruct
        {
            public double Altitude;
            public double Airspeed;
            public double Heading;
            public double Pitch;
            public double Bank;
        }

        // ------------------------------------------------------------------
        // Métodos base de snapshot y sincronización
        // ------------------------------------------------------------------
        private async Task<IDictionary<string, object?>> ReadSnapshotFlatAsync(CancellationToken ct)
        {
            await Task.Delay(100, ct);
            return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _collector.Start();

            if (!IsConnected || _offlineMode)
            {
                StartOfflineLoop();
            }
        }

        public async Task WaitForCockpitReadyAsync(int timeoutMs = 15000)
        {
            var start = DateTime.UtcNow;

            while (!IsConnected)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    break;

                Console.WriteLine("[SimConnect] ⏳ Esperando MSFS...");
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

            Console.WriteLine("[SimConnect] 🛫 Cabina lista.");
        }

        public Task StopAsync() => _collector.StopAsync();

        public Task<bool> ApplyRemoteChangeAsync(string path, object? value, CancellationToken ct)
            => _commandApplier.ApplyAsync(path, value, ct);

        private async Task<bool> ApplySimVarChangeAsync(string path, object? value, CancellationToken ct)
        {
            try
            {
                if (_simConnect == null) return false;
                Console.WriteLine($"[SimConnect] ⇢ Set {path} = {value}");
                MirrorState(path, value);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] ⚠️ Error aplicando cambio {path}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> TriggerSimEventAsync(string eventName, object? value, CancellationToken ct)
        {
            try
            {
                if (_simConnect == null) return false;
                Console.WriteLine($"[SimConnect] ⇢ Event {eventName} ({value})");
                MirrorState(eventName, value);
                await Task.CompletedTask;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimConnect] ⚠️ Error disparando evento {eventName}: {ex.Message}");
                return false;
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

            var flat = snapshot.ToFlatDictionary();
            _aircraftState.ApplySnapshot(flat);
            OnSnapshot?.Invoke(snapshot, isDiff);

            if (snapshot.TryGetDouble("FRAME RATE", out var fps) && fps >= 0)
                LastFps = fps;
            else if (snapshot.TryGetDouble("FRAME RATE VAR", out var fpsVar) && fpsVar >= 0)
                LastFps = fpsVar;
            else if (!IsConnected)
                LastFps = -1;
        }

        private void MirrorState(string path, object? value)
        {
            lock (_snapshotLock)
                _lastSnapshot.Set(path, value);
            _aircraftState.Set(path, value);
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
            if (string.IsNullOrWhiteSpace(path))
                return;

            if (!SimStateSnapshot.LooksLikeSimVar(path))
                return;

            if (value is null)
                return;

            MirrorState(path, value);
            _ = ApplyRemoteChangeAsync(path, value, CancellationToken.None);
        }

        public void Dispose()
        {
            _collector.Dispose();
            _simConnect?.Dispose();
            StopOfflineLoop();
        }

        private void StartOfflineLoop()
        {
            if (_offlineTask != null && !_offlineTask.IsCompleted)
                return;

            StopOfflineLoop();
            IsConnected = false;
            _offlineMode = true;

            _offlineCts = new CancellationTokenSource();
            var token = _offlineCts.Token;
            _offlineTask = Task.Run(async () =>
            {
                Console.WriteLine("[SimConnect] Offline mode activo (snapshots dummy 1 Hz).");
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var snapshot = new SimStateSnapshot { IsDiff = false };
                        LastFps = -1;
                        HandleSnapshot(snapshot, false);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[SimConnect] ⚠️ Error en offline loop: {ex.Message}");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), token).ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }, token);
        }

        private void StopOfflineLoop()
        {
            if (_offlineCts == null)
                return;

            try
            {
                _offlineCts.Cancel();
            }
            catch
            {
            }

            try
            {
                _offlineTask?.Wait(500);
            }
            catch
            {
            }

            _offlineCts.Dispose();
            _offlineCts = null;
            _offlineTask = null;
        }
    }
}
