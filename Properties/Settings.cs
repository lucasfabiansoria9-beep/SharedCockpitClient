using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SharedCockpitClient.Properties
{
    internal sealed class Settings
    {
        private static readonly Settings _default = new();
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Role"] = string.Empty,
            ["PeerAddress"] = string.Empty
        };

        private readonly string _filePath;

        private Settings()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            if (string.IsNullOrEmpty(appData))
            {
                appData = AppDomain.CurrentDomain.BaseDirectory;
            }

            var folder = Path.Combine(appData, "SharedCockpitClient");
            _filePath = Path.Combine(folder, "settings.json");
            Load();
        }

        public static Settings Default => _default;

        public object? this[string propertyName]
        {
            get
            {
                if (propertyName == null)
                    return null;

                lock (_values)
                {
                    return _values.TryGetValue(propertyName, out var value) ? value : string.Empty;
                }
            }
            set
            {
                if (propertyName == null)
                    return;

                lock (_values)
                {
                    _values[propertyName] = value?.ToString() ?? string.Empty;
                }
            }
        }

        public void Save()
        {
            lock (_values)
            {
                try
                {
                    var folder = Path.GetDirectoryName(_filePath);
                    if (!string.IsNullOrEmpty(folder))
                        Directory.CreateDirectory(folder);

                    var json = JsonSerializer.Serialize(_values);
                    File.WriteAllText(_filePath, json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Settings] ⚠️ No se pudo guardar configuración: {ex.Message}");
                }
            }
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return;

                var json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
                if (data == null)
                    return;

                lock (_values)
                {
                    foreach (var kv in data)
                        _values[kv.Key] = kv.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Settings] ⚠️ No se pudo leer configuración: {ex.Message}");
            }
        }
    }
}
