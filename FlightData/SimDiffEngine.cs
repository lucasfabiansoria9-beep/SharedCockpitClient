#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace SharedCockpitClient;

public sealed class SimDiffEngine
{
    private readonly Dictionary<string, object?> _lastFlat = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public string? ComputeDiff(string source, IReadOnlyDictionary<string, object?> snapshot, bool forceFull = false)
    {
        if (snapshot == null)
        {
            return null;
        }

        var flat = Flatten(snapshot);

        lock (_lock)
        {
            if (_lastFlat.Count == 0 || forceFull)
            {
                _lastFlat.Clear();
                foreach (var kv in flat)
                {
                    _lastFlat[kv.Key] = kv.Value;
                }

                return JsonSerializer.Serialize(new
                {
                    src = source,
                    full = true,
                    state = snapshot
                });
            }

            var changes = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

            foreach (var kv in flat)
            {
                if (!_lastFlat.TryGetValue(kv.Key, out var previous) || !ValuesEqual(previous, kv.Value))
                {
                    changes[kv.Key] = kv.Value;
                    _lastFlat[kv.Key] = kv.Value;
                }
            }

            var removedKeys = _lastFlat.Keys.Except(flat.Keys, StringComparer.OrdinalIgnoreCase).ToList();
            foreach (var removed in removedKeys)
            {
                changes[removed] = null;
                _lastFlat.Remove(removed);
            }

            if (changes.Count == 0)
            {
                return null;
            }

            return JsonSerializer.Serialize(new
            {
                src = source,
                changes
            });
        }
    }

    public void CommitExternalState(IReadOnlyDictionary<string, object?> snapshot)
    {
        if (snapshot == null)
        {
            return;
        }

        var flat = Flatten(snapshot);

        lock (_lock)
        {
            _lastFlat.Clear();
            foreach (var kv in flat)
            {
                _lastFlat[kv.Key] = kv.Value;
            }
        }
    }

    private static Dictionary<string, object?> Flatten(IReadOnlyDictionary<string, object?> snapshot, string? prefix = null)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in snapshot)
        {
            var key = prefix == null ? kv.Key : $"{prefix}.{kv.Key}";
            if (TryAsDictionary(kv.Value, out var nested))
            {
                foreach (var inner in Flatten(nested, key))
                {
                    result[inner.Key] = inner.Value;
                }
            }
            else
            {
                result[key] = kv.Value;
            }
        }

        return result;
    }

    private static bool TryAsDictionary(object? value, out IReadOnlyDictionary<string, object?> dictionary)
    {
        switch (value)
        {
            case null:
                dictionary = new Dictionary<string, object?>();
                return false;
            case IReadOnlyDictionary<string, object?> readOnly:
                dictionary = readOnly;
                return true;
            case IDictionary<string, object?> dict:
                dictionary = dict.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
                return true;
            default:
                dictionary = new Dictionary<string, object?>();
                return false;
        }
    }

    private static bool ValuesEqual(object? a, object? b)
    {
        if (a == null && b == null)
        {
            return true;
        }

        if (a == null || b == null)
        {
            return false;
        }

        if (TryAsDouble(a, out var da) && TryAsDouble(b, out var db))
        {
            return Math.Abs(da - db) < 0.001;
        }

        if (TryAsBool(a, out var ba) && TryAsBool(b, out var bb))
        {
            return ba == bb;
        }

        return string.Equals(Convert.ToString(a, CultureInfo.InvariantCulture), Convert.ToString(b, CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryAsDouble(object? value, out double result)
    {
        switch (value)
        {
            case null:
                result = 0;
                return false;
            case double d:
                result = d;
                return true;
            case float f:
                result = f;
                return true;
            case int i:
                result = i;
                return true;
            case long l:
                result = l;
                return true;
            case decimal m:
                result = (double)m;
                return true;
            case string s when double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed):
                result = parsed;
                return true;
            default:
                result = 0;
                return false;
        }
    }

    private static bool TryAsBool(object? value, out bool result)
    {
        switch (value)
        {
            case null:
                result = false;
                return false;
            case bool b:
                result = b;
                return true;
            case int i:
                result = i != 0;
                return true;
            case long l:
                result = l != 0;
                return true;
            case double d:
                result = Math.Abs(d) > double.Epsilon;
                return true;
            case string s when bool.TryParse(s, out var parsedBool):
                result = parsedBool;
                return true;
            case string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt):
                result = parsedInt != 0;
                return true;
            default:
                result = false;
                return false;
        }
    }
}
