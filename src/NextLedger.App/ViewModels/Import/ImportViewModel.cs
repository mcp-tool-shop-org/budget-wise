using System.Collections.ObjectModel;
using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.App.Services.Notifications;
using NextLedger.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace NextLedger.App.ViewModels.Import;

public sealed partial class ImportViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;
    private readonly IEngineErrorMessageMapper _errorMapper;

    public ImportViewModel(IBudgetEngine engine, INotificationService notifications, IEngineErrorMessageMapper errorMapper)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
        _errorMapper = errorMapper ?? throw new ArgumentNullException(nameof(errorMapper));
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
    private bool _hasHeaderRow = true;

    [ObservableProperty]
    private string _csvText = string.Empty;

    public ObservableCollection<ImportRow> Rows { get; } = new();

    [RelayCommand]
    private async Task PreviewAsync()
    {
        ErrorText = string.Empty;
        ResultText = string.Empty;
        Rows.Clear();

        if (SelectedAccount is null)
        {
            ErrorText = "Select an account to continue.";
            return;
        }

        if (string.IsNullOrWhiteSpace(CsvText))
        {
            ErrorText = "Paste CSV content to preview.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _engine.PreviewCsvImportAsync(new CsvImportPreviewRequest
            {
                AccountId = SelectedAccount.Id,
                CsvText = CsvText,
                HasHeaderRow = HasHeaderRow
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

            var preview = result.Value;
            if (preview is null)
            {
                ErrorText = "Preview completed, but returned no data.";
                return;
            }

            foreach (var row in preview.Rows)
            {
                Rows.Add(new ImportRow(row)
                {
                    IsSelected = row.Status == CsvImportRowStatus.New
                });
            }

            ResultText = $"New: {preview.NewCount}  Duplicates: {preview.DuplicateCount}  Invalid: {preview.InvalidCount}";
        }
        catch (Exception)
        {
            ErrorText = "Preview failed due to an unexpected error. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t preview import",
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
    private void SelectAllNew()
    {
        foreach (var row in Rows)
        {
            row.IsSelected = row.Status == CsvImportRowStatus.New;
        }
    }

    [RelayCommand]
    private void SelectNone()
    {
        foreach (var row in Rows)
            row.IsSelected = false;
    }

    [RelayCommand]
    private async Task CommitAsync()
    {
        ErrorText = string.Empty;
        ResultText = string.Empty;

        if (SelectedAccount is null)
        {
            ErrorText = "Select an account to continue.";
            return;
        }

        var selected = Rows
            .Where(r => r.IsSelected && r.Status == CsvImportRowStatus.New && r.CanCommit)
            .ToList();

        if (selected.Count == 0)
        {
            ErrorText = "Select at least one new row to import.";
            return;
        }

        IsLoading = true;
        try
        {
            var commitRequest = new CsvImportCommitRequest
            {
                AccountId = SelectedAccount.Id,
                Rows = selected.Select(r => r.ToCommitRow()).ToArray()
            };

            var result = await _engine.CommitCsvImportAsync(commitRequest);
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
                ErrorText = "Import completed, but returned no result.";
                return;
            }

            ResultText = $"Inserted: {payload.InsertedCount}  Skipped duplicates: {payload.SkippedDuplicateCount}";
            _notifications.ShowSuccess("Imported", ResultText);

            // Refresh preview so the user immediately sees what's left.
            await PreviewAsync();
        }
        catch (Exception)
        {
            ErrorText = "Import failed due to an unexpected error. Open Diagnostics for details.";
            _notifications.ShowErrorAction(
                "Couldn’t import",
                "Try again. If it keeps happening, open Diagnostics and copy details.",
                NotificationActionKind.CopyDiagnostics,
                "Copy diagnostics");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadAccountsAsync()
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;
            Accounts = await _engine.GetActiveAccountsAsync();
            SelectedAccount = Accounts.FirstOrDefault();
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

    public sealed partial class ImportRow : ObservableObject
    {
        public ImportRow(CsvImportRowDto dto)
        {
            RowNumber = dto.RowNumber;
            Date = dto.Date;
            Amount = dto.Amount;
            Payee = dto.Payee;
            Memo = dto.Memo;
            Status = dto.Status;
            Fingerprint = dto.Fingerprint;
            Error = dto.Error;

            DateText = dto.Date?.ToString("yyyy-MM-dd") ?? string.Empty;
            AmountText = dto.Amount?.ToFormattedString() ?? string.Empty;
            StatusText = dto.Status.ToString();
        }

        public int RowNumber { get; }
        public DateOnly? Date { get; }
        public string DateText { get; }
        public string Payee { get; }
        public string? Memo { get; }
        public CsvImportRowStatus Status { get; }
        public string StatusText { get; }
        public string? Fingerprint { get; }
        public string? Error { get; }
        public Money? Amount { get; }
        public string AmountText { get; }

        [ObservableProperty]
        private bool _isSelected;

        public bool CanCommit => Date.HasValue && Amount.HasValue && !string.IsNullOrWhiteSpace(Fingerprint);

        public CsvImportCommitRowDto ToCommitRow()
        {
            if (!CanCommit)
                throw new InvalidOperationException("Row is not commit-ready.");

            return new CsvImportCommitRowDto
            {
                RowNumber = RowNumber,
                Date = Date!.Value,
                Amount = Amount!.Value,
                Payee = Payee,
                Memo = Memo,
                Fingerprint = Fingerprint!
            };
        }
    }
}
