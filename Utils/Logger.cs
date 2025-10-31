using System;

namespace SharedCockpitClient.Utils
{
    public static class Logger
    {
        public static void Info(string message) =>
            Console.WriteLine($"[INFO] {DateTime.Now:HH:mm:ss} {message}");

        public static void Warn(string message) =>
            Console.WriteLine($"[WARN] {DateTime.Now:HH:mm:ss} {message}");

        public static void Error(string message) =>
            Console.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss} {message}");

        public static void Debug(string message)
        {
#if DEBUG
            Console.WriteLine($"[DEBUG] {DateTime.Now:HH:mm:ss} {message}");
#endif
        }
    }
}
