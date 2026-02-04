using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.App.Services.Notifications;
using NextLedger.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace NextLedger.App.ViewModels;

public sealed partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;
    private readonly IEngineErrorMessageMapper _errorMapper;

    [ObservableProperty]
    private BudgetViewState _state;

    [ObservableProperty]
    private ObservableCollection<EnvelopeRowViewModel> _envelopeRows = new();

    public BudgetViewModel(IBudgetEngine engine, INotificationService notifications, IEngineErrorMessageMapper errorMapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _errorMapper = errorMapper ?? throw new ArgumentNullException(nameof(errorMapper));

        var now = DateTime.Now;
        _state = BudgetViewState.Empty(now.Year, now.Month) with { IsLoading = true };

        _ = LoadAsync(_state.Year, _state.Month);
    }

    public bool IsLoading => State.IsLoading;
    public bool HasError => State.Errors.Count > 0;
    public bool HasEnvelopes => EnvelopeRows.Count > 0;
    public bool ShowEmptyState => !IsLoading && !HasError && !HasEnvelopes;

    public string ErrorText => State.Errors.Count == 0
        ? string.Empty
        : string.Join(Environment.NewLine, State.Errors.Select(e => e.Message));

    public string YearMonthText => $"{State.Year:D4}-{State.Month:D2}";

    public string ReadyToAssignText
        => State.Snapshot?.ReadyToAssign.ToFormattedString() ?? "$0.00";

    public bool IsOverbudgeted
        => State.Snapshot?.ReadyToAssign.IsNegative ?? false;

    /// <summary>
    /// True if there are envelopes with goals that could receive auto-assign funds.
    /// </summary>
    public bool HasGoalsToFund
    {
        get
        {
            var readyToAssign = State.Snapshot?.ReadyToAssign ?? Money.Zero;
            if (readyToAssign.IsNegative || readyToAssign.IsZero)
                return false;

            return State.Summary?.Envelopes.Any(e => e.HasGoal) ?? false;
        }
    }

    /// <summary>
    /// Contextual hint for auto-assign button.
    /// </summary>
    public string GoalsFundableHint
    {
        get
        {
            var envelopesWithGoals = State.Summary?.Envelopes.Count(e => e.HasGoal) ?? 0;
            if (envelopesWithGoals == 0)
                return string.Empty;

            return envelopesWithGoals == 1
                ? "Fund 1 envelope with a goal"
                : $"Fund {envelopesWithGoals} envelopes with goals";
        }
    }

    [RelayCommand]
    private async Task AutoAssignToGoalsAsync()
    {
        try
        {
            State = State with { IsLoading = true, Errors = Array.Empty<BudgetOperationError>() };
            OnDerivedPropertiesChanged();

            var result = await _engine.AutoAssignToGoalsAsync(
                new AutoAssignToGoalsRequest { Mode = AutoAssignMode.EarliestGoalDateFirst },
                State.Year,
                State.Month);

            if (!result.Success)
            {
                State = State with { IsLoading = false, Errors = result.Errors };
                OnDerivedPropertiesChanged();

                var (title, message) = _errorMapper.Map(result.Errors);
                _notifications.ShowError(title, message);
                return;
            }

            await LoadAsync(State.Year, State.Month);

            var changed = result.AllocationChanges.Count;
            _notifications.ShowSuccess(
                "Updated",
                changed == 0 ? "No envelopes changed." : $"Updated {changed} envelope(s).",
                duration: TimeSpan.FromSeconds(4));
        }
        catch (Exception)
        {
            State = State with
            {
                IsLoading = false,
                Errors = new[] { BudgetOperationError.Create("UNEXPECTED", "An unexpected error occurred. Open Diagnostics for details.") }
            };
            OnDerivedPropertiesChanged();

            _notifications.ShowErrorAction(
                "Couldn't auto-assign",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
    }

    /// <summary>
    /// Called when user commits an allocation change on an envelope row.
    /// </summary>
    public async Task SaveAllocationAsync(EnvelopeRowViewModel row)
    {
        if (row is null || !row.HasPendingChange)
            return;

        try
        {
            // Parse the new allocation amount
            if (!decimal.TryParse(row.AssignedText.Replace("$", "").Replace(",", "").Trim(), out var newAmount))
            {
                _notifications.ShowWarning("Invalid amount", "Please enter a valid number.");
                row.ResetAssigned();
                return;
            }

            var request = new AllocateToEnvelopeRequest
            {
                EnvelopeId = row.EnvelopeId,
                Amount = Money.USD(newAmount)
            };

            var result = await _engine.SetEnvelopeAllocationAsync(request, State.Year, State.Month);

            if (!result.Success)
            {
                var (title, message) = _errorMapper.Map(result.Errors);
                _notifications.ShowError(title, message);
                row.ResetAssigned();
                return;
            }

            // Reload to get updated ReadyToAssign and Available values
            await LoadAsync(State.Year, State.Month);

            _notifications.ShowSuccess("Assigned", $"Assigned {Money.USD(newAmount).ToFormattedString()} to {row.Name}.", TimeSpan.FromSeconds(2));
        }
        catch (Exception)
        {
            _notifications.ShowErrorAction(
                "Couldn't save allocation",
                "Try again. If it keeps happening, open Diagnostics.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
            row.ResetAssigned();
        }
    }

    private async Task LoadAsync(int year, int month)
    {
        try
        {
            State = State with { IsLoading = true, Errors = Array.Empty<BudgetOperationError>() };
            OnDerivedPropertiesChanged();

            var summary = await _engine.GetBudgetSummaryAsync(year, month);
            var snapshot = new BudgetSnapshotDto
            {
                Year = summary.Year,
                Month = summary.Month,
                IsClosed = summary.IsClosed,
                CarriedOver = summary.CarriedOver,
                TotalIncome = summary.TotalIncome,
                TotalAllocated = summary.TotalAllocated,
                TotalSpent = summary.TotalSpent,
                ReadyToAssign = summary.ReadyToAssign
            };

            State = State with { Summary = summary, Snapshot = snapshot, IsLoading = false };

            // Rebuild envelope rows
            EnvelopeRows.Clear();
            foreach (var env in summary.Envelopes.OrderBy(e => e.GroupName).ThenBy(e => e.Name))
            {
                EnvelopeRows.Add(new EnvelopeRowViewModel(
                    envelopeId: env.Id,
                    name: env.Name,
                    groupName: env.GroupName,
                    allocated: env.Allocated,
                    available: env.Available,
                    goalAmount: env.GoalAmount,
                    goalDate: env.GoalDate,
                    isOverspent: env.IsOverspent,
                    goalProgressPercent: env.GoalProgress,
                    onAllocationCommit: SaveAllocationAsync));
            }

            OnDerivedPropertiesChanged();
        }
        catch (Exception)
        {
            State = State with
            {
                IsLoading = false,
                Errors = new[] { BudgetOperationError.Create("UNEXPECTED", "An unexpected error occurred. Open Diagnostics for details.") }
            };
            OnDerivedPropertiesChanged();
        }
    }

    partial void OnStateChanged(BudgetViewState value) => OnDerivedPropertiesChanged();

    private void OnDerivedPropertiesChanged()
    {
        OnPropertyChanged(nameof(IsLoading));
        OnPropertyChanged(nameof(HasError));
        OnPropertyChanged(nameof(HasEnvelopes));
        OnPropertyChanged(nameof(ShowEmptyState));
        OnPropertyChanged(nameof(ErrorText));
        OnPropertyChanged(nameof(YearMonthText));
        OnPropertyChanged(nameof(ReadyToAssignText));
        OnPropertyChanged(nameof(IsOverbudgeted));
        OnPropertyChanged(nameof(HasGoalsToFund));
        OnPropertyChanged(nameof(GoalsFundableHint));
    }
}

/// <summary>
/// View model for a single envelope row with editable allocation.
/// </summary>
public sealed partial class EnvelopeRowViewModel : ObservableObject
{
    private readonly Func<EnvelopeRowViewModel, Task> _onAllocationCommit;
    private readonly string _originalAssignedText;
    private readonly Money _availableMoney;

    public Guid EnvelopeId { get; }
    public string Name { get; }
    public string? GroupName { get; }
    public string AvailableText { get; }
    public string GoalAmountText { get; }
    public string GoalDateText { get; }
    public bool IsOverspent { get; }
    public decimal GoalProgressPercent { get; }

    /// <summary>
    /// Hint showing overspend magnitude (e.g., "$42 over")
    /// </summary>
    public string OverspentHint
    {
        get
        {
            if (!IsOverspent)
                return string.Empty;

            var overAmount = _availableMoney.Abs();
            return $"{overAmount.ToFormattedString()} over";
        }
    }

    [ObservableProperty]
    private string _assignedText;

    [ObservableProperty]
    private bool _isEditing;

    public bool HasPendingChange => AssignedText != _originalAssignedText;

    public EnvelopeRowViewModel(
        Guid envelopeId,
        string name,
        string? groupName,
        Money allocated,
        Money available,
        Money? goalAmount,
        DateOnly? goalDate,
        bool isOverspent,
        decimal goalProgressPercent,
        Func<EnvelopeRowViewModel, Task> onAllocationCommit)
    {
        EnvelopeId = envelopeId;
        Name = name;
        GroupName = groupName;
        _availableMoney = available;
        AvailableText = available.ToFormattedString();
        GoalAmountText = goalAmount?.ToFormattedString() ?? string.Empty;
        GoalDateText = goalDate?.ToString("yyyy-MM-dd") ?? string.Empty;
        IsOverspent = isOverspent;
        GoalProgressPercent = goalProgressPercent;

        _assignedText = allocated.Amount.ToString("F2");
        _originalAssignedText = _assignedText;
        _onAllocationCommit = onAllocationCommit;
    }

    /// <summary>
    /// Called when Enter is pressed or focus leaves the allocation input.
    /// </summary>
    [RelayCommand]
    private async Task CommitAllocationAsync()
    {
        if (HasPendingChange)
        {
            await _onAllocationCommit(this);
        }
        IsEditing = false;
    }

    /// <summary>
    /// Called when Escape is pressed to cancel editing.
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        ResetAssigned();
        IsEditing = false;
    }

    public void ResetAssigned()
    {
        AssignedText = _originalAssignedText;
    }
}
