
using SharedCockpitClient.FlightData;
using SharedCockpitClient.Network;
using SharedCockpitClient.Utils;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using Microsoft.FlightSimulator.SimConnect;


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
