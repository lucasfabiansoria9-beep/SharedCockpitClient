#if SIMCONNECT_STUB
#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.FlightSimulator.SimConnect
{
    // -------------------------------------------------------------------------
    // ----- BEGIN SIMCONNECT STUB (used when the native SimConnect.dll is missing)
    // -------------------------------------------------------------------------
    /// <summary>
    /// Minimal managed stub for the Microsoft Flight Simulator SimConnect API.
    /// Provides enough surface area for offline compilation and laboratory mode execution.
    /// </summary>
    public class SimConnect : IDisposable
    {
        public const uint SIMCONNECT_OBJECT_ID_USER = 0;
        public const uint SIMCONNECT_UNUSED = 0xFFFFFFFF;

        private bool _disposed;

        public SimConnect(string name, IntPtr hWnd, uint userEventWin32, object? hEventHandle, uint configIndex)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));

            // Simulate an asynchronous OPEN notification to preserve initialization flow.
            Task.Run(() => OnRecvOpen?.Invoke(this, EventArgs.Empty));
        }

        public string Name { get; }

        public static bool IsStub { get; } = true;

        public event EventHandler? OnRecvOpen;
        public event EventHandler? OnRecvQuit;
        public event EventHandler<SIMCONNECT_RECV_EXCEPTION>? OnRecvException;
        public event EventHandler<SIMCONNECT_RECV_SIMOBJECT_DATA>? OnRecvSimobjectData;
        public event EventHandler<SIMCONNECT_RECV_EVENT>? OnRecvEvent;

        public void AddToDataDefinition(
            SIMCONNECT_DATA_DEFINITION_ID defineId,
            string datumName,
            string unitsName,
            SIMCONNECT_DATATYPE datumType = SIMCONNECT_DATATYPE.FLOAT64,
            float epsilon = 0,
            uint datumId = SIMCONNECT_UNUSED)
        {
            // Stubbed: definitions are tracked implicitly.
        }

        public void RegisterDataDefineStruct<T>(SIMCONNECT_DATA_DEFINITION_ID defineId)
        {
            // Stubbed: no-op to satisfy generic API contract.
        }

        public void RequestDataOnSimObject(
            SIMCONNECT_DATA_REQUEST_ID requestId,
            SIMCONNECT_DATA_DEFINITION_ID defineId,
            uint objectId,
            SIMCONNECT_PERIOD period,
            SIMCONNECT_DATA_REQUEST_FLAG flags,
            uint origin,
            uint interval,
            uint limit)
        {
            // Stubbed: real scheduling not available without native SimConnect runtime.
        }

        public void RequestDataOnSimObjectType(
            SIMCONNECT_DATA_REQUEST_ID requestId,
            SIMCONNECT_DATA_DEFINITION_ID defineId,
            uint radiusMeters,
            SIMCONNECT_SIMOBJECT_TYPE type)
        {
            // Stubbed.
        }

        public void MapClientEventToSimEvent(SIMCONNECT_CLIENT_EVENT_ID clientEventId, string eventName)
        {
            // Stubbed.
        }

        public void AddClientEventToNotificationGroup(
            SIMCONNECT_NOTIFICATION_GROUP_ID groupId,
            SIMCONNECT_CLIENT_EVENT_ID clientEventId,
            bool maskable)
        {
            // Stubbed.
        }

        public void SetNotificationGroupPriority(SIMCONNECT_NOTIFICATION_GROUP_ID groupId, uint priority)
        {
            // Stubbed.
        }

        public void SetDataOnSimObject(
            SIMCONNECT_DATA_DEFINITION_ID defineId,
            uint objectId,
            SIMCONNECT_DATA_SET_FLAG flags,
            object dataSet)
        {
            // Stubbed.
        }

        public void TransmitClientEvent(
            uint objectId,
            SIMCONNECT_CLIENT_EVENT_ID clientEventId,
            uint data,
            SIMCONNECT_NOTIFICATION_GROUP_ID groupId,
            SIMCONNECT_EVENT_FLAG flags)
        {
            // Stubbed.
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            OnRecvQuit?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Allows tests to simulate a SimConnect data callback when running with the stub.
        /// </summary>
        public void RaiseSimobjectData(uint requestId, params object?[] data)
        {
            var payload = data?.Cast<object>().ToArray() ?? Array.Empty<object>();
            OnRecvSimobjectData?.Invoke(this, new SIMCONNECT_RECV_SIMOBJECT_DATA
            {
                dwRequestID = requestId,
                dwData = payload
            });
        }

        /// <summary>
        /// Allows tests to simulate a SimConnect event callback when running with the stub.
        /// </summary>
        public void RaiseEvent(uint eventId, uint data = 0)
        {
            OnRecvEvent?.Invoke(this, new SIMCONNECT_RECV_EVENT
            {
                uEventID = (SIMCONNECT_CLIENT_EVENT_ID)eventId,
                dwData = data
            });
        }

        public void RaiseException(uint exceptionCode)
        {
            OnRecvException?.Invoke(this, new SIMCONNECT_RECV_EXCEPTION
            {
                dwException = exceptionCode
            });
        }
    }

    public enum SIMCONNECT_DATATYPE : uint
    {
        INVALID = 0,
        INT32 = 1,
        INT64 = 2,
        FLOAT32 = 3,
        FLOAT64 = 4,
        STRING8 = 5,
        STRING64 = 6,
        STRING128 = 7,
        STRING256 = 8
    }

    public enum SIMCONNECT_PERIOD : uint
    {
        NEVER = 0,
        ONCE = 1,
        VISUAL_FRAME = 2,
        SIM_FRAME = 3,
        SECOND = 4
    }

    [Flags]
    public enum SIMCONNECT_DATA_REQUEST_FLAG : uint
    {
        DEFAULT = 0,
        CHANGED = 1,
        TAGGED = 2
    }

    public enum SIMCONNECT_SIMOBJECT_TYPE : uint
    {
        USER = 0,
        ALL = 1
    }

    public enum SIMCONNECT_DATA_SET_FLAG : uint
    {
        DEFAULT = 0
    }

    public enum SIMCONNECT_GROUP_PRIORITY : uint
    {
        HIGHEST = 1,
        HIGHEST_MASKABLE = 10000000,
        STANDARD = 1900000000,
        DEFAULT = 2000000000,
        LOWEST = 4000000000
    }

    [Flags]
    public enum SIMCONNECT_EVENT_FLAG : uint
    {
        DEFAULT = 0,
        GROUPID_IS_PRIORITY = 0x10
    }

    public enum SIMCONNECT_DATA_DEFINITION_ID : uint
    {
        Dummy = 0
    }

    public enum SIMCONNECT_DATA_REQUEST_ID : uint
    {
        Dummy = 0
    }

    public enum SIMCONNECT_CLIENT_EVENT_ID : uint
    {
        Dummy = 0
    }

    public enum SIMCONNECT_NOTIFICATION_GROUP_ID : uint
    {
        Dummy = 0
    }

    public struct SIMCONNECT_RECV_EVENT
    {
        public SIMCONNECT_CLIENT_EVENT_ID uEventID;
        public uint dwData;
    }

    public struct SIMCONNECT_RECV_SIMOBJECT_DATA
    {
        public uint dwRequestID;
        public SIMCONNECT_DATA_DEFINITION_ID dwDefineID;
        public uint dwFlags;
        public uint dwentrynumber;
        public uint dwoutof;
        public uint dwDefineCount;
        public object[] dwData;
    }

    public struct SIMCONNECT_RECV_EXCEPTION
    {
        public uint dwException;
        public uint dwSendID;
        public SIMCONNECT_EXCEPTION dwExceptionEnum;
    }

    public enum SIMCONNECT_EXCEPTION : uint
    {
        NONE = 0
    }
    // -------------------------------------------------------------------------
    // ----- END SIMCONNECT STUB -----------------------------------------------
    // -------------------------------------------------------------------------
}
#endif
