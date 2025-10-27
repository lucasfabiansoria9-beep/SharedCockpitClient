using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Network;

namespace SharedCockpitClient.Walkaround
{
    public sealed class WalkaroundSync
    {
        private readonly INetworkBus _bus;
        private readonly Guid _localId;
        private int _sequence;

        public WalkaroundSync(INetworkBus bus, Guid localId)
        {
            _bus = bus ?? throw new ArgumentNullException(nameof(bus));
            _localId = localId;
        }

        public event Action<AvatarPoseMessage>? OnRemotePose;
        public event Action<SessionMessage>? OnRemoteSession;

        public Task PublishPoseAsync(AvatarPose pose, CancellationToken ct)
        {
            if (pose == null) throw new ArgumentNullException(nameof(pose));
            var msg = new AvatarPoseMessage
            {
                originId = _localId,
                sequence = Interlocked.Increment(ref _sequence),
                pose = pose
            };

            var json = JsonSerializer.Serialize(msg);
            Console.WriteLine($"[AvatarPose] send lat={pose.lat} lon={pose.lon} state={pose.state}");
            return _bus.SendAsync(json);
        }

        public Task PublishSessionAsync(string role, string camera, CancellationToken ct)
        {
            var msg = new SessionMessage
            {
                originId = _localId,
                role = role,
                camera = camera,
                metadata = new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            };

            var json = JsonSerializer.Serialize(msg);
            Console.WriteLine($"[Session] send role={role} camera={camera}");
            return _bus.SendAsync(json);
        }

        public void ApplyRemotePose(AvatarPoseMessage message)
        {
            if (message.originId == _localId)
                return;

            Console.WriteLine($"[AvatarPose] recv id={message.originId} lat={message.pose.lat} state={message.pose.state}");
            OnRemotePose?.Invoke(message);
        }

        public void ApplySession(SessionMessage message)
        {
            if (message.originId == _localId)
                return;

            Console.WriteLine($"[Session] recv role={message.role} camera={message.camera}");
            OnRemoteSession?.Invoke(message);
        }
    }
}
