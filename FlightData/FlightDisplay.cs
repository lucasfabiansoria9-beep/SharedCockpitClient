using System;
using System.Globalization;
using SharedCockpitClient.Utils;

namespace SharedCockpitClient.FlightData;

public static class FlightDisplay
{
    private static readonly object Sync = new();
    private static int _line = -1;
    private static string _lastText = string.Empty;
    private static ConsoleColor _lastColor = ConsoleColor.Gray;
    private static DateTime _lastWriteUtc = DateTime.MinValue;

    public static DateTime LastUpdateUtc => _lastWriteUtc;

    public static void Initialize()
    {
        lock (Sync)
        {
            if (_line >= 0)
            {
                return;
            }

            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine();
            _line = Math.Max(0, Console.CursorTop - 1);
        }
    }

    public static void ShowWaiting(string message = "⚠️ esperando conexión...")
    {
        WriteLine(message, ConsoleColor.Yellow);
    }

    public static void ShowFlightData(FlightSnapshot snapshot, bool isSynced)
    {
        Initialize();

        if (!snapshot.HasPrimaryFlightValues())
        {
            return;
        }

        var age = DateTime.UtcNow - snapshot.Timestamp;
        if (age > TimeSpan.FromSeconds(3))
        {
            ShowNoData();
            return;
        }

        var icon = isSynced ? "🟢" : "⚠️";
        var color = isSynced ? ConsoleColor.Green : ConsoleColor.Yellow;

        if (age > TimeSpan.FromSeconds(1))
        {
            icon = "⚠️";
            color = ConsoleColor.Yellow;
        }

        string line = string.Format(
            CultureInfo.InvariantCulture,
            "{0} 🛫 IAS: {1:F1} kts | VS: {2:F1} fpm | GS: {3:F1} kts | ALT: {4:F1} m | HDG: {5:F1}° | LAT: {6:F1} | LON: {7:F1}",
            icon,
            snapshot.Speed.IndicatedAirspeed,
            snapshot.Speed.VerticalSpeed,
            snapshot.Speed.GroundSpeed,
            snapshot.Position.Altitude,
            snapshot.Attitude.Heading,
            snapshot.Position.Latitude,
            snapshot.Position.Longitude);

        WriteLine(line, color);
    }

    public static void ShowLagging(TimeSpan age)
    {
        Initialize();
        var seconds = Math.Max(0, age.TotalSeconds);
        if (_lastText.StartsWith("🟢", StringComparison.Ordinal))
        {
            var rest = _lastText.Length > 1 ? _lastText[1..] : string.Empty;
            WriteLine($"⚠️{rest}", ConsoleColor.Yellow);
        }
        else if (_lastText.StartsWith("⚠️", StringComparison.Ordinal))
        {
            WriteLine(_lastText, ConsoleColor.Yellow);
        }
        else
        {
            var text = string.Format(
                CultureInfo.InvariantCulture,
                "⚠️ sincronización retrasada ({0:F1}s sin datos)",
                seconds);
            WriteLine(text, ConsoleColor.Yellow);
        }
    }

    public static void ShowNoData()
    {
        Initialize();
        WriteLine("🔴 sin datos", ConsoleColor.DarkGray);
    }

    private static void WriteLine(string text, ConsoleColor color)
    {
        lock (Sync)
        {
            if (_line < 0)
            {
                Initialize();
            }

            if (text == _lastText && color == _lastColor)
            {
                _lastWriteUtc = DateTime.UtcNow;
                return;
            }

            int previousLeft = Console.CursorLeft;
            int previousTop = Console.CursorTop;
            int safeWidth = SafeConsoleWidth();
            int targetTop = Math.Clamp(_line, 0, SafeConsoleHeight() - 1);

            Console.SetCursorPosition(0, targetTop);

            var originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            string output = text;
            if (output.Length >= safeWidth)
            {
                output = output[..Math.Max(0, safeWidth - 1)];
            }
            else
            {
                output = output.PadRight(Math.Max(0, safeWidth - 1));
            }

            Console.Write(output);
            Console.ForegroundColor = originalColor;

            _lastText = text;
            _lastColor = color;
            _lastWriteUtc = DateTime.UtcNow;

            Console.SetCursorPosition(previousLeft, previousTop);
        }
    }

    private static int SafeConsoleWidth()
    {
        try
        {
            return Console.BufferWidth;
        }
        catch
        {
            return 120;
        }
    }

    private static int SafeConsoleHeight()
    {
        try
        {
            return Console.BufferHeight;
        }
        catch
        {
            return int.MaxValue;
        }
    }
}
