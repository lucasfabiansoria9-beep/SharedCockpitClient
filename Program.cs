using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient;

class Program
{
    static async Task Main()
    {
        Logger.Info("🛫 Iniciando SharedCockpitClient...");
        using var sync = new SyncController(new SimConnectManager());
        await sync.RunAsync();
    }
}
