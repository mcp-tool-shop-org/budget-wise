using NextLedger.Domain.Common;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents a budget envelope (category) for allocating money.
/// Envelopes are the core of envelope budgeting - you allocate income to envelopes
/// and spend from those envelopes.
/// </summary>
public class Envelope : Entity
{
    public string Name { get; private set; }
    public string? GroupName { get; private set; }
    public string Color { get; private set; }
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsHidden { get; private set; }
    public Money? GoalAmount { get; private set; }
    public DateOnly? GoalDate { get; private set; }
    public string? Note { get; private set; }

    private Envelope() : base()
    {
        Name = string.Empty;
        Color = "#5B9BD5"; // Default blue
    }

    public static Envelope Create(
        string name,
        string? groupName = null,
        string? color = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Envelope name cannot be empty.", nameof(name));

        return new Envelope
        {
            Name = name.Trim(),
            GroupName = groupName?.Trim(),
            Color = color ?? "#5B9BD5",
            SortOrder = 0,
            IsActive = true,
            IsHidden = false
        };
    }

    public static Envelope CreateSystemEnvelope(string name, string color)
    {
        return new Envelope
        {
            Name = name,
            GroupName = null,
            Color = color,
            SortOrder = -1,
            IsActive = true,
            IsHidden = false
        };
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Envelope name cannot be empty.", nameof(newName));

        Name = newName.Trim();
        Touch();
    }

    public void SetGroup(string? groupName)
    {
        GroupName = groupName?.Trim();
        Touch();
    }

    public void SetColor(string color)
    {
        if (string.IsNullOrWhiteSpace(color))
            throw new ArgumentException("Color cannot be empty.", nameof(color));

        Color = color;
        Touch();
    }

    public void SetSortOrder(int order)
    {
        SortOrder = order;
        Touch();
    }

    public void SetNote(string? note)
    {
        Note = note?.Trim();
        Touch();
    }

    public void Hide()
    {
        IsHidden = true;
        Touch();
    }

    public void Show()
    {
        IsHidden = false;
        Touch();
    }

    public void Archive()
    {
        IsActive = false;
        Touch();
    }

    public void Unarchive()
    {
        IsActive = true;
        Touch();
    }

    public void SetGoal(Money amount, DateOnly? targetDate = null)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Goal amount cannot be negative.", nameof(amount));

        GoalAmount = amount;
        GoalDate = targetDate;
        Touch();
    }

    public void ClearGoal()
    {
        GoalAmount = null;
        GoalDate = null;
        Touch();
    }

    public bool HasGoal => GoalAmount is not null && !GoalAmount.Value.IsZero;
}
