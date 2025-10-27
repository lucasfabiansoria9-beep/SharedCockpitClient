using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Fuente de datos simulada para laboratorio y pruebas sin MSFS.
    /// </summary>
    public sealed class SimDataMock
    {
        private readonly Dictionary<string, object?> _state = new(StringComparer.OrdinalIgnoreCase);
        private readonly Random _rng = new();

        public SimDataMock()
        {
            foreach (var descriptor in SimDataDefinition.AllSimVars)
            {
                if (!_state.ContainsKey(descriptor.Path))
                {
                    _state[descriptor.Path] = descriptor.DataType switch
                    {
                        SimDataType.Bool => false,
                        SimDataType.Int32 => 0,
                        SimDataType.Float32 or SimDataType.Float64 => 0.0,
                        SimDataType.String256 => string.Empty,
                        _ => null
                    };
                }
            }

            _state["Controls.Throttle[1]"] = 0.4;
            _state["Controls.Throttle[2]"] = 0.4;
            _state["Systems.Autopilot.AP_MASTER"] = false;
            _state["Systems.Lights.Beacon"] = true;
            _state["Systems.Gear.Nose"] = 1.0;
        }

        public Task<IDictionary<string, object?>> SnapshotAsync(CancellationToken ct)
        {
            lock (_state)
            {
                // Simulación sencilla: variamos levemente algunas variables.
                _state["Controls.Throttle[1]"] = Clamp01((double)_state["Controls.Throttle[1]"]! + (_rng.NextDouble() - 0.5) * 0.02);
                _state["Controls.Throttle[2]"] = Clamp01((double)_state["Controls.Throttle[2]"]! + (_rng.NextDouble() - 0.5) * 0.02);
                _state["Environment.OutsideAirTemp"] = 18 + Math.Sin(DateTime.UtcNow.Second / 60.0 * Math.PI * 2) * 2;

                return Task.FromResult<IDictionary<string, object?>>(new Dictionary<string, object?>(_state));
            }
        }

        public Task<bool> ApplyVarAsync(SimVarDescriptor descriptor, object? value, CancellationToken ct)
        {
            lock (_state)
            {
                _state[descriptor.Path] = ConvertValue(descriptor.DataType, value);
                return Task.FromResult(true);
            }
        }

        public Task<bool> TriggerEventAsync(SimEventDescriptor descriptor, object? value, CancellationToken ct)
        {
            lock (_state)
            {
                // Para el mock reflejamos cambios booleanos.
                if (descriptor.Path.EndsWith("Swap", StringComparison.OrdinalIgnoreCase))
                {
                    // intercambio simple active/standby
                    if (descriptor.Path.Contains("Com1", StringComparison.OrdinalIgnoreCase))
                    {
                        Swap("Systems.Radios.Com1Active", "Systems.Radios.Com1Standby");
                    }
                    else if (descriptor.Path.Contains("Com2", StringComparison.OrdinalIgnoreCase))
                    {
                        Swap("Systems.Radios.Com2Active", "Systems.Radios.Com2Standby");
                    }
                    else if (descriptor.Path.Contains("Nav1", StringComparison.OrdinalIgnoreCase))
                    {
                        Swap("Systems.Radios.Nav1Active", "Systems.Radios.Nav1Standby");
                    }
                }
                else if (_state.ContainsKey(descriptor.Path))
                {
                    var current = _state[descriptor.Path];
                    if (current is bool b)
                    {
                        _state[descriptor.Path] = !b;
                    }
                }

                return Task.FromResult(true);
            }
        }

        private void Swap(string a, string b)
        {
            var tmp = _state[a];
            _state[a] = _state[b];
            _state[b] = tmp;
        }

        private static object? ConvertValue(SimDataType dataType, object? value)
        {
            if (value is null)
                return dataType switch
                {
                    SimDataType.String256 => string.Empty,
                    SimDataType.Bool => false,
                    _ => 0
                };

            try
            {
                return dataType switch
                {
                    SimDataType.Bool => value is bool b ? b : Convert.ToBoolean(value, CultureInfo.InvariantCulture),
                    SimDataType.Int32 => value is int i ? i : Convert.ToInt32(value, CultureInfo.InvariantCulture),
                    SimDataType.Float32 => value is float f ? f : Convert.ToSingle(value, CultureInfo.InvariantCulture),
                    SimDataType.Float64 => value is double d ? d : Convert.ToDouble(value, CultureInfo.InvariantCulture),
                    SimDataType.String256 => Convert.ToString(value, CultureInfo.InvariantCulture),
                    _ => value
                };
            }
            catch
            {
                return value;
            }
        }

        private static double Clamp01(double value) => Math.Max(0, Math.Min(1, value));
    }
}
