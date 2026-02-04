using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Application.Services;
using NextLedger.App.Services.Notifications;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

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
}
