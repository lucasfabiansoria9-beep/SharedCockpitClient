using System;

namespace SharedCockpitClient.Network
{
    public sealed record LanDiscoveryMessage(string RoomName, string Address, int Port, DateTime ReceivedUtc)
    {
        public string Display => $"{RoomName} — {Address}:{Port}";
    }
}
