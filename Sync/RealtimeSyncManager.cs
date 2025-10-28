using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;

namespace SharedCockpitClient.Sync
{
    public class RealtimeSyncManager
    {
        private readonly SimConnectManager simManager;
        private readonly WebSocketManager websocket;
        private SimStateSnapshot? lastSnapshot;
        private readonly object syncLock = new();
        private readonly Queue<(DateTime Timestamp, int Count)> diffSamples = new();
        private double currentDiffRate;

        public RealtimeSyncManager(SimConnectManager simManager, WebSocketManager websocket)
        {
            this.simManager = simManager ?? throw new ArgumentNullException(nameof(simManager));
            this.websocket = websocket ?? throw new ArgumentNullException(nameof(websocket));
        }

        public double CurrentDiffRate
        {
            get
            {
                lock (syncLock)
                {
                    return currentDiffRate;
                }
            }
        }

        public void UpdateAndSync(SimStateSnapshot current, string role)
        {
            if (current == null) throw new ArgumentNullException(nameof(current));
            role ??= "UNKNOWN";

            Dictionary<string, object?> diff;
            string payload;
            string logLine;
            int diffCount;

            lock (syncLock)
            {
                diff = CalculateDiff(lastSnapshot, current);
                lastSnapshot = current.Clone();

                if (diff.Count == 0)
                    return;

                diffCount = diff.Count;
                payload = JsonSerializer.Serialize(new
                {
                    type = "state-diff",
                    role,
                    diff,
                    timestamp = DateTime.UtcNow.Ticks
                });

                RecordDiffSample(diffCount);
                logLine = FormatLogLine(role, "Sent", diff.Keys, diffCount);
            }

            _ = websocket.SendAsync(payload);
            Log(logLine);
        }

        public void ApplyRemoteDiff(string? role, Dictionary<string, object?> diff)
        {
            if (diff == null || diff.Count == 0)
                return;

            foreach (var kv in diff)
            {
                simManager.ApplySimVar(kv.Key, kv.Value);
            }

            string logLine;
            var diffCount = diff.Count;
            lock (syncLock)
            {
                RecordDiffSample(diffCount);
                logLine = FormatLogLine(role ?? "REMOTE", "Received", diff.Keys, diffCount);
            }

            Log(logLine);
        }

        private Dictionary<string, object?> CalculateDiff(SimStateSnapshot? previous, SimStateSnapshot current)
        {
            if (previous == null)
            {
                return new Dictionary<string, object?>(current.Data, StringComparer.OrdinalIgnoreCase);
            }

            var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in current.Data)
            {
                if (!previous.Data.TryGetValue(kv.Key, out var oldValue) || !Equals(oldValue, kv.Value))
                {
                    diff[kv.Key] = kv.Value;
                }
            }

            return diff;
        }

        private void RecordDiffSample(int count)
        {
            var now = DateTime.UtcNow;
            diffSamples.Enqueue((now, count));

            while (diffSamples.Count > 0 && (now - diffSamples.Peek().Timestamp) > TimeSpan.FromSeconds(1))
            {
                diffSamples.Dequeue();
            }

            if (diffSamples.Count == 0)
            {
                currentDiffRate = 0;
                return;
            }

            var total = diffSamples.Sum(s => s.Count);
            var span = (now - diffSamples.Peek().Timestamp).TotalSeconds;
            if (span <= 0)
                span = 1;

            currentDiffRate = total / span;
        }

        private static string FormatLogLine(string role, string direction, IEnumerable<string> keys, int totalCount)
        {
            var roleTag = $"[{NormalizeRole(role)}]";
            var keyList = keys.Take(15).ToList();
            var keysText = keyList.Count == 0 ? "--" : string.Join(", ", keyList);
            if (totalCount > keyList.Count)
            {
                keysText += ", …";
            }

            return $"{DateTime.UtcNow:O} {roleTag} {direction} {totalCount} diffs ({keysText})";
        }

        private static string NormalizeRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                return "REMOTE";

            role = role.Trim().ToUpperInvariant();
            return role switch
            {
                "HOST" => "HOST",
                "CLIENT" => "CLIENT",
                "PILOT" => "PILOT",
                "COPILOT" => "COPILOT",
                _ => role
            };
        }

        private static readonly object logLock = new();

        private static void Log(string text)
        {
            lock (logLock)
            {
                Directory.CreateDirectory("logs");
                File.AppendAllText(Path.Combine("logs", "realtime-sync.log"), text + Environment.NewLine);
            }
        }
    }
}
