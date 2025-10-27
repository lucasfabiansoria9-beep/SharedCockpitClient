using System.Text.Json;

namespace SharedCockpitClient.Network
{
    /// <summary>
    /// Mensaje genérico de cambio de estado. type = "stateChange".
    /// </summary>
    public sealed class StateChangeMessage
    {
        public string type { get; set; } = "stateChange";
        public string prop { get; set; } = string.Empty; // p.ej. "Flaps", "GearDown"
        public JsonElement value { get; set; }          // number | bool
        public string originId { get; set; } = string.Empty; // GUID emisor
        public int sequence { get; set; }                  // contador monótono por emisor
        public long serverTime { get; set; }               // host lo completa al rebroadcast
    }
}
