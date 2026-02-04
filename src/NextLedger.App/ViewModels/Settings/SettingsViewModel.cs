using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Application.Services;
using NextLedger.App.Services;
using NextLedger.App.Services.Notifications;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;

namespace NextLedger.App.ViewModels.Settings;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly IBudgetEngine _engine;
    private readonly INotificationService _notifications;

    public SettingsViewModel(IBudgetEngine engine, INotificationService notifications)
    {
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));

        _ = LoadAsync();
    }

    // ========== State ==========

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _errorText = string.Empty;

    // ========== Net Worth Summary ==========

    [ObservableProperty]
    private string _netWorthText = "$0.00";

    [ObservableProperty]
    private string _totalAssetsText = "$0.00";

    [ObservableProperty]
    private string _totalLiabilitiesText = "$0.00";

    [ObservableProperty]
    private string _totalOnBudgetText = "$0.00";

    [ObservableProperty]
    private string _totalOffBudgetText = "$0.00";

    [ObservableProperty]
    private string _xrpTotalText = "0.000000 XRP";

    [ObservableProperty]
    private bool _hasXrpAccounts;

    // ========== Accounts ==========

    [ObservableProperty]
    private IReadOnlyList<AccountRow> _accounts = Array.Empty<AccountRow>();

    [ObservableProperty]
    private IReadOnlyList<AccountRow> _closedAccounts = Array.Empty<AccountRow>();

    [ObservableProperty]
    private bool _showClosedAccounts;

    // New account form
    [ObservableProperty]
    private string _newAccountName = string.Empty;

    [ObservableProperty]
    private AccountType _newAccountType = AccountType.Checking;

    [ObservableProperty]
    private string _newAccountBalance = "0.00";

    [ObservableProperty]
    private bool _newAccountOnBudget = true;

    // Edit account
    [ObservableProperty]
    private AccountRow? _editingAccount;

    [ObservableProperty]
    private string _editAccountName = string.Empty;

    [ObservableProperty]
    private bool _editAccountOnBudget;

    // ========== Envelopes ==========

    [ObservableProperty]
    private IReadOnlyList<EnvelopeRow> _envelopes = Array.Empty<EnvelopeRow>();

    [ObservableProperty]
    private IReadOnlyList<EnvelopeRow> _archivedEnvelopes = Array.Empty<EnvelopeRow>();

    [ObservableProperty]
    private bool _showArchivedEnvelopes;

    [ObservableProperty]
    private IReadOnlyList<string> _groupNames = Array.Empty<string>();

    // New envelope form
    [ObservableProperty]
    private string _newEnvelopeName = string.Empty;

    [ObservableProperty]
    private string _newEnvelopeGroup = string.Empty;

    [ObservableProperty]
    private string _newEnvelopeColor = "#5B9BD5";

    // Edit envelope
    [ObservableProperty]
    private EnvelopeRow? _editingEnvelope;

    [ObservableProperty]
    private string _editEnvelopeName = string.Empty;

    [ObservableProperty]
    private string _editEnvelopeGroup = string.Empty;

    [ObservableProperty]
    private string _editEnvelopeColor = string.Empty;

    // ========== XRPL ==========

    [ObservableProperty]
    private IReadOnlyList<XrplAccountRow> _xrplAccounts = Array.Empty<XrplAccountRow>();

    // New XRPL address form
    [ObservableProperty]
    private string _newXrplName = string.Empty;

    [ObservableProperty]
    private string _newXrplAddress = string.Empty;

    [ObservableProperty]
    private string _newXrplNetwork = "mainnet";

    // XRPL status
    [ObservableProperty]
    private string _xrplStatusMessage = "Not checked yet";

    [ObservableProperty]
    private string _xrplStatusIcon = "\uE946"; // Info icon

    [ObservableProperty]
    private string _xrplStatusColor = "Gray";

    public IReadOnlyList<string> XrplNetworks { get; } = new[] { "mainnet", "testnet" };

    // ========== Computed ==========

    public bool HasClosedAccounts => ClosedAccounts.Count > 0;
    public bool HasArchivedEnvelopes => ArchivedEnvelopes.Count > 0;

    // Filter out ExternalXrpl from regular account types (use XRPL tab instead)
    public IReadOnlyList<AccountType> AccountTypes { get; } = Enum.GetValues<AccountType>()
        .Where(t => t != AccountType.ExternalXrpl)
        .ToArray();

    public string[] PredefinedColors { get; } = new[]
    {
        "#5B9BD5", // Blue
        "#70AD47", // Green
        "#ED7D31", // Orange
        "#FFC000", // Yellow
        "#9E480E", // Brown
        "#7030A0", // Purple
        "#C00000", // Red
        "#00B0F0", // Light Blue
        "#00B050", // Bright Green
        "#BFBFBF"  // Gray
    };

    // ========== Load ==========

    [RelayCommand]
    private Task RefreshAsync() => LoadAsync();

    private async Task LoadAsync()
    {
        IsLoading = true;
        try
        {
            ErrorText = string.Empty;

            // Load accounts
            var allAccounts = await _engine.GetAllAccountsAsync();
            Accounts = allAccounts
                .Where(a => a.IsActive)
                .OrderBy(a => a.Name)
                .Select(a => new AccountRow(a.Id, a.Name, a.Type, a.Balance.ToFormattedString(), a.IsOnBudget, a.IsActive, a.TypeDisplayName))
                .ToArray();

            ClosedAccounts = allAccounts
                .Where(a => !a.IsActive)
                .OrderBy(a => a.Name)
                .Select(a => new AccountRow(a.Id, a.Name, a.Type, a.Balance.ToFormattedString(), a.IsOnBudget, a.IsActive, a.TypeDisplayName))
                .ToArray();

            // Load envelopes
            var allEnvelopes = await _engine.GetAllEnvelopesAsync();
            Envelopes = allEnvelopes
                .Where(e => e.IsActive)
                .OrderBy(e => e.GroupName ?? "")
                .ThenBy(e => e.Name)
                .Select(e => new EnvelopeRow(e.Id, e.Name, e.GroupName, e.Color, e.IsActive, e.IsHidden))
                .ToArray();

            ArchivedEnvelopes = allEnvelopes
                .Where(e => !e.IsActive)
                .OrderBy(e => e.Name)
                .Select(e => new EnvelopeRow(e.Id, e.Name, e.GroupName, e.Color, e.IsActive, e.IsHidden))
                .ToArray();

            // Load group names for autocomplete
            GroupNames = await _engine.GetEnvelopeGroupNamesAsync();

            // Load XRPL accounts
            var xrplAccounts = await _engine.GetXrplAccountsAsync();
            XrplAccounts = xrplAccounts
                .Select(a => new XrplAccountRow(
                    a.Id,
                    a.Name,
                    a.BalanceText,
                    a.ExternalAddress,
                    a.ExternalNetwork,
                    a.LastExternalSyncAt,
                    a.ExternalReserveDrops,
                    a.ReserveXrp,
                    a.SpendableXrpBalance))
                .ToArray();

            // Load net worth summary (includes XRP)
            var summary = await _engine.GetAccountsSummaryAsync();
            NetWorthText = summary.NetWorth.ToFormattedString();
            TotalAssetsText = summary.TotalAssets.ToFormattedString();
            TotalLiabilitiesText = summary.TotalLiabilities.ToFormattedString();
            TotalOnBudgetText = summary.TotalOnBudget.ToFormattedString();
            TotalOffBudgetText = summary.TotalOffBudget.ToFormattedString();

            // Calculate XRP total (external ledger accounts only)
            var xrpTotal = xrplAccounts.Sum(a => a.Balance.Amount);
            XrpTotalText = $"{xrpTotal:N6} XRP";
            HasXrpAccounts = xrplAccounts.Count > 0;

            OnPropertyChanged(nameof(HasClosedAccounts));
            OnPropertyChanged(nameof(HasArchivedEnvelopes));
        }
        catch (Exception ex)
        {
            ErrorText = $"Couldn't load settings: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ========== Account Commands ==========

    [RelayCommand]
    private async Task CreateAccountAsync()
    {
        if (string.IsNullOrWhiteSpace(NewAccountName))
        {
            _notifications.ShowWarning("Name required", "Please enter an account name.");
            return;
        }

        if (!decimal.TryParse(NewAccountBalance, out var balance))
            balance = 0m;

        try
        {
            await _engine.CreateAccountAsync(new CreateAccountRequest
            {
                Name = NewAccountName.Trim(),
                Type = NewAccountType,
                InitialBalance = Money.USD(balance),
                IsOnBudget = NewAccountOnBudget
            });

            _notifications.ShowSuccess("Account created", $"'{NewAccountName}' has been added.");

            // Reset form
            NewAccountName = string.Empty;
            NewAccountType = AccountType.Checking;
            NewAccountBalance = "0.00";
            NewAccountOnBudget = true;

            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't create account", ex.Message);
        }
    }

    [RelayCommand]
    private void StartEditAccount(AccountRow account)
    {
        EditingAccount = account;
        EditAccountName = account.Name;
        EditAccountOnBudget = account.IsOnBudget;
    }

    [RelayCommand]
    private void CancelEditAccount()
    {
        EditingAccount = null;
        EditAccountName = string.Empty;
        EditAccountOnBudget = true;
    }

    [RelayCommand]
    private async Task SaveAccountAsync()
    {
        if (EditingAccount is null)
            return;

        if (string.IsNullOrWhiteSpace(EditAccountName))
        {
            _notifications.ShowWarning("Name required", "Account name cannot be empty.");
            return;
        }

        try
        {
            await _engine.UpdateAccountAsync(new UpdateAccountRequest
            {
                Id = EditingAccount.Id,
                Name = EditAccountName.Trim(),
                IsOnBudget = EditAccountOnBudget
            });

            _notifications.ShowSuccess("Account updated", $"'{EditAccountName}' has been saved.");

            EditingAccount = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't update account", ex.Message);
        }
    }

    [RelayCommand]
    private async Task CloseAccountAsync(AccountRow account)
    {
        try
        {
            await _engine.CloseAccountAsync(account.Id);
            _notifications.ShowSuccess("Account closed", $"'{account.Name}' has been closed. You can reopen it anytime.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't close account", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ReopenAccountAsync(AccountRow account)
    {
        try
        {
            await _engine.ReopenAccountAsync(account.Id);
            _notifications.ShowSuccess("Account reopened", $"'{account.Name}' is active again.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't reopen account", ex.Message);
        }
    }

    // ========== Envelope Commands ==========

    [RelayCommand]
    private async Task CreateEnvelopeAsync()
    {
        if (string.IsNullOrWhiteSpace(NewEnvelopeName))
        {
            _notifications.ShowWarning("Name required", "Please enter an envelope name.");
            return;
        }

        try
        {
            await _engine.CreateEnvelopeAsync(
                NewEnvelopeName.Trim(),
                string.IsNullOrWhiteSpace(NewEnvelopeGroup) ? null : NewEnvelopeGroup.Trim(),
                NewEnvelopeColor);

            _notifications.ShowSuccess("Envelope created", $"'{NewEnvelopeName}' has been added.");

            // Reset form
            NewEnvelopeName = string.Empty;
            NewEnvelopeGroup = string.Empty;
            NewEnvelopeColor = "#5B9BD5";

            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't create envelope", ex.Message);
        }
    }

    [RelayCommand]
    private void StartEditEnvelope(EnvelopeRow envelope)
    {
        EditingEnvelope = envelope;
        EditEnvelopeName = envelope.Name;
        EditEnvelopeGroup = envelope.GroupName ?? string.Empty;
        EditEnvelopeColor = envelope.Color;
    }

    [RelayCommand]
    private void CancelEditEnvelope()
    {
        EditingEnvelope = null;
        EditEnvelopeName = string.Empty;
        EditEnvelopeGroup = string.Empty;
        EditEnvelopeColor = "#5B9BD5";
    }

    [RelayCommand]
    private async Task SaveEnvelopeAsync()
    {
        if (EditingEnvelope is null)
            return;

        if (string.IsNullOrWhiteSpace(EditEnvelopeName))
        {
            _notifications.ShowWarning("Name required", "Envelope name cannot be empty.");
            return;
        }

        try
        {
            await _engine.UpdateEnvelopeAsync(new UpdateEnvelopeRequest
            {
                Id = EditingEnvelope.Id,
                Name = EditEnvelopeName.Trim(),
                GroupName = string.IsNullOrWhiteSpace(EditEnvelopeGroup) ? "" : EditEnvelopeGroup.Trim(),
                Color = EditEnvelopeColor
            });

            _notifications.ShowSuccess("Envelope updated", $"'{EditEnvelopeName}' has been saved.");

            EditingEnvelope = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't update envelope", ex.Message);
        }
    }

    [RelayCommand]
    private async Task ArchiveEnvelopeAsync(EnvelopeRow envelope)
    {
        try
        {
            await _engine.ArchiveEnvelopeAsync(envelope.Id);
            _notifications.ShowSuccess("Envelope archived", $"'{envelope.Name}' has been archived. You can restore it anytime.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't archive envelope", ex.Message);
        }
    }

    [RelayCommand]
    private async Task UnarchiveEnvelopeAsync(EnvelopeRow envelope)
    {
        try
        {
            await _engine.UnarchiveEnvelopeAsync(envelope.Id);
            _notifications.ShowSuccess("Envelope restored", $"'{envelope.Name}' is active again.");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't restore envelope", ex.Message);
        }
    }

    // ========== XRPL Commands ==========

    [RelayCommand]
    private async Task TrackXrplAddressAsync()
    {
        if (string.IsNullOrWhiteSpace(NewXrplName))
        {
            _notifications.ShowWarning("Name required", "Please enter a display name for this address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(NewXrplAddress))
        {
            _notifications.ShowWarning("Address required", "Please enter an XRPL r-address.");
            return;
        }

        try
        {
            await _engine.TrackXrplAddressAsync(new TrackXrplAddressRequest
            {
                Name = NewXrplName.Trim(),
                Address = NewXrplAddress.Trim(),
                Network = NewXrplNetwork
            });

            _notifications.ShowSuccess("Address tracked", $"Now tracking '{NewXrplName}' on XRPL {NewXrplNetwork}.");

            // Reset form
            NewXrplName = string.Empty;
            NewXrplAddress = string.Empty;
            NewXrplNetwork = "mainnet";

            await LoadAsync();
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Couldn't track address", ex.Message);
        }
    }

    [RelayCommand]
    private async Task RefreshXrplStatusAsync()
    {
        try
        {
            XrplStatusMessage = "Checking connection...";
            XrplStatusIcon = "\uE895"; // Sync icon
            XrplStatusColor = "Gray";

            var status = await _engine.GetXrplNetworkStatusAsync();

            if (status.IsConnected)
            {
                XrplStatusMessage = $"Connected to XRPL. Validated ledger: {status.ValidatedLedgerIndex:N0}";
                XrplStatusIcon = "\uE8FB"; // Checkmark
                XrplStatusColor = "Green";
            }
            else
            {
                XrplStatusMessage = status.ErrorMessage ?? "Unable to connect to XRPL.";
                XrplStatusIcon = "\uE783"; // Warning
                XrplStatusColor = "Orange";
            }
        }
        catch (Exception ex)
        {
            XrplStatusMessage = $"Error: {ex.Message}";
            XrplStatusIcon = "\uE711"; // Error
            XrplStatusColor = "Red";
        }
    }

    [RelayCommand]
    private async Task SyncXrplAccountAsync(XrplAccountRow account)
    {
        try
        {
            _notifications.ShowInfo("Syncing...", $"Fetching balance for {account.Name}");

            var result = await _engine.SyncXrplAccountAsync(account.Id);

            if (result.Success)
            {
                _notifications.ShowSuccess("Synced", $"{account.Name}: {result.BalanceXrp:N6} XRP");
                await LoadAsync();
            }
            else
            {
                _notifications.ShowWarning("Sync failed", result.ErrorMessage ?? "Unknown error");
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Sync error", ex.Message);
        }
    }

    // ========== XRPL Detail View Commands ==========

    [ObservableProperty]
    private XrplAccountRow? _selectedXrplAccount;

    [ObservableProperty]
    private IReadOnlyList<XrplTransactionRow> _xrplTransactions = Array.Empty<XrplTransactionRow>();

    [ObservableProperty]
    private string _xrplReconciliationStatus = string.Empty;

    [ObservableProperty]
    private bool _isXrplDetailVisible;

    [ObservableProperty]
    private bool _isLoadingXrplDetail;

    [RelayCommand]
    private async Task ViewXrplAccountDetailAsync(XrplAccountRow account)
    {
        SelectedXrplAccount = account;
        IsXrplDetailVisible = true;
        IsLoadingXrplDetail = true;

        try
        {
            // Load transaction history
            var historyResult = await _engine.GetXrplTransactionHistoryAsync(account.Id, 20);
            if (historyResult.Success)
            {
                XrplTransactions = historyResult.Transactions
                    .Select(tx => new XrplTransactionRow(
                        tx.Hash,
                        tx.Timestamp,
                        tx.TransactionType,
                        tx.Category.ToString(),
                        tx.Summary,
                        tx.AmountDrops,
                        tx.AmountXrp,
                        tx.FeeXrp,
                        tx.ExplorerUrl,
                        tx.IsSuccess,
                        FormatCategoryIcon(tx.Category)))
                    .ToArray();
            }
            else
            {
                XrplTransactions = Array.Empty<XrplTransactionRow>();
                _notifications.ShowWarning("Transaction history", historyResult.ErrorMessage ?? "Could not load transactions.");
            }

            // Load reconciliation status
            var recon = await _engine.GetXrplReconciliationAsync(account.Id);
            XrplReconciliationStatus = recon.Explanation;
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Load failed", ex.Message);
        }
        finally
        {
            IsLoadingXrplDetail = false;
        }
    }

    [RelayCommand]
    private void CloseXrplDetail()
    {
        IsXrplDetailVisible = false;
        SelectedXrplAccount = null;
        XrplTransactions = Array.Empty<XrplTransactionRow>();
        XrplReconciliationStatus = string.Empty;
    }

    private static string FormatCategoryIcon(XrplTransactionCategory category) => category switch
    {
        XrplTransactionCategory.IncomingPayment => "\uE896", // Download arrow
        XrplTransactionCategory.OutgoingPayment => "\uE898", // Upload arrow
        XrplTransactionCategory.FeeOnly => "\uE8C8", // Tag
        XrplTransactionCategory.ReserveChange => "\uE72E", // Lock
        XrplTransactionCategory.AccountActivation => "\uE8FA", // Star
        _ => "\uE946" // Info
    };

    // ========== XRPL Intent Layer (Phase 8) ==========
    // Plans, not executions — NextLedger never signs or submits.

    [ObservableProperty]
    private bool _isTransferIntentVisible;

    [ObservableProperty]
    private string _transferDestinationAddress = string.Empty;

    [ObservableProperty]
    private string _transferAmountXrp = string.Empty;

    [ObservableProperty]
    private string _transferDestinationTag = string.Empty;

    [ObservableProperty]
    private string _transferMemo = string.Empty;

    [ObservableProperty]
    private string _transferUserNote = string.Empty;

    // Intent validation preview
    [ObservableProperty]
    private bool _hasIntentValidation;

    [ObservableProperty]
    private bool _intentIsValid;

    [ObservableProperty]
    private string _intentValidationMessage = string.Empty;

    [ObservableProperty]
    private string _intentCurrentBalance = string.Empty;

    [ObservableProperty]
    private string _intentProjectedBalance = string.Empty;

    [ObservableProperty]
    private string _intentEstimatedFee = string.Empty;

    [ObservableProperty]
    private string _intentReserveRequirement = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<string> _intentWarnings = Array.Empty<string>();

    [ObservableProperty]
    private XrplIntentDto? _pendingIntent;

    /// <summary>
    /// Opens the Transfer Intent dialog for the selected XRPL account.
    /// This creates a PLAN, not an execution.
    /// </summary>
    [RelayCommand]
    private void ShowTransferIntent()
    {
        if (SelectedXrplAccount is null)
        {
            _notifications.ShowWarning("No account selected", "Please select an XRPL account first.");
            return;
        }

        // Reset the form
        TransferDestinationAddress = string.Empty;
        TransferAmountXrp = string.Empty;
        TransferDestinationTag = string.Empty;
        TransferMemo = string.Empty;
        TransferUserNote = string.Empty;
        HasIntentValidation = false;
        PendingIntent = null;

        IsTransferIntentVisible = true;
    }

    /// <summary>
    /// Closes the Transfer Intent dialog without taking action.
    /// </summary>
    [RelayCommand]
    private void CancelTransferIntent()
    {
        IsTransferIntentVisible = false;
        HasIntentValidation = false;
        PendingIntent = null;
    }

    /// <summary>
    /// Validates the Transfer Intent and shows the "Future Ledger" preview.
    /// Does NOT execute anything — just shows what would happen.
    /// </summary>
    [RelayCommand]
    private async Task ValidateTransferIntentAsync()
    {
        if (SelectedXrplAccount is null)
            return;

        if (string.IsNullOrWhiteSpace(TransferDestinationAddress))
        {
            _notifications.ShowWarning("Destination required", "Please enter a destination XRPL address.");
            return;
        }

        if (!decimal.TryParse(TransferAmountXrp, out var amount) || amount <= 0)
        {
            _notifications.ShowWarning("Invalid amount", "Please enter a valid XRP amount greater than 0.");
            return;
        }

        try
        {
            var intentService = AppHost.Current.Services.GetRequiredService<XrplIntentService>();

            uint? destTag = null;
            if (!string.IsNullOrWhiteSpace(TransferDestinationTag) && uint.TryParse(TransferDestinationTag, out var tag))
                destTag = tag;

            var request = new CreateTransferIntentRequest
            {
                SourceAccountId = SelectedXrplAccount.Id,
                DestinationAddress = TransferDestinationAddress.Trim(),
                AmountXrp = amount,
                DestinationTag = destTag,
                Memo = string.IsNullOrWhiteSpace(TransferMemo) ? null : TransferMemo,
                UserNote = string.IsNullOrWhiteSpace(TransferUserNote) ? null : TransferUserNote
            };

            var result = await intentService.CreateTransferIntentAsync(request, "SettingsPage");

            if (!result.Success)
            {
                _notifications.ShowError("Intent failed", result.ErrorMessage ?? "Could not create intent.");
                return;
            }

            // Show the validation preview
            PendingIntent = result.Intent;
            HasIntentValidation = true;
            IntentIsValid = result.Validation!.IsValid;
            IntentValidationMessage = result.Validation.Message;
            IntentCurrentBalance = $"{result.Validation.CurrentBalanceXrp:N6} XRP";
            IntentProjectedBalance = $"{result.Validation.ProjectedBalanceXrp:N6} XRP";
            IntentEstimatedFee = $"{result.Validation.EstimatedFeeXrp:N6} XRP";
            IntentReserveRequirement = $"{result.Validation.ReserveXrp:N6} XRP";
            IntentWarnings = result.Validation.Warnings;
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Validation error", ex.Message);
        }
    }

    /// <summary>
    /// Approves the pending intent — user acknowledges "I understand this plan."
    /// Still does NOT execute anything.
    /// </summary>
    [RelayCommand]
    private async Task ApproveIntentAsync()
    {
        if (PendingIntent is null)
            return;

        try
        {
            var intentService = AppHost.Current.Services.GetRequiredService<XrplIntentService>();
            var success = await intentService.ApproveIntentAsync(PendingIntent.Id);

            if (success)
            {
                _notifications.ShowSuccess(
                    "Intent approved",
                    "Your transfer plan has been saved. Execute it in your wallet, and NextLedger will detect the transaction.");
            }
            else
            {
                _notifications.ShowWarning("Approval failed", "Could not approve the intent.");
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Approval error", ex.Message);
        }
        finally
        {
            IsTransferIntentVisible = false;
            HasIntentValidation = false;
            PendingIntent = null;
        }
    }

    /// <summary>
    /// Generates an exportable execution plan text.
    /// This is what the user takes to their wallet.
    /// </summary>
    [RelayCommand]
    private async Task ExportExecutionPlanAsync()
    {
        if (PendingIntent is null)
            return;

        try
        {
            var intentService = AppHost.Current.Services.GetRequiredService<XrplIntentService>();
            var plan = await intentService.GenerateExecutionPlanAsync(PendingIntent.Id);

            if (plan is not null)
            {
                // Copy to clipboard
                var planText = plan.GetExportText();
                var dataPackage = new Windows.ApplicationModel.DataTransfer.DataPackage();
                dataPackage.SetText(planText);
                Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(dataPackage);

                _notifications.ShowSuccess("Plan exported", "Execution plan copied to clipboard. Paste it into your notes or wallet app.");
            }
            else
            {
                _notifications.ShowWarning("Export failed", "Could not generate execution plan.");
            }
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Export error", ex.Message);
        }
    }

    // ========== Intent History ==========

    [ObservableProperty]
    private IReadOnlyList<IntentHistoryRow> _intentHistory = Array.Empty<IntentHistoryRow>();

    [ObservableProperty]
    private bool _isLoadingIntentHistory;

    [ObservableProperty]
    private XrplIntentStats? _intentStats;

    public bool HasIntentHistory => IntentHistory.Count > 0;

    [RelayCommand]
    private async Task LoadIntentHistoryAsync()
    {
        IsLoadingIntentHistory = true;
        try
        {
            var intentService = AppHost.Current.Services.GetRequiredService<XrplIntentService>();
            var intents = await intentService.GetRecentIntentsAsync(20);
            var stats = await intentService.GetStatsAsync();

            IntentHistory = intents.Select(i => new IntentHistoryRow(
                i.Id,
                i.IntentTypeText,
                i.StatusText,
                i.Summary,
                i.AmountText,
                i.CreatedAt,
                i.ApprovedAt,
                i.MatchedAt,
                i.IsValid,
                GetStatusIcon(i.Status),
                GetStatusColor(i.Status)
            )).ToArray();

            IntentStats = stats;
            OnPropertyChanged(nameof(HasIntentHistory));
        }
        catch (Exception ex)
        {
            _notifications.ShowError("Load failed", ex.Message);
        }
        finally
        {
            IsLoadingIntentHistory = false;
        }
    }

    private static string GetStatusIcon(XrplIntentStatus status) => status switch
    {
        XrplIntentStatus.Draft => "\uE8C3", // Edit
        XrplIntentStatus.Approved => "\uE8FB", // Checkmark
        XrplIntentStatus.Cancelled => "\uE711", // Cancel
        XrplIntentStatus.Matched => "\uE930", // Link
        _ => "\uE946" // Info
    };

    private static string GetStatusColor(XrplIntentStatus status) => status switch
    {
        XrplIntentStatus.Draft => "Gray",
        XrplIntentStatus.Approved => "ForestGreen",
        XrplIntentStatus.Cancelled => "Tomato",
        XrplIntentStatus.Matched => "DodgerBlue",
        _ => "Gray"
    };

    // ========== Row Models ==========

    public sealed record AccountRow(
        Guid Id,
        string Name,
        AccountType Type,
        string BalanceText,
        bool IsOnBudget,
        bool IsActive,
        string TypeDisplayName);

    public sealed record EnvelopeRow(
        Guid Id,
        string Name,
        string? GroupName,
        string Color,
        bool IsActive,
        bool IsHidden);

    public sealed record XrplAccountRow(
        Guid Id,
        string Name,
        string BalanceText,
        string? ExternalAddress,
        string? ExternalNetwork,
        DateTime? LastExternalSyncAt,
        long? ExternalReserveDrops,
        decimal? ReserveXrp,
        decimal? SpendableXrpBalance);

    /// <summary>
    /// Row model for XRPL transaction timeline. Read-only, no editing.
    /// </summary>
    public sealed record XrplTransactionRow(
        string Hash,
        DateTime Timestamp,
        string TransactionType,
        string Category,
        string Summary,
        long AmountDrops,
        decimal AmountXrp,
        decimal FeeXrp,
        string ExplorerUrl,
        bool IsSuccess,
        string CategoryIcon)
    {
        /// <summary>Formatted amount for display.</summary>
        public string AmountText => $"{AmountXrp:N6} XRP";

        /// <summary>Formatted timestamp for display.</summary>
        public string TimestampText => Timestamp.ToString("yyyy-MM-dd HH:mm");
    }

    /// <summary>
    /// Row model for XRPL intent history. Audit trail of user plans.
    /// </summary>
    public sealed record IntentHistoryRow(
        Guid Id,
        string IntentType,
        string Status,
        string Summary,
        string AmountText,
        DateTime CreatedAt,
        DateTime? ApprovedAt,
        DateTime? MatchedAt,
        bool IsValid,
        string StatusIcon,
        string StatusColor)
    {
        /// <summary>Formatted creation time.</summary>
        public string CreatedAtText => CreatedAt.ToString("yyyy-MM-dd HH:mm");

        /// <summary>Formatted approved time.</summary>
        public string ApprovedAtText => ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";

        /// <summary>Formatted matched time.</summary>
        public string MatchedAtText => MatchedAt?.ToString("yyyy-MM-dd HH:mm") ?? "-";
    }
}
