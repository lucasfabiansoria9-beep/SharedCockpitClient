using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SharedCockpitClient.FlightData;

namespace SharedCockpitClient.Persistence
{
    public sealed class SnapshotStore
    {
        private readonly string? _customPath;
        private DateTime _lastSaveUtc = DateTime.MinValue;
        private string _lastHash = string.Empty;

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
            Console.WriteLine($"[SnapshotStore] Estado restaurado desde {path} ({snap.Data.Count} entradas)");
            return new Dictionary<string, object?>(snap.Data, StringComparer.OrdinalIgnoreCase);
        }

        public async Task SaveIfChangedAsync(SimStateSnapshot snap, CancellationToken ct)
        {
            if (snap == null)
                return;

            snap.CompactInPlace();

            if (snap.Values == null || snap.Values.Count == 0)
            {
                Console.WriteLine("[SnapshotStore] ⚠️ Snapshot vacío omitido (sin cambios detectados)");
                return;
            }

            if ((DateTime.UtcNow - _lastSaveUtc).TotalSeconds < 2)
                return;

            var json = JsonSerializer.Serialize(snap);
            using var md5 = MD5.Create();
            var hash = Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(json)));
            if (hash == _lastHash)
                return;

            _lastHash = hash;
            var path = GetUserSnapshotPath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, json, ct).ConfigureAwait(false);
            _lastSaveUtc = DateTime.UtcNow;
            Console.WriteLine("[SnapshotStore] ✅ Guardado");
        }

        public Task SaveAsync(IReadOnlyDictionary<string, object?> snapshot, CancellationToken ct)
        {
            if (snapshot == null)
                return Task.CompletedTask;

            var snap = new SimStateSnapshot(new Dictionary<string, object?>(snapshot, StringComparer.OrdinalIgnoreCase));
            return SaveIfChangedAsync(snap, ct);
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
