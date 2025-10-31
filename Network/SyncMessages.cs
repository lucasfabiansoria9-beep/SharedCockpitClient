using System;
using System.Collections.Generic;
using System.Text.Json;

namespace SharedCockpitClient
{
    public static class SyncMessageTypes
    {
        public const string StateChange = "stateChange";
        public const string AvatarPose = "avatarPose";
        public const string Session = "session";
        public const string Snapshot = "snapshot";
    }

    public sealed class StateChangeMessage
    {
        public string type { get; set; } = SyncMessageTypes.StateChange;
        public string prop { get; set; } = string.Empty;
        public JsonElement value { get; set; }
        public Guid originId { get; set; }
        public int sequence { get; set; }
        public long serverTime { get; set; }
    }

    public sealed class AvatarPoseMessage
    {
        public string type { get; set; } = SyncMessageTypes.AvatarPose;
        public Guid originId { get; set; }
        public int sequence { get; set; }
        public AvatarPose pose { get; set; } = new();
    }

    public sealed class AvatarPose
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public double alt { get; set; }
        public double hdg { get; set; }
        public double pitch { get; set; }
        public double bank { get; set; }
        public string state { get; set; } = "idle";
    }

    public sealed class SessionMessage
    {
        public string type { get; set; } = SyncMessageTypes.Session;
        public Guid originId { get; set; }
        public string role { get; set; } = "pilot";
        public string camera { get; set; } = "cabin";
        public Dictionary<string, object?>? metadata { get; set; }
    }

    public sealed class SnapshotMessage
    {
        public string type { get; set; } = SyncMessageTypes.Snapshot;
        public Guid originId { get; set; }
        public bool full { get; set; }
        public Dictionary<string, object?> state { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public long serverTime { get; set; }
    }
}
