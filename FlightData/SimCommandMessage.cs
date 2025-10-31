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

        public object? Value { get; set; }
            = null;

        public string? Target { get; set; }
            = null;

        public string OriginId { get; set; }
            = string.Empty;

        public long Sequence { get; set; }
            = 0;

        public double ServerTime { get; set; }
            = 0;

        public string NormalizedCommand => string.IsNullOrWhiteSpace(Command)
            ? string.Empty
            : SimDataDefinition.NormalizeEventName(Command);
    }
}
