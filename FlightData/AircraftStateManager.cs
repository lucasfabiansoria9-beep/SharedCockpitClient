using System;
using System.Collections.Generic;

namespace SharedCockpitClient
{
    /// <summary>
    /// Mantiene el estado actual del avión y notifica cambios a SyncController.
    /// Soporta lectura/escritura de variables y snapshots completos.
    /// </summary>
    public sealed class AircraftStateManager
    {
        // Diccionario base: propiedad → valor
        private readonly Dictionary<string, object?> _state = new(StringComparer.OrdinalIgnoreCase);

        // Último snapshot recibido (para reconexiones)
        private Dictionary<string, object?> _lastSnapshot = new(StringComparer.OrdinalIgnoreCase);

        // Evento de cambio: (prop, nuevo valor)
        public event Action<string, object?>? OnPropertyChanged;

        public AircraftStateManager()
        {
            // Estado inicial por defecto (se puede extender)
            _state["Flaps"] = 0.0;
            _state["GearDown"] = true;
            _state["Lights"] = false;
            _state["AvionicsOn"] = true;
            _state["DoorOpen"] = false;
            _state["EngineOn"] = false;
            _state["ParkingBrake"] = true;
        }

        /// <summary>
        /// Asigna un nuevo valor y dispara evento si cambia.
        /// </summary>
        public void Set(string prop, object? value)
        {
            if (prop == null) return;

            lock (_state)
            {
                if (!_state.ContainsKey(prop) || !Equals(_state[prop], value))
                {
                    _state[prop] = value;
                    OnPropertyChanged?.Invoke(prop, value);
                }
            }
        }

        /// <summary>
        /// Obtiene el valor actual de una propiedad (null si no existe).
        /// </summary>
        public object? Get(string prop)
        {
            lock (_state)
            {
                return _state.TryGetValue(prop, out var val) ? val : null;
            }
        }

        /// <summary>
        /// Devuelve snapshot completo del estado actual.
        /// </summary>
        public Dictionary<string, object?> GetSnapshot()
        {
            lock (_state)
            {
                return new Dictionary<string, object?>(_state);
            }
        }

        /// <summary>
        /// Aplica un snapshot completo recibido por red.
        /// Solo actualiza propiedades que cambian.
        /// </summary>
        public void ApplySnapshot(Dictionary<string, object?> snapshot)
        {
            if (snapshot == null)
                return;

            lock (_state)
            {
                foreach (var kvp in snapshot)
                {
                    if (!SimStateSnapshot.LooksLikeSimVar(kvp.Key))
                        continue;
                    if (kvp.Value is null)
                        continue;

                    if (!_state.ContainsKey(kvp.Key) || !Equals(_state[kvp.Key], kvp.Value))
                    {
                        _state[kvp.Key] = kvp.Value;
                        OnPropertyChanged?.Invoke(kvp.Key, kvp.Value);
                    }
                }

                _lastSnapshot = new Dictionary<string, object?>(_state);
            }
        }

        /// <summary>
        /// Guarda snapshot actual para reconexión futura.
        /// </summary>
        public void SaveSnapshot()
        {
            lock (_state)
            {
                _lastSnapshot = new Dictionary<string, object?>(_state);
            }
        }

        /// <summary>
        /// Restaura último snapshot guardado.
        /// </summary>
        public void RestoreSnapshot()
        {
            lock (_state)
            {
                if (_lastSnapshot.Count > 0)
                {
                    foreach (var kvp in _lastSnapshot)
                        _state[kvp.Key] = kvp.Value;
                }
            }
        }

        public SimStateSnapshot ExportFullSnapshot()
        {
            lock (_state)
            {
                var snapshot = new SimStateSnapshot();
                foreach (var kvp in _state)
                {
                    if (!SimStateSnapshot.LooksLikeSimVar(kvp.Key))
                        continue;

                    if (kvp.Value is null)
                        continue;

                    snapshot.Set(kvp.Key, kvp.Value);
                }

                return snapshot;
            }
        }
    }
}
