namespace NextLedger.Domain.ValueObjects;

/// <summary>
/// Represents a range of dates (inclusive start and end).
/// Immutable value object for date-based queries.
/// </summary>
public readonly struct DateRange : IEquatable<DateRange>
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (end < start)
            throw new ArgumentException("End date cannot be before start date.", nameof(end));

        Start = start;
        End = end;
    }

    public int TotalDays => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => date >= Start && date <= End;

    public bool Overlaps(DateRange other)
        => Start <= other.End && End >= other.Start;

    public static DateRange ForMonth(int year, int month)
    {
        var start = new DateOnly(year, month, 1);
        var end = start.AddMonths(1).AddDays(-1);
        return new DateRange(start, end);
    }

    public static DateRange ForYear(int year)
    {
        var start = new DateOnly(year, 1, 1);
        var end = new DateOnly(year, 12, 31);
        return new DateRange(start, end);
    }

    public static DateRange ThisMonth()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        return ForMonth(today.Year, today.Month);
    }

    public static DateRange LastNDays(int days)
    {
        var end = DateOnly.FromDateTime(DateTime.Today);
        var start = end.AddDays(-(days - 1));
        return new DateRange(start, end);
    }

    public bool Equals(DateRange other)
        => Start == other.Start && End == other.End;

    public override bool Equals(object? obj)
        => obj is DateRange other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Start, End);

    public static bool operator ==(DateRange left, DateRange right) => left.Equals(right);
    public static bool operator !=(DateRange left, DateRange right) => !left.Equals(right);

    public override string ToString()
        => Start == End ? Start.ToString("yyyy-MM-dd") : $"{Start:yyyy-MM-dd} to {End:yyyy-MM-dd}";
}
