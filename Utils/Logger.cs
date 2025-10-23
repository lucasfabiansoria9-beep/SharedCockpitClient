using System;
using System.Linq;

namespace SharedCockpitClient.Utils;

public static class Logger
{
    private static readonly object Sync = new();
    private static bool _debugEnabled = Environment.GetCommandLineArgs().Any(a => string.Equals(a, "--debug", StringComparison.OrdinalIgnoreCase));

    public static void EnableDebug() => _debugEnabled = true;

    public static void Info(string message) => Write(ConsoleColor.Cyan, message);

    public static void Warn(string message) => Write(ConsoleColor.Yellow, message);

    public static void Error(string message) => Write(ConsoleColor.Red, message);

    public static void Debug(string message)
    {
        if (!_debugEnabled)
        {
            return;
        }

        Write(ConsoleColor.DarkCyan, message);
    }

    private static void Write(ConsoleColor color, string message)
    {
        lock (Sync)
        {
            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ForegroundColor = previous;
        }
    }
}
