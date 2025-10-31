#nullable enable

using System;

namespace SharedCockpitClient
{
    /// <summary>
    /// Representa un comando discreto recibido/enviado por WebSocket.
    /// </summary>
    public sealed record CommandPayload(
        string Command,
        string? OriginId,
        long Sequence,
        double ServerTime,
        string? Target,
        object? Value)
    {
        public long Timestamp => (long)ServerTime;

        public string NormalizedCommand => string.IsNullOrWhiteSpace(Command)
            ? string.Empty
            : SimDataDefinition.NormalizeEventName(Command);
    }
}
