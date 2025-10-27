using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Representa un snapshot (full o incremental) del estado del simulador con rutas canónicas.
    /// </summary>
    public sealed class SimStateSnapshot
    {
        private readonly Dictionary<string, object?> _values;

        public SimStateSnapshot()
            : this(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase))
        {
        }

        public SimStateSnapshot(IDictionary<string, object?> values, DateTime? timestampUtc = null, bool isDiff = false, long sequence = 0)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            _values = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
            TimestampUtc = timestampUtc ?? DateTime.UtcNow;
            IsDiff = isDiff;
            Sequence = sequence;
        }

        /// <summary>
        /// Instante en UTC en que el snapshot fue generado.
        /// </summary>
        public DateTime TimestampUtc { get; }

        /// <summary>
        /// Indica si representa únicamente cambios respecto al snapshot previo.
        /// </summary>
        public bool IsDiff { get; }

        /// <summary>
        /// Secuencia global o correlativo asociado al snapshot.
        /// </summary>
        public long Sequence { get; }

        /// <summary>
        /// Valores aplanados (ruta → valor).
        /// </summary>
        public IReadOnlyDictionary<string, object?> Values => _values;

        /// <summary>
        /// Obtiene el valor para una ruta concreta.
        /// </summary>
        public bool TryGetValue(string path, out object? value)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            return _values.TryGetValue(path, out value);
        }

        /// <summary>
        /// Establece/actualiza una ruta dentro del snapshot.
        /// </summary>
        public void Set(string path, object? value)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));
            _values[path] = value;
        }

        /// <summary>
        /// Crea un snapshot incremental sobre esta instancia.
        /// </summary>
        public SimStateSnapshot CreateDiff(IDictionary<string, object?> diffValues, long sequence)
        {
            return new SimStateSnapshot(diffValues ?? throw new ArgumentNullException(nameof(diffValues)), DateTime.UtcNow, true, sequence);
        }

        /// <summary>
        /// Funde un diff dentro de una copia del snapshot actual.
        /// </summary>
        public SimStateSnapshot MergeDiff(SimStateSnapshot diff)
        {
            if (diff == null) throw new ArgumentNullException(nameof(diff));

            var merged = new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase);
            foreach (var kv in diff.Values)
            {
                if (kv.Value is null)
                {
                    merged.Remove(kv.Key);
                }
                else
                {
                    merged[kv.Key] = kv.Value;
                }
            }

            return new SimStateSnapshot(merged, diff.TimestampUtc, false, diff.Sequence);
        }

        public SimStateSnapshot Clone()
        {
            return new SimStateSnapshot(new Dictionary<string, object?>(_values, StringComparer.OrdinalIgnoreCase), TimestampUtc, IsDiff, Sequence);
        }

        public override string ToString()
        {
            return $"Snapshot[{TimestampUtc:O}] (diff={IsDiff}, values={_values.Count})";
        }

        /// <summary>
        /// Construye un snapshot a partir de pares ruta/valor.
        /// </summary>
        public static SimStateSnapshot FromPairs(IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            if (pairs == null) throw new ArgumentNullException(nameof(pairs));
            return new SimStateSnapshot(pairs.ToDictionary(k => k.Key, k => k.Value, StringComparer.OrdinalIgnoreCase));
        }
    }
}
