#nullable enable
using System;

namespace SharedCockpitClient.FlightData.Stubs
{
    // Shim mínimo y no conflictivo.
    // Evitar declarar enums que ya existen en Microsoft.FlightSimulator.SimConnect.
    public readonly struct SIMCONNECT_CLIENT_EVENT_ID
    {
        private readonly uint _value;
        public SIMCONNECT_CLIENT_EVENT_ID(uint value) => _value = value;
        public static implicit operator uint(SIMCONNECT_CLIENT_EVENT_ID id) => id._value;
        public static implicit operator SIMCONNECT_CLIENT_EVENT_ID(uint value) => new SIMCONNECT_CLIENT_EVENT_ID(value);
        public override string ToString() => _value.ToString();
    }

    public readonly struct SIMCONNECT_DATA_DEFINITION_ID
    {
        private readonly uint _value;
        public SIMCONNECT_DATA_DEFINITION_ID(uint value) => _value = value;
        public static implicit operator uint(SIMCONNECT_DATA_DEFINITION_ID id) => id._value;
        public static implicit operator SIMCONNECT_DATA_DEFINITION_ID(uint value) => new SIMCONNECT_DATA_DEFINITION_ID(value);
        public override string ToString() => _value.ToString();
    }

    public readonly struct SIMCONNECT_DATA_REQUEST_ID
    {
        private readonly uint _value;
        public SIMCONNECT_DATA_REQUEST_ID(uint value) => _value = value;
        public static implicit operator uint(SIMCONNECT_DATA_REQUEST_ID id) => id._value;
        public static implicit operator SIMCONNECT_DATA_REQUEST_ID(uint value) => new SIMCONNECT_DATA_REQUEST_ID(value);
        public override string ToString() => _value.ToString();
    }

    // Añade aquí tipos realmente ausentes si detectas alguno más,
    // siempre en este namespace para evitar colisiones con la DLL oficial.
}
