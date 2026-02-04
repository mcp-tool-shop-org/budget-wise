using System.Collections.ObjectModel;
using System.Globalization;
using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.App.Services.Notifications;
using BudgetWise.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BudgetWise.App.ViewModels.Reconciliation;

public sealed partial class ReconciliationViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;
    private readonly IEngineErrorMessageMapper _errorMapper;

    public ReconciliationViewModel(IBudgetEngine engine, INotificationService notifications, IEngineErrorMessageMapper errorMapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _errorMapper = errorMapper ?? throw new ArgumentNullException(nameof(errorMapper));

        var now = DateTimeOffset.Now;
        StatementDate = now;
        StatementEndingBalanceText = "";

        _ = LoadAccountsAsync();
    }

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorText = string.Empty;

    [ObservableProperty]
    private string _resultText = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<AccountDto> _accounts = Array.Empty<AccountDto>();

    [ObservableProperty]
    private AccountDto? _selectedAccount;

    [ObservableProperty]
    private DateTimeOffset _statementDate;

    [ObservableProperty]
    private string _statementEndingBalanceText;

    [ObservableProperty]
    private bool _createAdjustmentIfNeeded = true;

    public ObservableCollection<TransactionRow> Transactions { get; } = new();

    public string StatementYearMonthText => $"{StatementDate.Year:D4}-{StatementDate.Month:D2}";

    partial void OnSelectedAccountChanged(AccountDto? value)
    {
        _ = LoadTransactionsAsync(userInitiated: false);
    }

    partial void OnStatementDateChanged(DateTimeOffset value)
    {
        OnPropertyChanged(nameof(StatementYearMonthText));
        _ = LoadTransactionsAsync(userInitiated: false);
    }

    [RelayCommand]
    private Task RefreshAsync() => LoadTransactionsAsync(userInitiated: true);

    [RelayCommand]
    private void SelectAll()
    {
        foreach (var row in Transactions)
            row.IsSelected = true;
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var row in Transactions)
            row.IsSelected = false;
    }

    [RelayCommand]
    private async Task ReconcileAsync()
    {
        ErrorText = string.Empty;
        ResultText = string.Empty;

        if (SelectedAccount is null)
        {
            ErrorText = "Select an account to continue.";
            return;
        }

        if (!TryParseMoney(StatementEndingBalanceText, out var endingBalance, out var parseError))
        {
            ErrorText = parseError;
            return;
        }

        var selectedIds = Transactions
            .Where(t => t.IsSelected)
            .Select(t => t.Id)
            .ToList();

        if (selectedIds.Count == 0)
        {
            ErrorText = "Select at least one transaction to reconcile.";
            return;
        }

        IsLoading = true;
        try
        {
            var statementDateOnly = DateOnly.FromDateTime(StatementDate.DateTime);

            var result = await _engine.ReconcileAccountAsync(new ReconcileAccountRequest
            {
                AccountId = SelectedAccount.Id,
                StatementDate = statementDateOnly,
                StatementEndingBalance = endingBalance,
                TransactionIdsToReconcile = selectedIds,
                CreateAdjustmentIfNeeded = CreateAdjustmentIfNeeded
            });

            if (!result.Success)
            {
                var (title, message) = _errorMapper.Map(result.Errors);
                ErrorText = message;
                if (result.Errors.Count > 0 && result.Errors[0].Code == "UNEXPECTED")
                    _notifications.ShowErrorAction(title, message, NotificationActionKind.CopyDiagnostics, "Copy diagnostics");
                else
                    _notifications.ShowError(title, message);
                return;
            }

            var payload = result.Value;
            if (payload is null)
            {
                ErrorText = "Reconciliation completed, but returned no results.";
                return;
            }

            ResultText = $"Cleared: {payload.ClearedBalance.ToFormattedString()}  Difference: {payload.Difference.ToFormattedString()}";
            _notifications.ShowSuccess("Reconciled", ResultText);

            await LoadAccountsAsync(selectAccountId: SelectedAccount.Id);
            await LoadTransactionsAsync(userInitiated: false);
        }
        catch (Exception)
        {
            ErrorText = "Reconciliation failed due to an unexpected error. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t reconcile",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAccountsAsync(Guid? selectAccountId = null)
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;

            Accounts = await _engine.GetActiveAccountsAsync();

            if (Accounts.Count > 0)
            {
                var chosen = selectAccountId.HasValue
                    ? Accounts.FirstOrDefault(a => a.Id == selectAccountId.Value)
                    : SelectedAccount;

                SelectedAccount = chosen ?? Accounts[0];
            }
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t load accounts. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t load accounts",
                "Open Diagnostics for details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
            Accounts = Array.Empty<AccountDto>();
            SelectedAccount = null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTransactionsAsync(bool userInitiated)
    {
        Transactions.Clear();
        ResultText = string.Empty;

        if (SelectedAccount is null)
            return;

        IsLoading = true;
        try
        {
            ErrorText = string.Empty;

            var year = StatementDate.Year;
            var month = StatementDate.Month;

            var transactions = await _engine.GetAccountTransactionsAsync(SelectedAccount.Id, year, month);

            foreach (var tx in transactions
                         .Where(t => !t.IsReconciled)
                         .OrderByDescending(t => t.Date)
                         .ThenBy(t => t.Payee))
            {
                Transactions.Add(new TransactionRow(tx));
            }
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t load transactions. Open Diagnostics for details.";
            if (userInitiated)
                _notifications.ShowErrorAction(
                    "Couldn’t load transactions",
                    "Try again. If it keeps happening, open Diagnostics and copy details.",
                    NotificationActionKind.CopyDiagnostics,
                    "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool TryParseMoney(string text, out Money money, out string error)
    {
        money = Money.Zero;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter the statement ending balance.";
            return false;
        }

        // Be forgiving: accept "123.45" and also currency-prefixed inputs like "$123.45".
        var cleaned = text.Trim();
        cleaned = cleaned.Replace("$", string.Empty, StringComparison.Ordinal);

        if (!decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.CurrentCulture, out var amount)
            && !decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out amount))
        {
            error = "Statement ending balance must be a valid number.";
            return false;
        }

        money = new Money(amount);
        return true;
    }

    public sealed partial class TransactionRow : ObservableObject
    {
        public TransactionRow(TransactionDto dto)
        {
            Id = dto.Id;
            Date = dto.Date;
            Payee = dto.Payee;
            Amount = dto.Amount;
            AmountText = dto.Amount.ToFormattedString();
            ClearedText = dto.IsCleared ? "Cleared" : "Uncleared";
        }

        public Guid Id { get; }
        public DateOnly Date { get; }
        public string Payee { get; }
        public Money Amount { get; }
        public string AmountText { get; }
        public string ClearedText { get; }

        [ObservableProperty]
        private bool _isSelected;
    }
}
