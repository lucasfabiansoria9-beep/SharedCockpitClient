using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient
{
    /// <summary>
    /// Encargado de sondear SimConnect (o su mock) y emitir snapshots incrementales.
    /// </summary>
    public sealed class SimDataCollector : IDisposable
    {
        private readonly Func<CancellationToken, Task<IDictionary<string, object?>>> _sampler;
        private readonly TimeSpan _pollInterval;
        private readonly SimDiffEngine _diffEngine = new();
        private CancellationTokenSource? _cts;
        private Task? _loopTask;
        private long _sequence;

        public event Action<SimStateSnapshot, bool>? OnSnapshot;

        public SimDataCollector(Func<CancellationToken, Task<IDictionary<string, object?>>> sampler, TimeSpan? pollInterval = null)
        {
            _sampler = sampler ?? throw new ArgumentNullException(nameof(sampler));
            _pollInterval = pollInterval ?? TimeSpan.FromMilliseconds(100);
        }

        public void Start()
        {
            if (_cts != null)
                return;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunLoopAsync(_cts.Token));
        }

        public async Task StopAsync()
        {
            if (_cts == null)
                return;

            _cts.Cancel();
            try
            {
                if (_loopTask != null)
                    await _loopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // esperado
            }
            finally
            {
                _loopTask = null;
                _cts.Dispose();
                _cts = null;
            }
        }

        private async Task RunLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    var values = await _sampler(token).ConfigureAwait(false);
                    if (values == null)
                    {
                        await Task.Delay(_pollInterval, token).ConfigureAwait(false);
                        continue;
                    }

                    var flat = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
                    var diff = ComputeDiff(flat);
                    if (diff == null)
                    {
                        await Task.Delay(_pollInterval, token).ConfigureAwait(false);
                        continue;
                    }

                    Interlocked.Increment(ref _sequence);
                    var snapshot = diff.IsDiff
                        ? new SimStateSnapshot(diff.Values, DateTime.UtcNow, true, _sequence)
                        : new SimStateSnapshot(diff.Values, DateTime.UtcNow, false, _sequence);

                    OnSnapshot?.Invoke(snapshot, snapshot.IsDiff);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SimCollector] ⚠️ Error en loop: {ex.Message}");
                }

                try
                {
                    await Task.Delay(_pollInterval, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private DiffResult? ComputeDiff(IReadOnlyDictionary<string, object?> current)
        {
            if (current == null)
                return null;

            var previousJson = _diffEngine.ComputeDiff("sim", current, forceFull: _sequence == 0);
            if (previousJson == null)
            {
                return null;
            }

            // El motor de diff devuelve JSON; reconstruimos snapshot para mantener compatibilidad.
            // Simplificamos parseando a diccionario dinámico.
            try
            {
                var node = JsonDocument.Parse(previousJson).RootElement;
                if (node.TryGetProperty("full", out var full) && full.GetBoolean())
                {
                    var state = node.GetProperty("state");
                    var dict = ExtractDictionary(state);
                    return new DiffResult(dict, false);
                }

                if (node.TryGetProperty("changes", out var changes))
                {
                    var dict = ExtractDictionary(changes);
                    return dict.Count == 0 ? null : new DiffResult(dict, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SimCollector] ⚠️ Error procesando diff: {ex.Message}");
            }

            return null;
        }

        private static Dictionary<string, object?> ExtractDictionary(JsonElement element)
        {
            var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = elementToValue(property.Value);
            }
            return result;

            static object? elementToValue(JsonElement value)
            {
                return value.ValueKind switch
                {
                    JsonValueKind.Number => value.TryGetInt64(out var l) ? l : value.GetDouble(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Object => ExtractDictionary(value),
                    _ => null
                };
            }
        }

        public void Dispose()
        {
            _ = StopAsync();
        }

        private sealed class DiffResult
        {
            public DiffResult(Dictionary<string, object?> values, bool isDiff)
            {
                Values = values;
                IsDiff = isDiff;
            }

            public Dictionary<string, object?> Values { get; }
            public bool IsDiff { get; }
        }
    }
}
