using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.App.Services.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudgetWise.App.ViewModels;

public sealed partial class BudgetViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;
    private readonly IEngineErrorMessageMapper _errorMapper;

    [ObservableProperty]
    private BudgetViewState _state;

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

    public IReadOnlyList<EnvelopeRow> EnvelopeRows
        => State.Summary?.Envelopes
               .OrderBy(e => e.GroupName)
               .ThenBy(e => e.Name)
               .Select(e => new EnvelopeRow(
                   Name: e.Name,
                   GroupName: e.GroupName,
                   AvailableText: e.Available.ToFormattedString(),
                   GoalAmountText: e.GoalAmount.HasValue ? e.GoalAmount.Value.ToFormattedString() : string.Empty,
                   GoalDateText: e.GoalDate.HasValue ? e.GoalDate.Value.ToString("yyyy-MM-dd") : string.Empty,
                   IsOverspent: e.IsOverspent,
                   GoalProgressPercent: e.GoalProgress))
               .ToArray()
           ?? Array.Empty<EnvelopeRow>();

    public sealed record EnvelopeRow(
        string Name,
        string? GroupName,
        string AvailableText,
        string GoalAmountText,
        string GoalDateText,
        bool IsOverspent,
        decimal GoalProgressPercent);

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
                "Couldn’t auto-assign",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
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
        OnPropertyChanged(nameof(EnvelopeRows));
    }
}
