#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    public class RealtimeSyncManager : IDisposable
    {
        private readonly SimConnectManager simManager;
        private readonly WebSocketManager websocket;
        private SimStateSnapshot? lastSnapshot;
        private readonly object syncLock = new();
        private readonly Queue<(DateTime Timestamp, int Count)> diffSamples = new();
        private double currentDiffRate;
        private DateTime _firstSnapshotUtc = DateTime.MinValue;
        private const int WARMUP_MS = 1000;
        private readonly Dictionary<string, long> _lastSequenceByOrigin = new(StringComparer.OrdinalIgnoreCase);
        private readonly string _localInstanceId;
        private long _localSequence;

        public bool IsActive { get; private set; }

        public RealtimeSyncManager(SimConnectManager simManager, WebSocketManager websocket)
        {
            this.simManager = simManager ?? throw new ArgumentNullException(nameof(simManager));
            this.websocket = websocket ?? throw new ArgumentNullException(nameof(websocket));

            _localInstanceId = !string.IsNullOrWhiteSpace(simManager.LocalInstanceKey)
                ? simManager.LocalInstanceKey
                : simManager.LocalInstanceId.ToString("N");

            this.simManager.OnCommand += HandleLocalCommand;
            this.websocket.OnCommand += HandleRemoteCommand;
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
            string? logLine = null;
            Dictionary<string, object?>? diffCopy = null;
            long sequence = 0;

            lock (syncLock)
            {
                if (_firstSnapshotUtc == DateTime.MinValue)
                    _firstSnapshotUtc = DateTime.UtcNow;

                if (current.IsDiff && lastSnapshot != null)
                    current = lastSnapshot.MergeDiff(current);
                else
                    current = current.Clone();

                if (!IsActive)
                {
                    if ((DateTime.UtcNow - _firstSnapshotUtc).TotalMilliseconds > WARMUP_MS)
                    {
                        IsActive = true;
                        Logger.Info("[RealtimeSync] ✅ Sincronización activada");
                    }
                    else
                    {
                        lastSnapshot = current.Clone();
                        return;
                    }
                }

                var diff = CalculateDiff(lastSnapshot, current);
                foreach (var key in diff.Where(kv => kv.Value is null).Select(kv => kv.Key).ToList())
                    diff.Remove(key);

                if (diff.Count == 0)
                {
                    lastSnapshot = current.Clone();
                    return;
                }

                diffCopy = new Dictionary<string, object?>(diff, StringComparer.OrdinalIgnoreCase);
                sequence = Interlocked.Increment(ref _localSequence);
                payload = JsonSerializer.Serialize(new
                {
                    type = "stateDiff",
                    originId = _localInstanceId,
                    sequence,
                    timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    role,
                    diff = diffCopy
                });

                RecordDiffSample(diff.Count);
                logLine = FormatLogLine(role, "Sent", diff.Keys, diff.Count);
                lastSnapshot = current.Clone();
            }

            if (payload != null)
            {
                _ = websocket.SendAsync(payload);
                Log(logLine!);
                var surfaces = diffCopy != null ? BuildSurfaceLog(diffCopy) : null;
                if (!string.IsNullOrEmpty(surfaces))
                    Logger.Debug(surfaces);
            }
        }

        public void ApplyRemoteDiff(string? role, string? originId, long sequence, Dictionary<string, object?> diff)
        {
            if (diff == null || diff.Count == 0)
                return;

            Dictionary<string, object?>? filtered = null;
            string logLine;
            Guid originGuid = Guid.Empty;

            if (!string.IsNullOrWhiteSpace(originId))
            {
                if (string.Equals(originId, _localInstanceId, StringComparison.OrdinalIgnoreCase))
                    return;

                Guid.TryParse(originId, out originGuid);
            }

            lock (syncLock)
            {
                if (lastSnapshot == null)
                    lastSnapshot = new SimStateSnapshot();

                if (originGuid != Guid.Empty)
                {
                    if (_lastSequenceByOrigin.TryGetValue(originGuid, out var lastSeq) && sequence <= lastSeq)
                        return;

                    _lastSequenceByOrigin[originGuid] = sequence;
                }

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
            var surfaces = BuildSurfaceLog(filtered);
            if (!string.IsNullOrEmpty(surfaces))
                Logger.Debug(surfaces);
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

        private void HandleLocalCommand(SimCommandMessage command)
        {
            if (command == null)
                return;

            if (command.ServerTime <= 0)
                command.ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var sequence = Interlocked.Increment(ref _localSequence);
            var payload = JsonSerializer.Serialize(new
            {
                type = "command",
                originId = _localInstanceId,
                sequence,
                timestamp = command.ServerTime,
                serverTime = command.ServerTime,
                command = command.Command,
                target = command.Target,
                value = command.Value
            });

            Logger.Debug($"[RealtimeSync] 🛠 Control enviado: {command.NormalizedCommand}");
            _ = websocket.SendAsync(payload);
        }

        private void HandleRemoteCommand(CommandPayload payload)
        {
            if (payload == null)
                return;

            if (!string.IsNullOrWhiteSpace(payload.OriginId) &&
                string.Equals(payload.OriginId, _localInstanceId, StringComparison.OrdinalIgnoreCase))
                return;

            lock (syncLock)
            {
                if (!string.IsNullOrWhiteSpace(payload.OriginId))
                {
                    var origin = payload.OriginId!;
                    if (_lastSequenceByOrigin.TryGetValue(origin, out var lastSeq) && payload.Sequence <= lastSeq)
                        return;

                    _lastSequenceByOrigin[origin] = payload.Sequence;
                }
            }

            Logger.Debug($"[RealtimeSync] 🛠 Control recibido: {payload.Command} (from {payload.OriginId ?? "remote"})");
            var path = payload.Target;
            if (string.IsNullOrWhiteSpace(path))
                path = payload.NormalizedCommand;
            _ = simManager.ApplyRemoteChangeAsync(path, payload.Value, CancellationToken.None);
        }

        private static string? BuildSurfaceLog(IReadOnlyDictionary<string, object?> diff)
        {
            if (diff == null || diff.Count == 0)
                return null;

            var segments = new List<string>();

            if (TryFormatControl(diff, "SimVars.AILERON POSITION", out var aileron))
                segments.Add($"Aileron={aileron}");
            if (TryFormatControl(diff, "SimVars.ELEVATOR POSITION", out var elevator))
                segments.Add($"Elevator={elevator}");
            if (TryFormatControl(diff, "SimVars.RUDDER POSITION", out var rudder))
                segments.Add($"Rudder={rudder}");
            if (TryFormatControl(diff, "SimVars.FLAPS HANDLE INDEX", out var flaps, format: "0"))
                segments.Add($"Flaps={flaps}");
            if (TryFormatControl(diff, "SimVars.GEAR HANDLE POSITION", out var gear, format: "0"))
                segments.Add($"Gear={(gear == "0" ? "UP" : "DOWN")}");

            if (segments.Count == 0)
                return null;

            return "[RealtimeSync] 📡 Superficies actualizadas: " + string.Join(" | ", segments);
        }

        private static bool TryFormatControl(IReadOnlyDictionary<string, object?> diff, string key, out string formatted, string format = "0.00")
        {
            formatted = string.Empty;
            if (!diff.TryGetValue(key, out var value) || value is null)
                return false;

            switch (value)
            {
                case double d:
                    formatted = d.ToString(format);
                    return true;
                case float f:
                    formatted = f.ToString(format);
                    return true;
                case int i:
                    formatted = i.ToString(format);
                    return true;
                case long l:
                    formatted = l.ToString(format);
                    return true;
                case bool b:
                    formatted = b ? "1" : "0";
                    return true;
                default:
                    var str = Convert.ToString(value);
                    if (string.IsNullOrWhiteSpace(str))
                        return false;
                    formatted = str!;
                    return true;
            }
        }

        public void Dispose()
        {
            simManager.OnCommand -= HandleLocalCommand;
            websocket.OnCommand -= HandleRemoteCommand;
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
                        Logger.Info(line);
                        _lastConsoleLog = DateTime.UtcNow;
                    }
                }
            }
        }
    }
}
