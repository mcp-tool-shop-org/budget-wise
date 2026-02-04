using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.App.Services.Notifications;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;

namespace BudgetWise.App.ViewModels.Transactions;

public sealed partial class TransactionsViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;
    private readonly IEngineErrorMessageMapper _errorMapper;
    private readonly List<SplitLineEditorRow> _splitLineSubscriptions = new();
    private readonly List<SplitLineEditorRow> _selectedSplitLineSubscriptions = new();

    public TransactionsViewModel(IBudgetEngine engine, INotificationService notifications, IEngineErrorMessageMapper errorMapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _errorMapper = errorMapper ?? throw new ArgumentNullException(nameof(errorMapper));

        var now = DateTime.Now;
        Year = now.Year;
        Month = now.Month;

        _ = InitializeAsync();
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
    private IReadOnlyList<AccountDto> _accounts = Array.Empty<AccountDto>();

    [ObservableProperty]
    private IReadOnlyList<EnvelopeDto> _envelopes = Array.Empty<EnvelopeDto>();

    [ObservableProperty]
    private AccountDto? _selectedAccount;

    [ObservableProperty]
    private EnvelopeDto? _selectedEnvelope;

    [ObservableProperty]
    private IReadOnlyList<TransactionDto> _transactions = Array.Empty<TransactionDto>();

    [ObservableProperty]
    private TransactionDto? _selectedTransaction;

    [ObservableProperty]
    private string _newPayee = string.Empty;

    [ObservableProperty]
    private string _newAmount = string.Empty;

    [ObservableProperty]
    private bool _newIsInflow;

    [ObservableProperty]
    private bool _newIsSplit;

    [ObservableProperty]
    private bool _isNewSplitTotalValid;

    public sealed partial class SplitLineEditorRow : ObservableObject
    {
        public IReadOnlyList<EnvelopeDto> AvailableEnvelopes { get; }

        public SplitLineEditorRow(IReadOnlyList<EnvelopeDto> availableEnvelopes)
        {
            AvailableEnvelopes = availableEnvelopes;
        }

        [ObservableProperty]
        private EnvelopeDto? _envelope;

        [ObservableProperty]
        private string _amount = string.Empty;
    }

    [ObservableProperty]
    private IReadOnlyList<SplitLineEditorRow> _splitLines = Array.Empty<SplitLineEditorRow>();

    [ObservableProperty]
    private string _splitSummaryText = string.Empty;

    [ObservableProperty]
    private bool _isEditingSelectedSplits;

    [ObservableProperty]
    private IReadOnlyList<SplitLineEditorRow> _selectedSplitLines = Array.Empty<SplitLineEditorRow>();

    [ObservableProperty]
    private bool _isSelectedSplitTotalValid;

    [ObservableProperty]
    private string _selectedSplitSummaryText = string.Empty;

    [ObservableProperty]
    private string _selectedEditPayee = string.Empty;

    [ObservableProperty]
    private string _selectedEditMemo = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _selectedEditDate = DateTimeOffset.Now;

    public bool IsOutflowEntry => !NewIsInflow;

    public bool HasSelectedTransaction => SelectedTransaction is not null;

    public bool CanEditSelectedSplits => SelectedTransaction is not null && SelectedTransaction.HasSplits;

    public bool CanAddTransaction
    {
        get
        {
            if (IsLoading)
                return false;

            if (SelectedAccount is null)
                return false;

            if (string.IsNullOrWhiteSpace(NewPayee))
                return false;

            if (!decimal.TryParse(NewAmount, out var amount) || amount <= 0m)
                return false;

            if (NewIsInflow)
                return true;

            if (NewIsSplit)
                return IsNewSplitTotalValid;

            return true;
        }
    }

    public bool CanSaveSelectedSplits
    {
        get
        {
            if (IsLoading)
                return false;

            if (!IsEditingSelectedSplits)
                return false;

            if (SelectedTransaction is null || !SelectedTransaction.HasSplits)
                return false;

            if (string.IsNullOrWhiteSpace(SelectedEditPayee))
                return false;

            return IsSelectedSplitTotalValid;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (SelectedAccount is null)
            return;

        await LoadTransactionsAsync(SelectedAccount.Id, userInitiated: true);
    }

    [RelayCommand]
    private async Task ToggleClearedAsync()
    {
        if (SelectedTransaction is null)
            return;

        ErrorText = string.Empty;
        IsLoading = true;
        try
        {
            if (SelectedTransaction.IsCleared)
            {
                var result = await _engine.MarkTransactionUnclearedAsync(SelectedTransaction.Id);
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

                _notifications.ShowSuccess("Updated", "Marked as uncleared.");
            }
            else
            {
                var result = await _engine.MarkTransactionClearedAsync(SelectedTransaction.Id);
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

                _notifications.ShowSuccess("Updated", "Marked as cleared.");
            }

            if (SelectedAccount is not null)
                await LoadTransactionsAsync(SelectedAccount.Id, userInitiated: false);
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t update cleared status. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t update",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task AddTransactionAsync()
    {
        if (SelectedAccount is null)
            return;

        ErrorText = string.Empty;

        if (!decimal.TryParse(NewAmount, out var amount) || amount <= 0)
        {
            ErrorText = "Enter an amount greater than 0.";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPayee))
        {
            ErrorText = "Enter a payee.";
            return;
        }

        IsLoading = true;
        try
        {
            var date = DateOnly.FromDateTime(DateTime.Today);

            if (NewIsInflow)
            {
                var result = await _engine.CreateInflowAsync(new CreateInflowRequest
                {
                    AccountId = SelectedAccount.Id,
                    Date = date,
                    Amount = new BudgetWise.Domain.ValueObjects.Money(amount),
                    Payee = NewPayee.Trim()
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

                _notifications.ShowSuccess("Added", $"Inflow · {NewPayee.Trim()} · {amount:0.00}");
            }
            else
            {
                if (NewIsSplit)
                {
                    if (SelectedEnvelope is not null)
                    {
                        ErrorText = "Split outflows can’t also select a single envelope.";
                        return;
                    }

                    if (SplitLines.Count == 0)
                    {
                        ErrorText = "Add at least one split line.";
                        return;
                    }

                    var parsedLines = new List<TransactionSplitLineRequest>();
                    decimal splitSum = 0m;

                    foreach (var line in SplitLines)
                    {
                        if (line.Envelope is null)
                        {
                            ErrorText = "Each split line must have an envelope.";
                            return;
                        }

                        if (!decimal.TryParse(line.Amount, out var lineAmount) || lineAmount <= 0)
                        {
                            ErrorText = "Each split line must have an amount greater than 0.";
                            return;
                        }

                        splitSum += lineAmount;
                        parsedLines.Add(new TransactionSplitLineRequest
                        {
                            EnvelopeId = line.Envelope.Id,
                            Amount = new BudgetWise.Domain.ValueObjects.Money(lineAmount)
                        });
                    }

                    if (splitSum != amount)
                    {
                        ErrorText = "Split amounts must add up to the transaction amount.";
                        return;
                    }

                    var result = await _engine.CreateOutflowAsync(new CreateOutflowRequest
                    {
                        AccountId = SelectedAccount.Id,
                        Date = date,
                        Amount = new BudgetWise.Domain.ValueObjects.Money(amount),
                        Payee = NewPayee.Trim(),
                        EnvelopeId = null,
                        SplitLines = parsedLines
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

                    _notifications.ShowSuccess("Added", $"Split outflow · {NewPayee.Trim()} · {amount:0.00}");

                    // Clear split editor
                    SplitLines = Array.Empty<SplitLineEditorRow>();
                    NewIsSplit = false;
                }
                else
                {
                    var result = await _engine.CreateOutflowAsync(new CreateOutflowRequest
                    {
                        AccountId = SelectedAccount.Id,
                        Date = date,
                        Amount = new BudgetWise.Domain.ValueObjects.Money(amount),
                        Payee = NewPayee.Trim(),
                        EnvelopeId = SelectedEnvelope?.Id
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

                    _notifications.ShowSuccess("Added", $"Outflow · {NewPayee.Trim()} · {amount:0.00}");
                }
            }

            NewPayee = string.Empty;
            NewAmount = string.Empty;
            await LoadTransactionsAsync(SelectedAccount.Id, userInitiated: false);
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t add the transaction. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t add transaction",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanAddTransaction));
        }
    }

    partial void OnSelectedAccountChanged(AccountDto? value)
    {
        if (value is null)
        {
            Transactions = Array.Empty<TransactionDto>();
            OnPropertyChanged(nameof(CanAddTransaction));
            return;
        }

        _ = LoadTransactionsAsync(value.Id, userInitiated: false);
        OnPropertyChanged(nameof(CanAddTransaction));
    }

    partial void OnSelectedTransactionChanged(TransactionDto? value)
    {
        OnPropertyChanged(nameof(HasSelectedTransaction));

        IsEditingSelectedSplits = false;
        SelectedSplitLines = Array.Empty<SplitLineEditorRow>();
        SelectedEditPayee = string.Empty;
        SelectedEditMemo = string.Empty;
        SelectedEditDate = DateTimeOffset.Now;

        if (value is not null && value.HasSplits)
        {
            SelectedEditPayee = value.Payee;
            SelectedEditMemo = value.Memo ?? string.Empty;
            SelectedEditDate = new DateTimeOffset(value.Date.ToDateTime(TimeOnly.MinValue));

            var lines = value.SplitLines
                .OrderBy(l => l.SortOrder)
                .Select(l =>
                {
                    var row = new SplitLineEditorRow(Envelopes);
                    row.Envelope = Envelopes.FirstOrDefault(e => e.Id == l.EnvelopeId);
                    row.Amount = l.Amount.Amount.ToString();
                    return row;
                })
                .ToList();

            SelectedSplitLines = lines;
            IsEditingSelectedSplits = true;
            AttachSelectedSplitLineSubscriptions();
            UpdateSelectedSplitSummary();
        }
        else
        {
            DetachSelectedSplitLineSubscriptions();
            SelectedSplitSummaryText = string.Empty;
            IsSelectedSplitTotalValid = false;
        }

        OnPropertyChanged(nameof(CanEditSelectedSplits));
        OnPropertyChanged(nameof(CanSaveSelectedSplits));
        SaveSelectedSplitsCommand.NotifyCanExecuteChanged();
    }

    partial void OnNewIsInflowChanged(bool value)
    {
        OnPropertyChanged(nameof(IsOutflowEntry));
        if (value)
        {
            SelectedEnvelope = null;
            NewIsSplit = false;
            SplitLines = Array.Empty<SplitLineEditorRow>();
            DetachSplitLineSubscriptions();
            SplitSummaryText = string.Empty;
            IsNewSplitTotalValid = false;
        }

        OnPropertyChanged(nameof(CanAddTransaction));
    }

    partial void OnNewIsSplitChanged(bool value)
    {
        if (value)
            SelectedEnvelope = null;

        if (!value)
        {
            DetachSplitLineSubscriptions();
            SplitLines = Array.Empty<SplitLineEditorRow>();
            SplitSummaryText = string.Empty;
            IsNewSplitTotalValid = false;
        }
        else
        {
            UpdateSplitSummary();
        }

        OnPropertyChanged(nameof(CanAddTransaction));
    }

    partial void OnNewAmountChanged(string value)
    {
        if (NewIsSplit)
            UpdateSplitSummary();

        OnPropertyChanged(nameof(CanAddTransaction));
    }

    partial void OnNewPayeeChanged(string value)
    {
        OnPropertyChanged(nameof(CanAddTransaction));
    }

    [RelayCommand]
    private void AddSplitLine()
    {
        var list = SplitLines.ToList();
        list.Add(new SplitLineEditorRow(Envelopes));
        SplitLines = list;
        AttachSplitLineSubscriptions();
        UpdateSplitSummary();
    }

    [RelayCommand]
    private void RemoveLastSplitLine()
    {
        var list = SplitLines.ToList();
        if (list.Count == 0)
            return;

        list.RemoveAt(list.Count - 1);
        SplitLines = list;
        AttachSplitLineSubscriptions();
        UpdateSplitSummary();
    }

    [RelayCommand]
    private void AddSelectedSplitLine()
    {
        if (SelectedTransaction is null || !SelectedTransaction.HasSplits)
            return;

        var list = SelectedSplitLines.ToList();
        list.Add(new SplitLineEditorRow(Envelopes));
        SelectedSplitLines = list;
        AttachSelectedSplitLineSubscriptions();
        UpdateSelectedSplitSummary();
    }

    [RelayCommand]
    private void RemoveLastSelectedSplitLine()
    {
        var list = SelectedSplitLines.ToList();
        if (list.Count == 0)
            return;

        list.RemoveAt(list.Count - 1);
        SelectedSplitLines = list;
        AttachSelectedSplitLineSubscriptions();
        UpdateSelectedSplitSummary();
    }

    private void AttachSplitLineSubscriptions()
    {
        DetachSplitLineSubscriptions();
        foreach (var row in SplitLines)
        {
            row.PropertyChanged += OnSplitLinePropertyChanged;
            _splitLineSubscriptions.Add(row);
        }
    }

    private void DetachSplitLineSubscriptions()
    {
        foreach (var row in _splitLineSubscriptions)
            row.PropertyChanged -= OnSplitLinePropertyChanged;
        _splitLineSubscriptions.Clear();
    }

    private void OnSplitLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SplitLineEditorRow.Amount) or nameof(SplitLineEditorRow.Envelope))
            UpdateSplitSummary();
    }

    private void AttachSelectedSplitLineSubscriptions()
    {
        DetachSelectedSplitLineSubscriptions();
        foreach (var row in SelectedSplitLines)
        {
            row.PropertyChanged += OnSelectedSplitLinePropertyChanged;
            _selectedSplitLineSubscriptions.Add(row);
        }
    }

    private void DetachSelectedSplitLineSubscriptions()
    {
        foreach (var row in _selectedSplitLineSubscriptions)
            row.PropertyChanged -= OnSelectedSplitLinePropertyChanged;
        _selectedSplitLineSubscriptions.Clear();
    }

    private void OnSelectedSplitLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(SplitLineEditorRow.Amount) or nameof(SplitLineEditorRow.Envelope))
            UpdateSelectedSplitSummary();
    }

    private void UpdateSplitSummary()
    {
        if (!NewIsSplit)
        {
            SplitSummaryText = string.Empty;
            IsNewSplitTotalValid = false;
            OnPropertyChanged(nameof(CanAddTransaction));
            return;
        }

        var total = 0m;
        var anyAmount = false;
        var allLinesValid = SplitLines.Count > 0;

        foreach (var line in SplitLines)
        {
            if (line.Envelope is null)
                allLinesValid = false;

            if (decimal.TryParse(line.Amount, out var a) && a > 0m)
            {
                total += a;
                anyAmount = true;
            }
            else
            {
                allLinesValid = false;
            }
        }

        var totalText = new BudgetWise.Domain.ValueObjects.Money(total).ToFormattedString();

        if (!decimal.TryParse(NewAmount, out var target) || target <= 0m)
        {
            SplitSummaryText = anyAmount ? $"Split total: {totalText}" : "Split total: (enter amounts)";
            IsNewSplitTotalValid = false;
            OnPropertyChanged(nameof(CanAddTransaction));
            return;
        }

        var targetText = new BudgetWise.Domain.ValueObjects.Money(target).ToFormattedString();
        SplitSummaryText = total == target
            ? $"Split total: {totalText} (OK)"
            : $"Split total: {totalText} (needs {targetText})";

        IsNewSplitTotalValid = allLinesValid && total == target;
        OnPropertyChanged(nameof(CanAddTransaction));
    }

    private void UpdateSelectedSplitSummary()
    {
        if (!IsEditingSelectedSplits)
        {
            SelectedSplitSummaryText = string.Empty;
            IsSelectedSplitTotalValid = false;
            OnPropertyChanged(nameof(CanSaveSelectedSplits));
            SaveSelectedSplitsCommand.NotifyCanExecuteChanged();
            return;
        }

        var allLinesValid = SelectedSplitLines.Count > 0;

        var total = 0m;
        foreach (var line in SelectedSplitLines)
        {
            if (line.Envelope is null)
                allLinesValid = false;

            if (decimal.TryParse(line.Amount, out var a) && a > 0m)
                total += a;
            else
                allLinesValid = false;
        }

        var totalText = new BudgetWise.Domain.ValueObjects.Money(total).ToFormattedString();
        if (SelectedTransaction is null)
        {
            SelectedSplitSummaryText = $"Split total: {totalText}";
            IsSelectedSplitTotalValid = false;
        }
        else
        {
            var target = SelectedTransaction.Amount.Amount;
            var targetText = SelectedTransaction.Amount.ToFormattedString();
            SelectedSplitSummaryText = total == target
                ? $"Split total: {totalText} (OK)"
                : $"Split total: {totalText} (needs {targetText})";

            IsSelectedSplitTotalValid = allLinesValid && total == target;
        }

        OnPropertyChanged(nameof(CanSaveSelectedSplits));
        SaveSelectedSplitsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand(CanExecute = nameof(CanSaveSelectedSplits))]
    private async Task SaveSelectedSplitsAsync()
    {
        if (SelectedTransaction is null || !SelectedTransaction.HasSplits)
            return;

        ErrorText = string.Empty;
        IsLoading = true;
        try
        {
            if (string.IsNullOrWhiteSpace(SelectedEditPayee))
            {
                ErrorText = "Enter a payee.";
                return;
            }

            var parsedLines = new List<TransactionSplitLineRequest>();
            decimal splitSum = 0m;

            foreach (var line in SelectedSplitLines)
            {
                if (line.Envelope is null)
                {
                    ErrorText = "Each split line must have an envelope.";
                    return;
                }

                if (!decimal.TryParse(line.Amount, out var lineAmount) || lineAmount <= 0)
                {
                    ErrorText = "Each split line must have an amount greater than 0.";
                    return;
                }

                splitSum += lineAmount;
                parsedLines.Add(new TransactionSplitLineRequest
                {
                    EnvelopeId = line.Envelope.Id,
                    Amount = new BudgetWise.Domain.ValueObjects.Money(lineAmount)
                });
            }

            if (parsedLines.Count == 0)
            {
                ErrorText = "Add at least one split line.";
                return;
            }

            if (splitSum != SelectedTransaction.Amount.Amount)
            {
                ErrorText = "Split amounts must add up to the original transaction amount.";
                return;
            }

            var result = await _engine.UpdateTransactionAsync(new UpdateTransactionRequest
            {
                Id = SelectedTransaction.Id,
                Date = DateOnly.FromDateTime(SelectedEditDate.DateTime),
                Amount = new BudgetWise.Domain.ValueObjects.Money(splitSum),
                Payee = SelectedEditPayee.Trim(),
                Memo = string.IsNullOrWhiteSpace(SelectedEditMemo) ? null : SelectedEditMemo.Trim(),
                EnvelopeId = null,
                SplitLines = parsedLines
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

            _notifications.ShowSuccess("Saved", "Changes saved.");

            if (SelectedAccount is not null)
                await LoadTransactionsAsync(SelectedAccount.Id, userInitiated: false);
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t save split changes. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t save",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
            OnPropertyChanged(nameof(CanSaveSelectedSplits));
            SaveSelectedSplitsCommand.NotifyCanExecuteChanged();
        }
    }

    partial void OnYearChanged(int value)
    {
        _ = ReloadPeriodAsync();
    }

    partial void OnMonthChanged(int value)
    {
        _ = ReloadPeriodAsync();
    }

    private async Task InitializeAsync()
    {
        IsLoading = true;
        try
        {
            Accounts = await _engine.GetActiveAccountsAsync();
            SelectedAccount = Accounts.FirstOrDefault(a => a.IsOnBudget) ?? Accounts.FirstOrDefault();

            Envelopes = await _engine.GetActiveEnvelopesAsync(Year, Month);
            SelectedEnvelope = null;
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t load accounts/envelopes. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t load data",
                "Open Diagnostics for details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ReloadPeriodAsync()
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;
            Envelopes = await _engine.GetActiveEnvelopesAsync(Year, Month);

            if (SelectedAccount is not null)
                Transactions = await _engine.GetAccountTransactionsAsync(SelectedAccount.Id, Year, Month);
        }
        catch (Exception)
        {
            ErrorText = "Couldn’t load data for this period. Open Diagnostics for details.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadTransactionsAsync(Guid accountId, bool userInitiated)
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;
            Transactions = await _engine.GetAccountTransactionsAsync(accountId, Year, Month);
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
            Transactions = Array.Empty<TransactionDto>();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
