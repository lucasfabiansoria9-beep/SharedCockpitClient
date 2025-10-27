using System;
using System.Collections.Generic;

namespace SharedCockpitClient.FlightData
{
    /// <summary>
    /// Clase base que representa un conjunto de datos SimConnect o variables
    /// definidas en la simulación. Se utiliza para registrar, mapear o aplicar comandos
    /// de vuelo sincrónicos entre cliente y host.
    /// </summary>
    public class SimDataDefinition
    {
        public string Name { get; set; } = string.Empty;              // nombre de variable
        public string Units { get; set; } = string.Empty;             // unidades (ft, deg, knots, etc.)
        public double Value { get; set; }                             // valor actual numérico
        public string? Type { get; set; }                             // tipo de variable (SimVar, Event, etc.)
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;    // último update

        public SimDataDefinition() { }

        public SimDataDefinition(string name, string units, double value, string? type = null)
        {
            Name = name;
            Units = units;
            Value = value;
            Type = type;
            Timestamp = DateTime.UtcNow;
        }

        public SimDataDefinition Clone()
        {
            return new SimDataDefinition
            {
                Name = this.Name,
                Units = this.Units,
                Value = this.Value,
                Type = this.Type,
                Timestamp = DateTime.UtcNow
            };
        }

        public override string ToString()
        {
            return $"{Name} = {Value} {Units}";
        }

        // Diccionario auxiliar si en algún punto el sistema necesita agrupar múltiples SimVars
        public static Dictionary<string, SimDataDefinition> FromList(IEnumerable<SimDataDefinition> list)
        {
            var dict = new Dictionary<string, SimDataDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in list)
            {
                if (!dict.ContainsKey(def.Name))
                    dict.Add(def.Name, def);
            }
            return dict;
        }
    }
}
