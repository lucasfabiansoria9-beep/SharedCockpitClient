#nullable enable

using System;

namespace SharedCockpitClient
{
    /// <summary>
    /// Representa un comando discreto de cabina (SimEvent) emitido por el host/local para sincronizarse.
    /// </summary>
    public sealed class SimCommandMessage
    {
        public string Command { get; set; } = string.Empty;

        public Guid OriginId { get; set; }
            = Guid.Empty;

        public long Sequence { get; set; }
            = 0;

        public long Timestamp { get; set; }
            = 0;

        public object? Value { get; set; }
            = null;

        public string? Target { get; set; }
            = null;

        public string NormalizedCommand => string.IsNullOrWhiteSpace(Command)
            ? string.Empty
            : SimDataDefinition.NormalizeEventName(Command);
    }
}
