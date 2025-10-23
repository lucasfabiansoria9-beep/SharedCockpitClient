using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public static class FlightDisplay
{
    public static void ShowReceivedMessage(string message)
    {
        Logger.Info($"📨 Datos recibidos por WebSocket: {message}");
    }

    public static void ShowSentToCopilot(string payload)
    {
        Logger.Info("[C#] Datos enviados al/los copiloto(s): " + payload);
    }

    public static void ShowSentToHost(string payload)
    {
        Logger.Info("[C#] Datos enviados al host: " + payload);
    }
}
