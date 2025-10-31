using System;
namespace SharedCockpitClient
{
    /// <summary>
    /// Representa un comando discreto recibido/enviado por WebSocket.
    /// </summary>
    public sealed record CommandPayload(
        string Event,
        string? OriginId,
        long Sequence,
        long Timestamp,
        string? Path,
        object? Value)
    {
        public string NormalizedEvent => string.IsNullOrWhiteSpace(Event)
            ? string.Empty
            : SimDataDefinition.NormalizeEventName(Event);
    }
}
