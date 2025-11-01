// Este shim existe para garantizar que tipos usados por nuestro código
// (por ejemplo SIMCONNECT_DATA_DEFINITION_ID) existan siempre, incluso si
// la DLL de SimConnect real no los define exactamente igual.
// IMPORTANTE: Esto NO debe chocar con las definiciones reales si existen.
// Por eso usamos enums/clases ligeras y mantenemos los miembros mínimos
// que utiliza el cliente. Si la DLL real ya define estos tipos, se espera
// que sean compatibles con estas firmas.

#nullable enable
using System;

namespace Microsoft.FlightSimulator.SimConnect
{
    public enum SIMCONNECT_DATA_DEFINITION_ID : uint { }
    public enum SIMCONNECT_DATA_REQUEST_ID : uint { }
    public enum SIMCONNECT_CLIENT_EVENT_ID : uint { }
    public enum SIMCONNECT_NOTIFICATION_GROUP_ID : uint { }

    public enum SIMCONNECT_GROUP_PRIORITY : uint
    {
        HIGHEST = 1,
        DEFAULT = 1900000000
    }

    public enum SIMCONNECT_EVENT_FLAG : uint
    {
        DEFAULT = 0,
        GROUPID_IS_PRIORITY = 1
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
        DEFAULT = 0x00000000,
        CHANGED = 0x00000001
    }

    public enum SIMCONNECT_SIMOBJECT_TYPE : uint
    {
        USER = 0,
        ALL = 1
    }

    [Flags]
    public enum SIMCONNECT_DATA_SET_FLAG : uint
    {
        DEFAULT = 0x00000000
    }

    public class SIMCONNECT_RECV_OPEN
    {
    }

    public class SIMCONNECT_RECV
    {
    }

    public class SIMCONNECT_RECV_EVENT
    {
        public uint uEventID;
        public uint dwData;
    }

    public class SIMCONNECT_RECV_SIMOBJECT_DATA
    {
        public uint dwRequestID;
        public object? dwData;
    }

    public class SIMCONNECT_RECV_EXCEPTION
    {
        public uint dwException;
    }
}
