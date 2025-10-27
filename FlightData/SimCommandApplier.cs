#if !LAB_MINIMAL
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimCommandApplier
{
    private const GROUP_PRIORITY GROUP_PRIORITY_HIGHEST = GROUP_PRIORITY.HIGHEST;

    private static readonly SimDataDefinition[] FullSyncDefinitions =
    {
        SimDataDefinition.Controls,
        SimDataDefinition.Cabin,
        SimDataDefinition.Systems,
        SimDataDefinition.Doors,
        SimDataDefinition.Ground,
        SimDataDefinition.Environment,
        SimDataDefinition.Avionics
    };

    private readonly SimConnect _simconnect;
    private readonly Dictionary<string, ClientEvent> _eventByKey;
    private readonly Func<SimStateSnapshot> _snapshotAccessor;
    private readonly Action<SimStateSnapshot> _snapshotUpdater;
    private readonly object _lock = new();

    public SimCommandApplier(
        SimConnect simconnect,
        Func<SimStateSnapshot> snapshotAccessor,
        Action<SimStateSnapshot> snapshotUpdater)
    {
        _simconnect = simconnect ?? throw new ArgumentNullException(nameof(simconnect));
        _snapshotAccessor = snapshotAccessor ?? throw new ArgumentNullException(nameof(snapshotAccessor));
        _snapshotUpdater = snapshotUpdater ?? throw new ArgumentNullException(nameof(snapshotUpdater));

        _eventByKey = new Dictionary<string, ClientEvent>(StringComparer.OrdinalIgnoreCase)
        {
            ["systems.landingLight"] = ClientEvent.LandingLightsSet,
            ["systems.beaconLight"] = ClientEvent.BeaconLightsSet,
            ["systems.navLight"] = ClientEvent.NavLightsSet,
            ["systems.strobeLight"] = ClientEvent.StrobeLightsSet,
            ["systems.taxiLight"] = ClientEvent.TaxiLightsSet,
            ["systems.batteryMaster"] = ClientEvent.MasterBatterySet,
            ["systems.alternator"] = ClientEvent.MasterAlternatorSet,
            ["systems.avionicsMaster"] = ClientEvent.AvionicsMasterSet,
            ["systems.fuelPump"] = ClientEvent.FuelPumpSet,
            ["systems.pitotHeat"] = ClientEvent.PitotHeatSet,
            ["systems.antiIce"] = ClientEvent.AntiIceSet,
            ["cabin.landingGearDown"] = ClientEvent.GearHandleSet,
            ["cabin.autopilotMaster"] = ClientEvent.AutopilotMasterSet,
            ["avionics.autopilotNavHold"] = ClientEvent.AutopilotNavHold
        };

        RegisterEvents();
    }

    public bool ApplyRemoteChanges(IReadOnlyDictionary<string, object?> changes)
    {
        if (changes == null || changes.Count == 0)
        {
            return false;
        }

        lock (_lock)
        {
            if (!TryGetSnapshot(out var snapshot))
            {
                return false;
            }

            var definitions = new HashSet<SimDataDefinition>();
            var eventsToSend = new List<(ClientEvent evt, uint data)>();
            var appliedAny = false;

            foreach (var change in changes)
            {
                var key = change.Key;
                var normalizedValue = NormalizeValue(change.Value);

                if (ApplyKey(key, normalizedValue, snapshot, definitions, eventsToSend))
                {
                    appliedAny = true;
                }
                else
                {
                    Logger.Warn($"⚠️ Cambio remoto desconocido o inválido: {key}");
                }
            }

            if (!appliedAny)
            {
                return false;
            }

            Dispatch(eventsToSend, definitions, snapshot);
            Logger.Info($"🔁 Cambios remotos aplicados ({changes.Count}).");
            return true;
        }
    }

    public bool ApplyRemoteChange(string key, object? value)
        => ApplyRemoteChanges(new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [key] = value
        });

    public bool ApplyFullSnapshot(SimStateSnapshot snapshot)
    {
        if (snapshot == null)
        {
            Logger.Warn("⚠️ Se recibió un snapshot remoto nulo.");
            return false;
        }

        lock (_lock)
        {
            var clone = snapshot.Clone();
            foreach (var definition in FullSyncDefinitions)
            {
                SendDefinition(definition, clone);
            }

            _snapshotUpdater(clone);
        }

        Logger.Info("🆕 Snapshot remoto completo aplicado correctamente.");
        return true;
    }

    private void Dispatch(
        List<(ClientEvent evt, uint data)> eventsToSend,
        HashSet<SimDataDefinition> definitions,
        SimStateSnapshot snapshot)
    {
        foreach (var (evt, data) in eventsToSend)
        {
            TransmitEvent(evt, data);
        }

        foreach (var definition in definitions)
        {
            SendDefinition(definition, snapshot);
        }

        _snapshotUpdater(snapshot.Clone());
    }

    private bool TryGetSnapshot(out SimStateSnapshot snapshot)
    {
        try
        {
            snapshot = _snapshotAccessor();
            return true;
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo obtener el estado actual antes de aplicar comandos: {ex.Message}");
            snapshot = new SimStateSnapshot();
            return false;
        }
    }

    private bool ApplyKey(
        string key,
        object? value,
        SimStateSnapshot snapshot,
        HashSet<SimDataDefinition> definitions,
        List<(ClientEvent evt, uint data)> events)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        if (_eventByKey.TryGetValue(key, out var evt))
        {
            var boolValue = ConvertToBool(value);
            events.Add((evt, boolValue ? 1u : 0u));
        }

        if (!snapshot.TryApplyChange(key, value))
        {
            return false;
        }

        if (TryGetDefinitionForKey(key, out var definition))
        {
            definitions.Add(definition);
        }

        return true;
    }

    private static object? NormalizeValue(object? value)
    {
        return value switch
        {
            null => null,
            JsonElement element => ConvertJsonValue(element),
            _ => value
        };
    }

    private static object? ConvertJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(ConvertJsonValue).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(
            property => property.Name,
            property => ConvertJsonValue(property.Value),
            StringComparer.OrdinalIgnoreCase),
        _ => element.ToString()
    };

    private void SendDefinition(SimDataDefinition definition, SimStateSnapshot snapshot)
    {
        try
        {
            switch (definition)
            {
                case SimDataDefinition.Controls:
                    var controls = snapshot.Controls;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, controls);
                    break;
                case SimDataDefinition.Cabin:
                    var cabin = snapshot.Cabin;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, cabin);
                    break;
                case SimDataDefinition.Systems:
                    var systems = snapshot.Systems;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, systems);
                    break;
                case SimDataDefinition.Doors:
                    var doors = snapshot.Doors;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, doors);
                    break;
                case SimDataDefinition.Ground:
                    var ground = snapshot.Ground;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, ground);
                    break;
                case SimDataDefinition.Environment:
                    var environment = snapshot.Environment;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, environment);
                    break;
                case SimDataDefinition.Avionics:
                    var avionics = snapshot.Avionics;
                    _simconnect.SetDataOnSimObject(definition, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_DATA_SET_FLAG.DEFAULT, avionics);
                    break;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo enviar el grupo {definition}: {ex.Message}");
        }
    }

    private void TransmitEvent(ClientEvent evt, uint value)
    {
        try
        {
            _simconnect.TransmitClientEvent(0, evt, value, GROUP_PRIORITY_HIGHEST, SIMCONNECT_EVENT_FLAG.GROUPID_IS_PRIORITY);
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo transmitir evento {evt}: {ex.Message}");
        }
    }

    private void RegisterEvents()
    {
        foreach (var mapping in _eventByKey)
        {
            var simEvent = GetSimEventName(mapping.Value);
            if (simEvent == null)
            {
                continue;
            }

            try
            {
                _simconnect.MapClientEventToSimEvent(mapping.Value, simEvent);
            }
            catch (Exception ex)
            {
                Logger.Warn($"⚠️ No se pudo mapear el evento {mapping.Value}: {ex.Message}");
            }
        }
    }

    private static string? GetSimEventName(ClientEvent evt) => evt switch
    {
        ClientEvent.LandingLightsSet => "LANDING_LIGHTS_SET",
        ClientEvent.BeaconLightsSet => "BEACON_LIGHTS_SET",
        ClientEvent.NavLightsSet => "NAV_LIGHTS_SET",
        ClientEvent.StrobeLightsSet => "STROBES_SET",
        ClientEvent.TaxiLightsSet => "TAXI_LIGHTS_SET",
        ClientEvent.MasterBatterySet => "MASTER_BATTERY_SET",
        ClientEvent.MasterAlternatorSet => "MASTER_ALTERNATOR_SET",
        ClientEvent.AvionicsMasterSet => "AVIONICS_MASTER_SET",
        ClientEvent.FuelPumpSet => "FUEL_PUMP_SET",
        ClientEvent.PitotHeatSet => "PITOT_HEAT_SET",
        ClientEvent.AntiIceSet => "ANTI_ICE_SET",
        ClientEvent.GearHandleSet => "GEAR_HANDLE_SET",
        ClientEvent.AutopilotMasterSet => "AP_MASTER",
        ClientEvent.AutopilotNavHold => "AP_NAV1_HOLD",
        _ => null
    };

    private static bool TryGetDefinitionForKey(string key, out SimDataDefinition definition)
    {
        definition = default;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var separatorIndex = key.IndexOf('.');
        if (separatorIndex <= 0)
        {
            return false;
        }

        var group = key[..separatorIndex];

        switch (group)
        {
            case "controls":
                definition = SimDataDefinition.Controls;
                return true;
            case "cabin":
                definition = SimDataDefinition.Cabin;
                return true;
            case "systems":
                definition = SimDataDefinition.Systems;
                return true;
            case "doors":
                definition = SimDataDefinition.Doors;
                return true;
            case "ground":
                definition = SimDataDefinition.Ground;
                return true;
            case "environment":
                definition = SimDataDefinition.Environment;
                return true;
            case "avionics":
                definition = SimDataDefinition.Avionics;
                return true;
            default:
                return false;
        }
    }

    private static bool ConvertToBool(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => Math.Abs(d) > double.Epsilon,
            decimal m => Math.Abs((double)m) > double.Epsilon,
            string s when bool.TryParse(s, out var parsedBool) => parsedBool,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt) => parsedInt != 0,
            _ => false
        };
    }

    private enum ClientEvent
    {
        LandingLightsSet,
        BeaconLightsSet,
        NavLightsSet,
        StrobeLightsSet,
        TaxiLightsSet,
        MasterBatterySet,
        MasterAlternatorSet,
        AvionicsMasterSet,
        FuelPumpSet,
        PitotHeatSet,
        AntiIceSet,
        GearHandleSet,
        AutopilotMasterSet,
        AutopilotNavHold
    }

    private enum GROUP_PRIORITY : uint
    {
        HIGHEST = 1
    }
}
#endif
