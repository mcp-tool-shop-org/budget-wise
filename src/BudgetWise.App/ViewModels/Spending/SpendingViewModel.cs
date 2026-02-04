using BudgetWise.Application.Interfaces;
using BudgetWise.App.Services.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudgetWise.App.ViewModels.Spending;

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

                    return new SpendingRow(
                        Name: e.Name,
                        GroupName: e.GroupName,
                        SpentText: spent.ToFormattedString(),
                        AvailableText: e.Available.ToFormattedString(),
                        IsOverspent: e.IsOverspent,
                        SpentPercent: percent);
                })
                .ToArray();

            OnPropertyChanged(nameof(YearMonthText));
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
        double SpentPercent);
}
