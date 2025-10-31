#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient
{
    public sealed class SnapshotStore
    {
        private readonly string? _customPath;
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private string _lastHash = string.Empty;
        private readonly Dictionary<string, object?> _lastSavedState = new(StringComparer.OrdinalIgnoreCase);

        public SnapshotStore(string? customPath = null)
        {
            _customPath = string.IsNullOrWhiteSpace(customPath) ? null : customPath;
        }

        public async Task<Dictionary<string, object?>> LoadAsync(CancellationToken ct)
        {
            var path = GetUserSnapshotPath();
            if (!File.Exists(path))
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            var json = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
            var snap = JsonSerializer.Deserialize<SimStateSnapshot>(json) ?? new SimStateSnapshot();
            snap.CompactInPlace();
            Logger.Info($"[SnapshotStore] Estado restaurado desde {path} ({snap.Data.Count} entradas)");
            return new Dictionary<string, object?>(snap.Data, StringComparer.OrdinalIgnoreCase);
        }

        public async Task SaveIfChangedAsync(SimStateSnapshot snap, CancellationToken ct)
        {
            if (snap == null)
                return;

            snap.CompactInPlace();

            if ((DateTime.UtcNow - _lastSaveUtc).TotalSeconds < 2)
                return;

            var json = JsonSerializer.Serialize(snap);
            using var md5 = MD5.Create();
            var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(json)));
            if (hash == _lastHash)
                return;

            var changes = CountChanges(snap.Values);
            if (changes == 0)
            {
                return;
            }

            _lastHash = hash;
            var path = GetUserSnapshotPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
            _lastSaveUtc = DateTime.UtcNow;
            Logger.Info($"[SnapshotStore] ✅ Guardado ({changes} variables modificadas)");
            UpdateLastState(snap.Values);
        }

        public Task SaveAsync(IReadOnlyDictionary<string, object?> snapshot, CancellationToken ct)
        {
            if (snapshot == null)
                return Task.CompletedTask;

            var snap = new SimStateSnapshot(new Dictionary<string, object?>(snapshot, StringComparer.OrdinalIgnoreCase));
            return SaveIfChangedAsync(snap, ct);
        }

        private int CountChanges(IReadOnlyDictionary<string, object?> values)
        {
            var changes = 0;

            foreach (var kv in values)
            {
                if (!_lastSavedState.TryGetValue(kv.Key, out var previous) || !Equals(previous, kv.Value))
                    changes++;
            }

            foreach (var key in _lastSavedState.Keys)
            {
                if (!values.ContainsKey(key))
                    changes++;
            }

            return changes;
        }

        private void UpdateLastState(IReadOnlyDictionary<string, object?> values)
        {
            _lastSavedState.Clear();
            foreach (var kv in values)
            {
                _lastSavedState[kv.Key] = kv.Value;
            }
        }

        private string GetUserSnapshotPath()
        {
            if (!string.IsNullOrWhiteSpace(_customPath))
                return _customPath!;

            var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharedCockpitClient");
            Directory.CreateDirectory(baseDir);
            return Path.Combine(baseDir, "snapshot.json");
        }
    }
}
