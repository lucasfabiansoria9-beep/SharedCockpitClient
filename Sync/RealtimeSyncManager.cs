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
        private DateTime _firstSnapshotUtc = DateTime.MinValue;
        private const int WARMUP_MS = 1000;

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

            string? payload = null;
            int diffCount = 0;
            string logLine;

            lock (syncLock)
            {
                if (_firstSnapshotUtc == DateTime.MinValue)
                    _firstSnapshotUtc = DateTime.UtcNow;

                if (current.IsDiff && lastSnapshot != null)
                    current = lastSnapshot.MergeDiff(current);
                else
                    current = current.Clone();

                var diff = CalculateDiff(lastSnapshot, current);
                foreach (var key in diff.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList())
                    diff.Remove(key);

                var warmup = (DateTime.UtcNow - _firstSnapshotUtc).TotalMilliseconds < WARMUP_MS;
                if (warmup || diff.Count == 0)
                {
                    lastSnapshot = current.Clone();
                    return;
                }

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
                lastSnapshot = current.Clone();
            }

            if (payload != null)
            {
                _ = websocket.SendAsync(payload);
                Log(logLine);
            }
        }

        public void ApplyRemoteDiff(string? role, Dictionary<string, object?> diff)
        {
            if (diff == null || diff.Count == 0)
                return;

            Dictionary<string, object?>? filtered = null;
            string logLine;

            lock (syncLock)
            {
                if (lastSnapshot == null)
                    lastSnapshot = new SimStateSnapshot();

                filtered = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var kv in diff)
                {
                    if (!SimStateSnapshot.LooksLikeSimVar(kv.Key))
                        continue;
                    if (kv.Value is null)
                        continue;

                    lastSnapshot.Set(kv.Key, kv.Value);
                    filtered[kv.Key] = kv.Value;
                }

                if (filtered.Count == 0)
                    return;

                RecordDiffSample(filtered.Count);
                logLine = FormatLogLine(role ?? "REMOTE", "Received", filtered.Keys, filtered.Count);
            }

            foreach (var kv in filtered)
            {
                simManager.ApplySimVar(kv.Key, kv.Value);
            }

            Log(logLine);
        }

        private Dictionary<string, object?> CalculateDiff(SimStateSnapshot? previous, SimStateSnapshot current)
        {
            var diff = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            if (previous == null)
            {
                foreach (var kv in current.Data)
                {
                    if (!SimStateSnapshot.LooksLikeSimVar(kv.Key))
                        continue;
                    if (kv.Value is null)
                        continue;
                    diff[kv.Key] = kv.Value;
                }

                return diff;
            }

            foreach (var kv in current.Data)
            {
                if (!SimStateSnapshot.LooksLikeSimVar(kv.Key))
                    continue;

                var newVal = kv.Value;
                if (newVal is null)
                    continue;

                if (!previous.Data.TryGetValue(kv.Key, out var oldValue) || !Equals(oldValue, newVal))
                    diff[kv.Key] = newVal;
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

            return $"{roleTag} {direction} {totalCount} diffs ({keysText})";
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
        private static readonly object _consoleLock = new();
        private static DateTime _lastConsoleLog = DateTime.MinValue;
        private const bool LOG_TO_CONSOLE = false;

        private static void Log(string text, bool important = false)
        {
            var line = $"{DateTime.UtcNow:O} {text}";
            lock (logLock)
            {
                Directory.CreateDirectory("logs");
                File.AppendAllText(Path.Combine("logs", "realtime-sync.log"), line + Environment.NewLine);
            }

            if (LOG_TO_CONSOLE || important)
            {
                lock (_consoleLock)
                {
                    if (important || (DateTime.UtcNow - _lastConsoleLog).TotalSeconds >= 1)
                    {
                        Console.WriteLine(line);
                        _lastConsoleLog = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
