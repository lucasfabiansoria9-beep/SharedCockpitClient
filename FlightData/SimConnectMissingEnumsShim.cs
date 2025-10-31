// ================================================================
// 🔧 SimConnectMissingEnumsShim.cs
// Corrige errores CS0246 por tipos faltantes del SDK de SimConnect
// ================================================================

using System;

namespace SharedCockpitClient
{
    // --- Garantiza que el compilador siempre encuentre estos tipos ---
    internal enum SIMCONNECT_DATA_DEFINITION_ID
    {
        DEFINITION_1 = 0,
        DEFINITION_2,
        DEFINITION_3,
        DEFINITION_4,
        DEFINITION_5,
        DEFINITION_MAX
    }

    internal enum SIMCONNECT_CLIENT_EVENT_ID
    {
        EVENT_1 = 0,
        EVENT_2,
        EVENT_3,
        EVENT_4,
        EVENT_5,
        EVENT_MAX
    }
}
