// ================================================================
// 🔧 SimConnectMissingEnumsShim.cs
// Corrige errores CS0246 por tipos faltantes del SDK de SimConnect
// ================================================================

namespace SharedCockpitClient
{
    internal enum SIMCONNECT_DATA_DEFINITION_ID : uint
    {
        DEFINITION_1,
        DEFINITION_2
    }

    internal enum SIMCONNECT_CLIENT_EVENT_ID : uint
    {
        EVENT_1,
        EVENT_2
    }

    internal enum SIMCONNECT_DATA_REQUEST_ID : uint
    {
        REQUEST_1,
        REQUEST_2
    }

    internal enum SIMCONNECT_NOTIFICATION_GROUP_ID : uint
    {
        GROUP_1,
        GROUP_2
    }

    internal enum SIMCONNECT_GROUP_PRIORITY : uint
    {
        HIGHEST,
        STANDARD,
        LOWEST
    }

    internal enum SIMCONNECT_EVENT_FLAG : uint
    {
        DEFAULT,
        GROUPID_IS_PRIORITY
    }

    internal enum SIMCONNECT_DATA_SET_FLAG : uint
    {
        DEFAULT
    }
}
