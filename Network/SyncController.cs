using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Walkaround;

namespace SharedCockpitClient.Network
{
    public interface INetworkBus
    {
        Task SendAsync(string json);
        event Action<string>? OnMessage;
    }

    public sealed class SyncController : IDisposable
    {
        private readonly INetworkBus _bus;
        private readonly AircraftStateManager? _aircraftState;
        private readonly SimConnectManager? _sim;
        private WalkaroundSync? _walkaround;

        private readonly Guid _localInstanceId = Guid.NewGuid();
        private int _localSequence;
        private readonly ConcurrentDictionary<Guid, int> _lastSeqByOrigin = new();
        private readonly ConcurrentDictionary<string, long> _suppressedProps = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, ThrottleState> _throttle = new(StringComparer.OrdinalIgnoreCase);
        private long _serverTimeBias;

        public Guid LocalInstanceId => _localInstanceId;

        public SyncController(INetworkBus bus, AircraftStateManager? stateManager = null, SimConnectManager? simManager = null, WalkaroundSync? walkaroundSync = null)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _aircraftState = stateManager;
            _sim = simManager;
            _walkaround = walkaroundSync;

            _bus.OnMessage += HandleIncomingJson;

            if (_aircraftState != null)
            {
                _aircraftState.OnPropertyChanged += async (prop, value) =>
                {
                    if (ShouldSuppress(prop))
                        return;

                    var descriptor = SimDataDefinition.TryGetVar(prop, out var varDescriptor) ? varDescriptor : null;
                    if (!ShouldSendNow(prop, value, descriptor))
                        return;

                    var message = NewStateMessage(prop, value);
                    Console.WriteLine($"[Send] {prop} = {value} seq={message.sequence} origin={message.originId}");
                    await SendAsync(message).ConfigureAwait(false);
                };
            }
        }

        public void AttachWalkaround(WalkaroundSync walkaround)
        {
            _walkaround = walkaround ?? throw new ArgumentNullException(nameof(walkaround));
        }

        public void Dispose()
        {
            _bus.OnMessage -= HandleIncomingJson;
        }

        private bool ShouldSuppress(string prop)
        {
            if (_suppressedProps.TryRemove(prop, out _))
            {
                return true;
            }

            return false;
        }

        private bool ShouldSendNow(string prop, object? value, SimVarDescriptor? descriptor)
        {
            var throttle = _throttle.GetOrAdd(prop, _ => new ThrottleState());
            lock (throttle)
            {
                var now = DateTime.UtcNow;
                var minInterval = descriptor != null && descriptor.Category.Equals("Controls", StringComparison.OrdinalIgnoreCase)
                    ? TimeSpan.FromMilliseconds(50)
                    : TimeSpan.FromMilliseconds(120);

                if (now - throttle.LastSentUtc < minInterval)
                {
                    if (descriptor?.MinDelta is double minDelta && TryAsDouble(value, out var newVal) && TryAsDouble(throttle.LastValue, out var lastVal))
                    {
                        if (Math.Abs(newVal - lastVal) < minDelta)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                throttle.LastSentUtc = now;
                throttle.LastValue = value;
                return true;
            }
        }

        private StateChangeMessage NewStateMessage(string prop, object? value)
        {
            var seq = Interlocked.Increment(ref _localSequence);
            var json = JsonSerializer.SerializeToElement(value, value?.GetType() ?? typeof(object));
            return new StateChangeMessage
            {
                prop = prop,
                value = json,
                originId = _localInstanceId,
                sequence = seq,
                serverTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
        }

        private Task SendAsync(StateChangeMessage msg)
        {
            var json = JsonSerializer.Serialize(msg);
            return _bus.SendAsync(json);
        }

        private void HandleIncomingJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var type = doc.RootElement.GetProperty("type").GetString();
                switch (type)
                {
                    case SyncMessageTypes.StateChange:
                        var stateMsg = JsonSerializer.Deserialize<StateChangeMessage>(json);
                        if (stateMsg != null)
                        {
                            HandleStateChange(stateMsg);
                        }
                        break;
                    case SyncMessageTypes.AvatarPose:
                        var avatarMsg = JsonSerializer.Deserialize<AvatarPoseMessage>(json);
                        if (avatarMsg != null)
                        {
                            _walkaround?.ApplyRemotePose(avatarMsg);
                        }
                        break;
                    case SyncMessageTypes.Session:
                        var session = JsonSerializer.Deserialize<SessionMessage>(json);
                        if (session != null)
                        {
                            _walkaround?.ApplySession(session);
                        }
                        break;
                    case SyncMessageTypes.Snapshot:
                        var snapshot = JsonSerializer.Deserialize<SnapshotMessage>(json);
                        if (snapshot != null)
                        {
                            HandleSnapshotMessage(snapshot);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Sync] Error procesando mensaje: {ex.Message}");
            }
        }

        private void HandleStateChange(StateChangeMessage msg)
        {
            if (msg.originId == _localInstanceId)
                return;

            var lastSeen = _lastSeqByOrigin.GetOrAdd(msg.originId, -1);
            if (msg.sequence <= lastSeen)
                return;

            _lastSeqByOrigin[msg.originId] = msg.sequence;
            _serverTimeBias = msg.serverTime - DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var value = ConvertJsonValue(msg.value);
            if (value is Dictionary<string, object?> opDict && opDict.TryGetValue("op", out var op) && string.Equals(Convert.ToString(op), "toggle", StringComparison.OrdinalIgnoreCase))
            {
                var current = _aircraftState?.Get(msg.prop);
                value = current is bool b ? !b : true;
            }
            Console.WriteLine($"[RemoteChange] {msg.prop} = {value} from={msg.originId} seq={msg.sequence}");

            _suppressedProps[msg.prop] = msg.sequence;

            if (_sim != null)
            {
                _ = _sim.ApplyRemoteChangeAsync(msg.prop, value, CancellationToken.None);
            }
            else
            {
                _aircraftState?.Set(msg.prop, value);
            }
        }

        private void HandleSnapshotMessage(SnapshotMessage msg)
        {
            if (msg.originId == _localInstanceId)
                return;

            foreach (var kv in msg.state)
            {
                _suppressedProps[kv.Key] = msg.serverTime;
                _aircraftState?.Set(kv.Key, kv.Value);
            }
        }

        private static object? ConvertJsonValue(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Null => null,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Object => JsonSerializer.Deserialize<Dictionary<string, object?>>(element.GetRawText()),
                _ => element.GetRawText()
            };
        }

        private static bool TryAsDouble(object? value, out double result)
        {
            switch (value)
            {
                case null:
                    result = 0;
                    return false;
                case double d:
                    result = d;
                    return true;
                case float f:
                    result = f;
                    return true;
                case int i:
                    result = i;
                    return true;
                case long l:
                    result = l;
                    return true;
                case decimal m:
                    result = (double)m;
                    return true;
                case string s when double.TryParse(s, out var parsed):
                    result = parsed;
                    return true;
                default:
                    result = 0;
                    return false;
            }
        }

        private sealed class ThrottleState
        {
            public DateTime LastSentUtc { get; set; } = DateTime.MinValue;
            public object? LastValue { get; set; }
        }
    }
}
