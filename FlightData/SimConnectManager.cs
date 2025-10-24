using System;
using System.Runtime.InteropServices;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimConnectManager : IDisposable
{
    private const int WM_USER_SIMCONNECT = 0x0402;

    private SimConnect? _simconnect;
    private IntPtr _windowHandle = IntPtr.Zero;
    private SimDataCollector? _collector;
    private SimCommandApplier? _commandApplier;
    private readonly SimDiffEngine _diffEngine = new();
    private readonly object _stateLock = new();
    private SimStateSnapshot _latestSnapshot = new();
    private string _localRole = "pilot";
    private bool _disposed;

    public event Action<string>? OnSimStateChanged;

    public bool Initialize()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(SimConnectManager));
        }

        _windowHandle = CreateHiddenSimConnectWindow();

        try
        {
            _simconnect = new SimConnect("SharedCockpitClient", _windowHandle, WM_USER_SIMCONNECT, null, 0);
            _simconnect.OnRecvOpen += OnSimConnectOpen;
            _simconnect.OnRecvQuit += OnSimConnectQuit;
            _simconnect.OnRecvException += OnSimConnectException;

            _collector = new SimDataCollector(_simconnect);
            _collector.OnSnapshot += HandleSnapshot;

            _commandApplier = new SimCommandApplier(_simconnect);

            _collector.Start();

            Logger.Info("✅ SimConnect inicializado correctamente.");
            return true;
        }
        catch (Exception ex)
        {
            Logger.Error($"❌ No se pudo conectar a SimConnect: {ex.Message}");
            Cleanup();
            return false;
        }
    }

    public void ReceiveMessage()
    {
        _simconnect?.ReceiveMessage();
    }

    public void ApplyRemoteCommand(string json)
    {
        if (_commandApplier == null)
        {
            return;
        }

        var updated = _commandApplier.Apply(json, GetSnapshotClone, UpdateSnapshot);
        if (updated != null)
        {
            _diffEngine.CommitExternalState(updated.ToDictionary());
        }
    }

    public string? GetFullSnapshotPayload()
    {
        var snapshot = GetSnapshotClone();
        var state = snapshot.ToDictionary();

        if (state.Count == 0)
        {
            return null;
        }

        return _diffEngine.ComputeDiff(_localRole, state, forceFull: true);
    }

    public string LocalRole
    {
        get
        {
            lock (_stateLock)
            {
                return _localRole;
            }
        }
    }

    public void SetUserRole(string role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return;
        }

        var normalized = role.Equals("copilot", StringComparison.OrdinalIgnoreCase) ? "copilot" : "pilot";

        lock (_stateLock)
        {
            if (string.Equals(_localRole, normalized, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            _localRole = normalized;
        }

        Logger.Info($"🧭 Rol local establecido como {_localRole.ToUpperInvariant()}.");
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Cleanup();
    }

    private void HandleSnapshot(SimStateSnapshot snapshot)
    {
        lock (_stateLock)
        {
            _latestSnapshot = snapshot.Clone();
        }

        var state = snapshot.ToDictionary();
        var payload = _diffEngine.ComputeDiff(_localRole, state);
        if (!string.IsNullOrWhiteSpace(payload))
        {
            OnSimStateChanged?.Invoke(payload);
        }
    }

    private SimStateSnapshot GetSnapshotClone()
    {
        lock (_stateLock)
        {
            return _latestSnapshot.Clone();
        }
    }

    private void UpdateSnapshot(SimStateSnapshot snapshot)
    {
        lock (_stateLock)
        {
            _latestSnapshot = snapshot.Clone();
        }
    }

    private void OnSimConnectOpen(SimConnect sender, SIMCONNECT_RECV_OPEN data)
    {
        Logger.Info($"🟢 Conectado a SimConnect ({data.szApplicationName}).");
    }

    private void OnSimConnectQuit(SimConnect sender, SIMCONNECT_RECV data)
    {
        Logger.Warn("🔴 El simulador se cerró. Deteniendo sincronización.");
    }

    private void OnSimConnectException(SimConnect sender, SIMCONNECT_RECV_EXCEPTION data)
    {
        Logger.Warn($"⚠️ Excepción de SimConnect: {data.dwException}.");
    }

    private void Cleanup()
    {
        if (_collector != null)
        {
            _collector.OnSnapshot -= HandleSnapshot;
            _collector.Dispose();
            _collector = null;
        }

        if (_simconnect != null)
        {
            _simconnect.OnRecvOpen -= OnSimConnectOpen;
            _simconnect.OnRecvQuit -= OnSimConnectQuit;
            _simconnect.OnRecvException -= OnSimConnectException;
            _simconnect.Dispose();
            _simconnect = null;
        }

        _commandApplier = null;
        _windowHandle = IntPtr.Zero;
    }

    private static IntPtr CreateHiddenSimConnectWindow()
    {
        var handle = CreateWindowEx(0, "Static", "SharedCockpitClient_SimConnect", 0,
            0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        if (handle == IntPtr.Zero)
        {
            Logger.Warn($"⚠️ No se pudo crear la ventana oculta para SimConnect (Error {Marshal.GetLastWin32Error()}).");
        }

        return handle;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
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
}
