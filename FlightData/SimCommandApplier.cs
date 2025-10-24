using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using Microsoft.FlightSimulator.SimConnect;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public sealed class SimCommandApplier
{
    private const GROUP_PRIORITY GROUP_PRIORITY_HIGHEST = GROUP_PRIORITY.HIGHEST;

    private readonly SimConnect _simconnect;
    private readonly Dictionary<string, ClientEvent> _eventByKey;

    public SimCommandApplier(SimConnect simconnect)
    {
        _simconnect = simconnect ?? throw new ArgumentNullException(nameof(simconnect));

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

    public SimStateSnapshot? Apply(string json,
        Func<SimStateSnapshot> snapshotAccessor,
        Action<SimStateSnapshot> snapshotUpdater)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        if (snapshotAccessor == null)
        {
            throw new ArgumentNullException(nameof(snapshotAccessor));
        }

        if (snapshotUpdater == null)
        {
            throw new ArgumentNullException(nameof(snapshotUpdater));
        }

        SimStateSnapshot snapshot;
        try
        {
            snapshot = snapshotAccessor();
        }
        catch (Exception ex)
        {
            Logger.Warn($"⚠️ No se pudo obtener el estado actual antes de aplicar comandos: {ex.Message}");
            return null;
        }

        var definitionsToUpdate = new HashSet<SimDataDefinition>();
        var eventsToSend = new List<(ClientEvent evt, uint data)>();

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("state", out var stateElement) && stateElement.ValueKind == JsonValueKind.Object)
            {
                ApplyNestedObject(stateElement, snapshot, definitionsToUpdate, eventsToSend);
            }

            if (root.TryGetProperty("changes", out var changesElement) && changesElement.ValueKind == JsonValueKind.Object)
            {
                ApplyFlatObject(changesElement, snapshot, definitionsToUpdate, eventsToSend);
            }
        }
        catch (JsonException ex)
        {
            Logger.Warn($"⚠️ No se pudo analizar el comando remoto: {ex.Message}");
            return null;
        }

        foreach (var (evt, data) in eventsToSend)
        {
            TransmitEvent(evt, data);
        }

        foreach (var definition in definitionsToUpdate)
        {
            SendDefinition(definition, snapshot);
        }

        snapshotUpdater(snapshot);
        return snapshot;
    }

    private void ApplyNestedObject(JsonElement element, SimStateSnapshot snapshot, HashSet<SimDataDefinition> definitions, List<(ClientEvent evt, uint data)> events)
    {
        foreach (var group in element.EnumerateObject())
        {
            if (group.Value.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var field in group.Value.EnumerateObject())
            {
                ApplyKey($"{group.Name}.{field.Name}", field.Value, snapshot, definitions, events);
            }
        }
    }

    private void ApplyFlatObject(JsonElement element, SimStateSnapshot snapshot, HashSet<SimDataDefinition> definitions, List<(ClientEvent evt, uint data)> events)
    {
        foreach (var property in element.EnumerateObject())
        {
            ApplyKey(property.Name, property.Value, snapshot, definitions, events);
        }
    }

    private void ApplyKey(string key, JsonElement valueElement, SimStateSnapshot snapshot, HashSet<SimDataDefinition> definitions, List<(ClientEvent evt, uint data)> events)
    {
        var value = ConvertJsonValue(valueElement);

        if (_eventByKey.TryGetValue(key, out var evt))
        {
            var boolValue = ConvertToBool(value);
            events.Add((evt, boolValue ? 1u : 0u));
        }

        if (!snapshot.TryApplyChange(key, value))
        {
            return;
        }

        if (TryGetDefinitionForKey(key, out var definition))
        {
            definitions.Add(definition);
        }
    }

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

    private static object? ConvertJsonValue(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
        JsonValueKind.Number => element.GetDouble(),
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Null => null,
        _ => element.ToString()
    };

    private static bool ConvertToBool(object? value)
    {
        return value switch
        {
            null => false,
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            double d => Math.Abs(d) > double.Epsilon,
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
