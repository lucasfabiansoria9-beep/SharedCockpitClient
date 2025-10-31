using System;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Representa un comando discreto de cabina (SimEvent) emitido por el host/local para sincronizarse.
    /// </summary>
    public sealed record SimCommandMessage(
        string EventName,
        Guid OriginId,
        long Sequence,
        long Timestamp,
        string? SourcePath = null,
        object? Value = null)
    {
        public string NormalizedEvent => SimDataDefinition.NormalizeEventName(EventName);
    }
}
