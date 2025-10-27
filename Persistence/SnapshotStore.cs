using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.Persistence
{
    public sealed class SnapshotStore
    {
        private readonly string _snapshotPath;

        public SnapshotStore(string? customPath = null)
        {
            if (!string.IsNullOrWhiteSpace(customPath))
            {
                _snapshotPath = customPath;
            }
            else
            {
                var baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SharedCockpitClient");
                Directory.CreateDirectory(baseDir);
                _snapshotPath = Path.Combine(baseDir, "snapshot.json");
            }
        }

        public async Task<IDictionary<string, object?>> LoadAsync(CancellationToken ct)
        {
            if (!File.Exists(_snapshotPath))
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            await using var stream = File.OpenRead(_snapshotPath);
            var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct).ConfigureAwait(false);
            if (!doc.RootElement.TryGetProperty("state", out var stateElement))
            {
                return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            }

            var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(stateElement.GetRawText(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            Console.WriteLine($"[SnapshotStore] Estado restaurado desde {_snapshotPath} ({dictionary.Count} entradas)");
            return dictionary;
        }

        public async Task SaveAsync(IReadOnlyDictionary<string, object?> snapshot, CancellationToken ct)
        {
            if (snapshot == null)
                return;

            Directory.CreateDirectory(Path.GetDirectoryName(_snapshotPath)!);
            await using var stream = new FileStream(_snapshotPath, FileMode.Create, FileAccess.Write, FileShare.None);
            var payload = new
            {
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                state = snapshot
            };

            await JsonSerializer.SerializeAsync(stream, payload, new JsonSerializerOptions { WriteIndented = true }, ct).ConfigureAwait(false);
            Console.WriteLine($"[SnapshotStore] Estado guardado en {_snapshotPath}");
        }
    }
}
