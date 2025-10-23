using System;

namespace SharedCockpitClient.Utils;

public static class Extensions
{
    public static bool IsDifferent(this double value, double other, double tolerance = 0.01)
        => Math.Abs(value - other) > tolerance;

    public static bool IsRecent(this DateTime timestamp, double seconds = 1)
    {
        if (timestamp == DateTime.MinValue)
        {
            return false;
        }

        return (DateTime.UtcNow - timestamp).TotalSeconds <= seconds;
    }
}
