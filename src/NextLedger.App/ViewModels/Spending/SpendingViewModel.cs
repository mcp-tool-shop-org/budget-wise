using NextLedger.Application.Interfaces;
using NextLedger.App.Services.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextLedger.App.ViewModels.Spending;

public sealed partial class SpendingViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;

    public SpendingViewModel(IBudgetEngine engine, INotificationService notifications)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        var now = DateTime.Now;
        Year = now.Year;
        Month = now.Month;

        _ = LoadAsync(userInitiated: false);
    }

    [ObservableProperty]
    private int _year;

    [ObservableProperty]
    private int _month;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<SpendingRow> _rows = Array.Empty<SpendingRow>();

    public string YearMonthText => $"{Year:D4}-{Month:D2}";

    public bool HasOverspentEnvelopes => Rows.Any(r => r.IsOverspent);

    public int OverspentCount => Rows.Count(r => r.IsOverspent);

    public string OverspentSummary
    {
        get
        {
            var count = OverspentCount;
            if (count == 0)
                return string.Empty;
            return count == 1
                ? "1 envelope is overspent. Go to Budget to add more funds."
                : $"{count} envelopes are overspent. Go to Budget to add more funds.";
        }
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync(userInitiated: true);

    partial void OnYearChanged(int value) => _ = LoadAsync(userInitiated: false);

    partial void OnMonthChanged(int value) => _ = LoadAsync(userInitiated: false);

    private async Task LoadAsync(bool userInitiated)
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;

            var summary = await _engine.GetBudgetSummaryAsync(Year, Month);

            var maxSpent = summary.Envelopes
                .Select(e => e.Spent.Abs().Amount)
                .DefaultIfEmpty(0m)
                .Max();

            Rows = summary.Envelopes
                .OrderByDescending(e => e.Spent.Abs().Amount)
                .ThenBy(e => e.Name)
                .Select(e =>
                {
                    var spent = e.Spent.Abs();
                    var percent = maxSpent == 0m ? 0d : (double)(spent.Amount / maxSpent * 100m);

                    var overspentAmount = e.IsOverspent ? e.Available.Abs() : NextLedger.Domain.ValueObjects.Money.Zero;
                    var overspentHint = e.IsOverspent ? $"{overspentAmount.ToFormattedString()} over budget" : string.Empty;

                    return new SpendingRow(
                        Name: e.Name,
                        GroupName: e.GroupName,
                        SpentText: spent.ToFormattedString(),
                        AvailableText: e.Available.ToFormattedString(),
                        IsOverspent: e.IsOverspent,
                        OverspentHint: overspentHint,
                        SpentPercent: percent);
                })
                .ToArray();

            OnPropertyChanged(nameof(YearMonthText));
            OnPropertyChanged(nameof(HasOverspentEnvelopes));
            OnPropertyChanged(nameof(OverspentCount));
            OnPropertyChanged(nameof(OverspentSummary));
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t load spending. Open Diagnostics for details.";
            Rows = Array.Empty<SpendingRow>();

            if (userInitiated)
                _notifications.ShowErrorAction(
                    "Couldn’t load spending",
                    "Try again. If it keeps happening, open Diagnostics and copy details.",
                    NotificationActionKind.CopyDiagnostics,
                    "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public sealed record SpendingRow(
        string Name,
        string? GroupName,
        string SpentText,
        string AvailableText,
        bool IsOverspent,
        string OverspentHint,
        double SpentPercent);
}
