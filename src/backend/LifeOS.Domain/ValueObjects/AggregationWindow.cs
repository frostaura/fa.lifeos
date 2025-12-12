using LifeOS.Domain.Enums;

namespace LifeOS.Domain.ValueObjects;

public class AggregationWindow : IEquatable<AggregationWindow>
{
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public WindowType WindowType { get; }

    private AggregationWindow(DateTime startTime, DateTime endTime, WindowType windowType)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time", nameof(endTime));
        }

        StartTime = startTime;
        EndTime = endTime;
        WindowType = windowType;
    }

    public static AggregationWindow Daily(DateTime date)
    {
        var start = date.Date;
        var end = start.AddDays(1).AddTicks(-1);
        return new AggregationWindow(start, end, WindowType.Daily);
    }

    public static AggregationWindow Weekly(DateTime weekStart)
    {
        var start = weekStart.Date;
        var end = start.AddDays(7).AddTicks(-1);
        return new AggregationWindow(start, end, WindowType.Weekly);
    }

    public static AggregationWindow Monthly(int year, int month)
    {
        if (month < 1 || month > 12)
        {
            throw new ArgumentException("Month must be between 1 and 12", nameof(month));
        }

        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1).AddTicks(-1);
        return new AggregationWindow(start, end, WindowType.Monthly);
    }

    public static AggregationWindow Custom(DateTime startTime, DateTime endTime)
    {
        return new AggregationWindow(startTime, endTime, WindowType.Custom);
    }

    public bool Contains(DateTime timestamp)
    {
        return timestamp >= StartTime && timestamp <= EndTime;
    }

    public TimeSpan Duration => EndTime - StartTime + TimeSpan.FromTicks(1);

    public override bool Equals(object? obj) => obj is AggregationWindow other && Equals(other);

    public bool Equals(AggregationWindow? other)
    {
        if (other is null) return false;
        return StartTime == other.StartTime 
            && EndTime == other.EndTime 
            && WindowType == other.WindowType;
    }

    public override int GetHashCode() => HashCode.Combine(StartTime, EndTime, WindowType);

    public static bool operator ==(AggregationWindow? left, AggregationWindow? right)
    {
        return left is null ? right is null : left.Equals(right);
    }

    public static bool operator !=(AggregationWindow? left, AggregationWindow? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return WindowType switch
        {
            WindowType.Daily => $"Daily: {StartTime:yyyy-MM-dd}",
            WindowType.Weekly => $"Weekly: {StartTime:yyyy-MM-dd} to {EndTime:yyyy-MM-dd}",
            WindowType.Monthly => $"Monthly: {StartTime:yyyy-MM}",
            WindowType.Custom => $"Custom: {StartTime:yyyy-MM-dd HH:mm} to {EndTime:yyyy-MM-dd HH:mm}",
            _ => $"{StartTime} to {EndTime}"
        };
    }
}
