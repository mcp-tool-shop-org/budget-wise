using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;

namespace NextLedger.Application.Services;

/// <summary>
/// Service for XRPL intent operations.
/// Handles intent creation, validation, and lifecycle management.
/// IMPORTANT: This service never signs or submits transactions.
/// </summary>
public sealed class XrplIntentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IXrplClient _xrplClient;

    // Standard XRPL transaction fee (12 drops)
    private const long StandardFeeDrops = 12;

    // App version for provenance tracking
    private static readonly string AppVersion = typeof(XrplIntentService).Assembly.GetName().Version?.ToString() ?? "1.0.0";

    public XrplIntentService(IUnitOfWork unitOfWork, IXrplClient xrplClient)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _xrplClient = xrplClient ?? throw new ArgumentNullException(nameof(xrplClient));
    }

    // Repository accessors
    private IXrplIntentRepository IntentRepository => _unitOfWork.XrplIntents;
    private IAccountRepository AccountRepository => _unitOfWork.Accounts;

    /// <summary>
    /// Creates a Transfer intent with validation preview.
    /// This is the "Future Ledger" moment — showing before/after without execution.
    /// </summary>
    public async Task<CreateIntentResult> CreateTransferIntentAsync(
        CreateTransferIntentRequest request,
        string actionSource = "UI",
        CancellationToken ct = default)
    {
        // 1. Load the source account
        var account = await AccountRepository.GetByIdAsync(request.SourceAccountId, ct);
        if (account is null)
            return CreateIntentResult.Error("Source account not found");

        if (account.Type != AccountType.ExternalXrpl)
            return CreateIntentResult.Error("Transfer intents are only supported for XRPL accounts");

        var sourceAddress = account.ExternalAddress;
        if (string.IsNullOrEmpty(sourceAddress))
            return CreateIntentResult.Error("Account does not have an XRPL address");

        // 2. Validate destination address format
        if (!IsValidXrplAddress(request.DestinationAddress))
            return CreateIntentResult.Error("Invalid destination address format. XRPL addresses start with 'r'.");

        if (request.DestinationAddress == sourceAddress)
            return CreateIntentResult.Error("Cannot transfer to the same address");

        // 3. Get current on-chain state for validation
        var accountInfo = await _xrplClient.GetAccountInfoTypedAsync(sourceAddress, ct);
        if (!accountInfo.Success)
            return CreateIntentResult.Error($"Could not fetch account state: {accountInfo.ErrorMessage}");

        var currentBalance = accountInfo.BalanceDrops ?? 0;
        var reserve = await _xrplClient.CalculateReserveAsync(accountInfo.OwnerCount ?? 0, ct);
        var reserveDrops = reserve.TotalReserveDrops;

        // 4. Calculate projected balance
        var amountDrops = request.AmountDrops;
        var feeDrops = StandardFeeDrops;
        var projectedBalance = currentBalance - amountDrops - feeDrops;

        // 5. Validate constraints (protective, not punitive)
        var validation = ValidateTransfer(
            currentBalance,
            projectedBalance,
            reserveDrops,
            amountDrops,
            feeDrops);

        // 6. Create the intent (even if invalid — user can see why)
        var intent = XrplIntent.CreateTransferIntent(
            sourceAccountId: request.SourceAccountId,
            sourceAddress: sourceAddress,
            destinationAddress: request.DestinationAddress,
            amountDrops: amountDrops,
            destinationTag: request.DestinationTag,
            memo: request.Memo,
            userNote: request.UserNote,
            appVersion: AppVersion,
            actionSource: actionSource);

        // 7. Set validation results
        intent.SetValidation(
            isValid: validation.IsValid,
            previewBalanceDrops: currentBalance,
            projectedBalanceDrops: projectedBalance,
            estimatedFeeDrops: feeDrops,
            reserveRequirementDrops: reserveDrops,
            warnings: validation.Warnings.Count > 0 ? string.Join("; ", validation.Warnings) : null);

        // 8. Persist the intent
        await IntentRepository.CreateAsync(intent, ct);

        // 9. Return result with full validation info
        return CreateIntentResult.Ok(
            intent: ToDto(intent),
            validation: validation);
    }

    /// <summary>
    /// Creates a Reconcile intent (acknowledge current state).
    /// </summary>
    public async Task<CreateIntentResult> CreateReconcileIntentAsync(
        CreateReconcileIntentRequest request,
        string actionSource = "UI",
        CancellationToken ct = default)
    {
        var account = await AccountRepository.GetByIdAsync(request.AccountId, ct);
        if (account is null)
            return CreateIntentResult.Error("Account not found");

        if (account.Type != AccountType.ExternalXrpl)
            return CreateIntentResult.Error("Reconcile intents are only supported for XRPL accounts");

        var address = account.ExternalAddress;
        if (string.IsNullOrEmpty(address))
            return CreateIntentResult.Error("Account does not have an XRPL address");

        // Get current balance
        var accountInfo = await _xrplClient.GetAccountInfoTypedAsync(address, ct);
        if (!accountInfo.Success)
            return CreateIntentResult.Error($"Could not fetch account state: {accountInfo.ErrorMessage}");

        var balanceDrops = accountInfo.BalanceDrops ?? 0;
        var ownerCount = accountInfo.OwnerCount ?? 0;

        // Create reconcile intent
        var intent = XrplIntent.CreateReconcileIntent(
            sourceAccountId: request.AccountId,
            sourceAddress: address,
            currentBalanceDrops: balanceDrops,
            userNote: request.UserNote,
            appVersion: AppVersion,
            actionSource: actionSource);

        await IntentRepository.CreateAsync(intent, ct);

        var validation = IntentValidationResult.Success(
            message: "Ready to acknowledge balance",
            currentBalance: balanceDrops,
            projectedBalance: balanceDrops,
            reserve: await GetReserveDropsAsync(ownerCount, ct),
            fee: 0);

        return CreateIntentResult.Ok(ToDto(intent), validation);
    }

    /// <summary>
    /// Approves an intent (user acknowledges the plan).
    /// </summary>
    public async Task<bool> ApproveIntentAsync(Guid intentId, CancellationToken ct = default)
    {
        var intent = await IntentRepository.GetByIdAsync(intentId, ct);
        if (intent is null)
            return false;

        intent.Approve();
        await IntentRepository.UpdateAsync(intent, ct);
        return true;
    }

    /// <summary>
    /// Cancels an intent.
    /// </summary>
    public async Task<bool> CancelIntentAsync(Guid intentId, CancellationToken ct = default)
    {
        var intent = await IntentRepository.GetByIdAsync(intentId, ct);
        if (intent is null)
            return false;

        intent.Cancel();
        await IntentRepository.UpdateAsync(intent, ct);
        return true;
    }

    /// <summary>
    /// Gets an intent by ID.
    /// </summary>
    public async Task<XrplIntentDto?> GetIntentAsync(Guid intentId, CancellationToken ct = default)
    {
        var intent = await IntentRepository.GetByIdAsync(intentId, ct);
        return intent is null ? null : ToDto(intent);
    }

    /// <summary>
    /// Gets recent intents for the history view.
    /// </summary>
    public async Task<IReadOnlyList<XrplIntentDto>> GetRecentIntentsAsync(
        int limit = 50,
        CancellationToken ct = default)
    {
        var intents = await IntentRepository.GetRecentAsync(limit, ct);
        return intents.Select(ToDto).ToList();
    }

    /// <summary>
    /// Gets pending (approved but not matched) intents for an account.
    /// </summary>
    public async Task<IReadOnlyList<XrplIntentDto>> GetPendingIntentsAsync(
        Guid accountId,
        CancellationToken ct = default)
    {
        var intents = await IntentRepository.GetPendingIntentsAsync(accountId, ct);
        return intents.Select(ToDto).ToList();
    }

    /// <summary>
    /// Gets intent statistics.
    /// </summary>
    public async Task<XrplIntentStats> GetStatsAsync(CancellationToken ct = default)
    {
        return await IntentRepository.GetStatsAsync(ct);
    }

    /// <summary>
    /// Generates an exportable execution plan for the user.
    /// This is what they take to their wallet — we don't execute it.
    /// </summary>
    public async Task<XrplExecutionPlan?> GenerateExecutionPlanAsync(
        Guid intentId,
        CancellationToken ct = default)
    {
        var intent = await IntentRepository.GetByIdAsync(intentId, ct);
        if (intent is null || intent.IntentType != XrplIntentType.Transfer)
            return null;

        return new XrplExecutionPlan
        {
            IntentId = intent.Id,
            IntentType = intent.IntentType,
            GeneratedAt = DateTime.UtcNow,
            TransactionType = "Payment",
            SourceAddress = intent.SourceAddress ?? string.Empty,
            DestinationAddress = intent.DestinationAddress,
            AmountDrops = intent.AmountDrops,
            DestinationTag = intent.DestinationTag,
            Memo = intent.Memo,
            Network = intent.Network,
            CurrentBalanceDrops = intent.PreviewBalanceDrops ?? 0,
            ProjectedBalanceDrops = intent.ProjectedBalanceDrops ?? 0,
            ReserveRequirementDrops = intent.ReserveRequirementDrops ?? 0,
            EstimatedFeeDrops = intent.EstimatedFeeDrops ?? StandardFeeDrops,
            Warnings = intent.ValidationWarnings
        };
    }

    /// <summary>
    /// Attempts to match a detected on-chain transaction to a pending intent.
    /// Called when we sync and detect a new transaction.
    /// </summary>
    public async Task<XrplIntentDto?> TryMatchTransactionAsync(
        string sourceAddress,
        string? destinationAddress,
        long amountDrops,
        string transactionHash,
        long ledgerIndex,
        CancellationToken ct = default)
    {
        // Find potential matches
        var matches = await IntentRepository.FindPotentialMatchesAsync(
            sourceAddress,
            destinationAddress,
            amountDrops,
            ct);

        if (matches.Count == 0)
            return null;

        // Take the most recent approved intent that matches
        var bestMatch = matches.FirstOrDefault();
        if (bestMatch is null)
            return null;

        // Mark as matched
        bestMatch.MarkAsMatched(transactionHash, ledgerIndex);
        await IntentRepository.UpdateAsync(bestMatch, ct);

        return ToDto(bestMatch);
    }

    // --- Private Helpers ---

    private IntentValidationResult ValidateTransfer(
        long currentBalance,
        long projectedBalance,
        long reserveDrops,
        long amountDrops,
        long feeDrops)
    {
        var warnings = new List<string>();
        var errors = new List<string>();

        // Check if transfer amount is reasonable
        if (amountDrops < 1)
        {
            errors.Add("Amount must be at least 1 drop");
        }

        // Check if we have enough to cover the transfer + fee
        if (currentBalance < amountDrops + feeDrops)
        {
            errors.Add($"Insufficient balance. You have {currentBalance / 1_000_000m:N6} XRP but need {(amountDrops + feeDrops) / 1_000_000m:N6} XRP");
        }

        // Check reserve requirement (protective warning)
        if (projectedBalance < reserveDrops)
        {
            var shortfall = reserveDrops - projectedBalance;
            warnings.Add($"⚠️ This transfer would leave your balance below the reserve requirement. " +
                        $"You need at least {reserveDrops / 1_000_000m:N6} XRP to keep your account active. " +
                        $"Consider transferring {shortfall / 1_000_000m:N6} XRP less.");
        }
        else if (projectedBalance < reserveDrops * 1.1m) // Within 10% of reserve
        {
            warnings.Add($"💡 This transfer will leave your balance close to the reserve requirement. " +
                        $"Consider keeping a buffer above {reserveDrops / 1_000_000m:N6} XRP.");
        }

        // Check if this might be a large transfer (> 50% of spendable)
        var spendable = currentBalance - reserveDrops;
        if (spendable > 0 && amountDrops > spendable * 0.5m)
        {
            var percentage = (amountDrops * 100.0m) / spendable;
            warnings.Add($"📊 This is a significant transfer ({percentage:N1}% of your spendable balance).");
        }

        var isValid = errors.Count == 0;
        var message = isValid
            ? warnings.Count > 0
                ? "Valid with warnings — please review"
                : "Valid — ready to approve"
            : "Cannot proceed — see errors";

        return isValid
            ? IntentValidationResult.Success(message, currentBalance, projectedBalance, reserveDrops, feeDrops, warnings)
            : IntentValidationResult.Failure(message, errors, currentBalance, projectedBalance, reserveDrops, feeDrops);
    }

    private async Task<long> GetReserveDropsAsync(int ownerCount, CancellationToken ct)
    {
        var reserve = await _xrplClient.CalculateReserveAsync(ownerCount, ct);
        return reserve.TotalReserveDrops;
    }

    private static bool IsValidXrplAddress(string address)
    {
        // Basic XRPL address validation
        // Real addresses are base58check encoded and start with 'r'
        return !string.IsNullOrWhiteSpace(address)
            && address.StartsWith('r')
            && address.Length >= 25
            && address.Length <= 35
            && address.All(c => char.IsLetterOrDigit(c));
    }

    private static XrplIntentDto ToDto(XrplIntent intent)
    {
        return new XrplIntentDto
        {
            Id = intent.Id,
            IntentType = intent.IntentType,
            Status = intent.Status,
            SourceAccountId = intent.SourceAccountId,
            SourceAddress = intent.SourceAddress,
            DestinationAddress = intent.DestinationAddress,
            AmountDrops = intent.AmountDrops,
            DestinationTag = intent.DestinationTag,
            Memo = intent.Memo,
            Network = intent.Network,
            UserNote = intent.UserNote,
            CreatedAt = intent.CreatedAt,
            ApprovedAt = intent.ApprovedAt,
            CancelledAt = intent.CancelledAt,
            MatchedAt = intent.MatchedAt,
            PreviewBalanceDrops = intent.PreviewBalanceDrops,
            ProjectedBalanceDrops = intent.ProjectedBalanceDrops,
            EstimatedFeeDrops = intent.EstimatedFeeDrops,
            ReserveRequirementDrops = intent.ReserveRequirementDrops,
            ValidationWarnings = intent.ValidationWarnings,
            IsValid = intent.IsValid,
            MatchedTransactionHash = intent.MatchedTransactionHash,
            MatchedLedgerIndex = intent.MatchedLedgerIndex,
            Summary = intent.GetSummary()
        };
    }
}
