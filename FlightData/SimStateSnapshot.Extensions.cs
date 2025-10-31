#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharedCockpitClient
{
    public static class SimStateSnapshotExtensions
    {
        /// <summary>
        /// Crea un diccionario jerárquico a partir de las rutas canónicas del snapshot.
        /// </summary>
        public static Dictionary<string, object?> ToDictionary(this SimStateSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var root = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in snapshot.Values)
            {
                Insert(root, kv.Key, kv.Value);
            }

            return root;
        }

        /// <summary>
        /// Representación plana del snapshot.
        /// </summary>
        public static Dictionary<string, object?> ToFlatDictionary(this SimStateSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return new Dictionary<string, object?>(snapshot.Values, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Aplica un cambio individual (ruta → valor) sobre el snapshot dado.
        /// </summary>
        public static bool TryApplyChange(this SimStateSnapshot snapshot, string path, object? value)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (string.IsNullOrWhiteSpace(path)) return false;

            snapshot.Set(path, value);
            return true;
        }

        /// <summary>
        /// Combina múltiples cambios con el snapshot.
        /// </summary>
        public static SimStateSnapshot Merge(this SimStateSnapshot snapshot, IReadOnlyDictionary<string, object?> changes)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            if (changes == null) throw new ArgumentNullException(nameof(changes));

            var merged = snapshot.Clone();
            foreach (var kv in changes)
            {
                merged.Set(kv.Key, kv.Value);
            }

            return merged;
        }

        /// <summary>
        /// Obtiene el valor como double si es posible.
        /// </summary>
        public static bool TryGetDouble(this SimStateSnapshot snapshot, string path, out double value)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            value = 0;
            if (!snapshot.TryGetValue(path, out var raw) || raw is null)
                return false;

            switch (raw)
            {
                case double d:
                    value = d;
                    return true;
                case float f:
                    value = f;
                    return true;
                case int i:
                    value = i;
                    return true;
                case long l:
                    value = l;
                    return true;
                case decimal m:
                    value = (double)m;
                    return true;
                case string s when double.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed):
                    value = parsed;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Obtiene el valor como bool si es posible.
        /// </summary>
        public static bool TryGetBool(this SimStateSnapshot snapshot, string path, out bool value)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            value = false;
            if (!snapshot.TryGetValue(path, out var raw) || raw is null)
                return false;

            switch (raw)
            {
                case bool b:
                    value = b;
                    return true;
                case int i:
                    value = i != 0;
                    return true;
                case long l:
                    value = l != 0;
                    return true;
                case double d:
                    value = Math.Abs(d) > double.Epsilon;
                    return true;
                case string s when bool.TryParse(s, out var parsedBool):
                    value = parsedBool;
                    return true;
                case string s when int.TryParse(s, out var parsedInt):
                    value = parsedInt != 0;
                    return true;
                default:
                    return false;
            }
        }

        private static void Insert(IDictionary<string, object?> root, string path, object? value)
        {
            var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return;

            IDictionary<string, object?> current = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (!current.TryGetValue(part, out var nested) || nested is not IDictionary<string, object?> nestedDict)
                {
                    nestedDict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                    current[part] = nestedDict;
                }
                current = nestedDict;
            }

            current[parts[^1]] = value;
        }
    }
}
