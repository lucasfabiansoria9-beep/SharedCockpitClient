using System;
using System.Threading;
using System.Threading.Tasks;

namespace SharedCockpitClient.FlightData
{
    public sealed class SimCommandApplier
    {
        private readonly Func<SimVarDescriptor, object?, CancellationToken, Task<bool>> _varWriter;
        private readonly Func<SimEventDescriptor, object?, CancellationToken, Task<bool>> _eventWriter;
        private readonly Action<string, object?> _stateMirror;

        public SimCommandApplier(
            Func<SimVarDescriptor, object?, CancellationToken, Task<bool>> varWriter,
            Func<SimEventDescriptor, object?, CancellationToken, Task<bool>> eventWriter,
            Action<string, object?> stateMirror)
        {
            _varWriter = varWriter ?? throw new ArgumentNullException(nameof(varWriter));
            _eventWriter = eventWriter ?? throw new ArgumentNullException(nameof(eventWriter));
            _stateMirror = stateMirror ?? throw new ArgumentNullException(nameof(stateMirror));
        }

        public async Task<bool> ApplyAsync(string path, object? value, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            var applied = false;

            if (!SimDataDefinition.TryGetVar(path, out var descriptor) &&
                !SimDataDefinition.TryGetVarBySimVarKey(path, out descriptor))
            {
                descriptor = null;
            }

            if (descriptor != null && descriptor.Writable)
            {
                applied |= await _varWriter(descriptor, value, ct).ConfigureAwait(false);
            }

            if (!SimDataDefinition.TryGetEvent(path, out var evt) &&
                !SimDataDefinition.TryGetEventByName(path, out evt))
            {
                evt = null;
            }

            if (evt != null)
            {
                applied |= await _eventWriter(evt, value, ct).ConfigureAwait(false);
            }

            if (!applied)
            {
                // Si no existe mapeo directo, igualmente reflejamos el valor localmente.
                _stateMirror(path, value);
            }
            else
            {
                _stateMirror(path, value);
            }

            return applied;
        }
    }
}
