#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    /// <summary>
    /// Contract for realtime network buses used by the synchronization layer.
    /// Provides message dispatching, diff propagation and command routing primitives.
    /// </summary>
    public interface INetworkBus
    {
        /// <summary>
        /// Raised when a raw message is received from the transport.
        /// </summary>
        event Action<string>? OnMessage;

        /// <summary>
        /// Raised when a remote state diff payload is received.
        /// </summary>
        event Action<string?, string?, long, Dictionary<string, object?>>? OnStateDiff;

        /// <summary>
        /// Raised when a remote command payload is received.
        /// </summary>
        event Action<CommandPayload>? OnCommand;

        /// <summary>
        /// Gets whether the transport is currently connected.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Average round-trip-time in milliseconds for heartbeat messages.
        /// </summary>
        double AverageRttMs { get; }

        /// <summary>
        /// Alias for AverageRttMs maintained for backwards compatibility.
        /// </summary>
        double AverageLagMs { get; }

        /// <summary>
        /// Task that completes once the transport is initialized and ready.
        /// </summary>
        Task Ready { get; }

        /// <summary>
        /// Starts the bus and begins listening for incoming messages.
        /// </summary>
        Task StartAsync(CancellationToken ct);

        /// <summary>
        /// Sends a payload to the current peer.
        /// </summary>
        Task SendAsync(string json);

        /// <summary>
        /// Broadcasts a payload to all connected peers. Host-only.
        /// </summary>
        Task BroadcastAsync(string json);
    }
}
