using NextLedger.Domain.Common;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents a monthly budget period.
/// Each month has its own allocations and calculations.
/// </summary>
public class BudgetPeriod : Entity
{
    public int Year { get; private set; }
    public int Month { get; private set; }
    public Money TotalIncome { get; private set; }
    public Money TotalAllocated { get; private set; }
    public Money TotalSpent { get; private set; }
    public Money CarriedOver { get; private set; }
    public bool IsClosed { get; private set; }

    private BudgetPeriod() : base()
    {
        TotalIncome = Money.Zero;
        TotalAllocated = Money.Zero;
        TotalSpent = Money.Zero;
        CarriedOver = Money.Zero;
    }

    public static BudgetPeriod Create(int year, int month, Money? carriedOver = null)
    {
        if (month < 1 || month > 12)
            throw new ArgumentOutOfRangeException(nameof(month), "Month must be between 1 and 12.");

        if (year < 1900 || year > 2100)
            throw new ArgumentOutOfRangeException(nameof(year), "Year must be reasonable.");

        return new BudgetPeriod
        {
            Year = year,
            Month = month,
            CarriedOver = carriedOver ?? Money.Zero,
            IsClosed = false
        };
    }

    public static BudgetPeriod ForCurrentMonth()
    {
        var now = DateTime.Today;
        return Create(now.Year, now.Month);
    }

    /// <summary>
    /// Money available to assign to envelopes.
    /// = Income + CarriedOver - TotalAllocated
    /// </summary>
    public Money ReadyToAssign => TotalIncome + CarriedOver - TotalAllocated;

    /// <summary>
    /// Money left unspent (across all envelopes).
    /// = TotalAllocated - TotalSpent
    /// </summary>
    public Money Remaining => TotalAllocated - TotalSpent;

    public DateRange GetDateRange() => DateRange.ForMonth(Year, Month);

    public string PeriodKey => $"{Year:D4}-{Month:D2}";

    public void UpdateIncome(Money income)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");

        TotalIncome = income;
        Touch();
    }

    public void UpdateAllocated(Money allocated)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");

        TotalAllocated = allocated;
        Touch();
    }

    public void UpdateSpent(Money spent)
    {
        if (IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");

        TotalSpent = spent;
        Touch();
    }

    public void Close()
    {
        IsClosed = true;
        Touch();
    }

    public void Reopen()
    {
        IsClosed = false;
        Touch();
    }

    public BudgetPeriod GetNextPeriod()
    {
        var nextMonth = Month == 12 ? 1 : Month + 1;
        var nextYear = Month == 12 ? Year + 1 : Year;
        return Create(nextYear, nextMonth, ReadyToAssign);
    }

    public override string ToString() => $"{Year}-{Month:D2}";
}
