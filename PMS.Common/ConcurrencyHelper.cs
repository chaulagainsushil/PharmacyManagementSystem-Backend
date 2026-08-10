namespace PMS.Common;

public static class ConcurrencyHelper
{
    /// <summary>
    /// Checks whether the incoming timestamp from the request matches the current database record timestamp.
    /// Uses a 1-second tolerance to account for subsecond rounding differences during JSON serialization.
    /// </summary>
    public static bool Matches(DateTime currentDbTimestamp, DateTime requestTimestamp)
    {
        return Math.Abs((currentDbTimestamp.ToUniversalTime() - requestTimestamp.ToUniversalTime()).TotalSeconds) < 1;
    }

    /// <summary>
    /// Checks nullable timestamps.
    /// </summary>
    public static bool Matches(DateTime? currentDbTimestamp, DateTime? requestTimestamp)
    {
        if (!currentDbTimestamp.HasValue || !requestTimestamp.HasValue)
            return false;

        return Matches(currentDbTimestamp.Value, requestTimestamp.Value);
    }
}
