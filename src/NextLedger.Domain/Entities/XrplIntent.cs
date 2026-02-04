using NextLedger.Domain.Enums;

namespace NextLedger.Domain.Entities;

/// <summary>
/// Represents a user's expressed financial intent involving XRP.
/// This is a PLAN, not an execution — NextLedger never signs or submits transactions.
///
/// The intent captures:
/// - What the user wants to accomplish
/// - The parameters needed
/// - User's reasoning (optional note)
/// - Lifecycle status (Draft → Approved → Cancelled/Matched)
/// - Full audit provenance
/// </summary>
public sealed class XrplIntent
{
    /// <summary>
    /// Unique identifier for this intent.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Type of intent (Transfer, Reconcile, BudgetFromXrp, etc.).
    /// </summary>
    public XrplIntentType IntentType { get; private set; }

    /// <summary>
    /// Current status in the intent lifecycle.
    /// </summary>
    public XrplIntentStatus Status { get; private set; }

    // --- Parameters (vary by intent type) ---

    /// <summary>
    /// Source XRPL account ID (NextLedger internal account reference).
    /// </summary>
    public Guid? SourceAccountId { get; private set; }

    /// <summary>
    /// Source XRPL address (r-address).
    /// </summary>
    public string? SourceAddress { get; private set; }

    /// <summary>
    /// Destination XRPL address (r-address) for transfers.
    /// </summary>
    public string? DestinationAddress { get; private set; }

    /// <summary>
    /// Amount in XRP drops (1 XRP = 1,000,000 drops).
    /// </summary>
    public long? AmountDrops { get; private set; }

    /// <summary>
    /// Amount in XRP (convenience property).
    /// </summary>
    public decimal? AmountXrp => AmountDrops.HasValue ? AmountDrops.Value / 1_000_000m : null;

    /// <summary>
    /// Destination tag for exchange/hosted wallet transfers.
    /// </summary>
    public uint? DestinationTag { get; private set; }

    /// <summary>
    /// Optional memo text (not on-chain, for NextLedger records only).
    /// </summary>
    public string? Memo { get; private set; }

    /// <summary>
    /// XRPL network: mainnet, testnet, devnet.
    /// </summary>
    public string Network { get; private set; } = "mainnet";

    // --- User Context ---

    /// <summary>
    /// User's note explaining why they're doing this (optional but encouraged).
    /// </summary>
    public string? UserNote { get; private set; }

    // --- Provenance (Audit Trail) ---

    /// <summary>
    /// When the intent was first created (Draft).
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the user approved the intent (if approved).
    /// </summary>
    public DateTime? ApprovedAt { get; private set; }

    /// <summary>
    /// When the intent was cancelled (if cancelled).
    /// </summary>
    public DateTime? CancelledAt { get; private set; }

    /// <summary>
    /// When the intent was matched to an on-chain transaction (if matched).
    /// </summary>
    public DateTime? MatchedAt { get; private set; }

    /// <summary>
    /// Application version that created this intent.
    /// </summary>
    public string? AppVersion { get; private set; }

    /// <summary>
    /// UI action that triggered this intent (e.g., "TransferButton", "QuickAction").
    /// </summary>
    public string? ActionSource { get; private set; }

    // --- Validation Snapshot ---

    /// <summary>
    /// Balance before the planned action (snapshot at intent creation).
    /// </summary>
    public long? PreviewBalanceDrops { get; private set; }

    /// <summary>
    /// Projected balance after the planned action.
    /// </summary>
    public long? ProjectedBalanceDrops { get; private set; }

    /// <summary>
    /// Estimated fee in drops for this transaction type.
    /// </summary>
    public long? EstimatedFeeDrops { get; private set; }

    /// <summary>
    /// Reserve requirement that must remain in the account.
    /// </summary>
    public long? ReserveRequirementDrops { get; private set; }

    /// <summary>
    /// Validation warnings (e.g., "This will leave you below reserve").
    /// </summary>
    public string? ValidationWarnings { get; private set; }

    /// <summary>
    /// Whether the intent passed all constraint validations.
    /// </summary>
    public bool IsValid { get; private set; }

    // --- Matching (for detecting external execution) ---

    /// <summary>
    /// On-chain transaction hash if this intent was matched to an executed transaction.
    /// </summary>
    public string? MatchedTransactionHash { get; private set; }

    /// <summary>
    /// Ledger index where the matched transaction was validated.
    /// </summary>
    public long? MatchedLedgerIndex { get; private set; }

    // --- Factory Methods ---

    /// <summary>
    /// Creates a new Transfer intent.
    /// </summary>
    public static XrplIntent CreateTransferIntent(
        Guid sourceAccountId,
        string sourceAddress,
        string destinationAddress,
        long amountDrops,
        uint? destinationTag = null,
        string? memo = null,
        string? userNote = null,
        string? appVersion = null,
        string? actionSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAddress);
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationAddress);
        if (amountDrops <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountDrops));

        return new XrplIntent
        {
            Id = Guid.NewGuid(),
            IntentType = XrplIntentType.Transfer,
            Status = XrplIntentStatus.Draft,
            SourceAccountId = sourceAccountId,
            SourceAddress = sourceAddress,
            DestinationAddress = destinationAddress,
            AmountDrops = amountDrops,
            DestinationTag = destinationTag,
            Memo = memo,
            UserNote = userNote,
            CreatedAt = DateTime.UtcNow,
            AppVersion = appVersion,
            ActionSource = actionSource,
            IsValid = false // Will be validated separately
        };
    }

    /// <summary>
    /// Creates a new Reconcile intent (acknowledge and record state).
    /// </summary>
    public static XrplIntent CreateReconcileIntent(
        Guid sourceAccountId,
        string sourceAddress,
        long currentBalanceDrops,
        string? userNote = null,
        string? appVersion = null,
        string? actionSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAddress);

        return new XrplIntent
        {
            Id = Guid.NewGuid(),
            IntentType = XrplIntentType.Reconcile,
            Status = XrplIntentStatus.Draft,
            SourceAccountId = sourceAccountId,
            SourceAddress = sourceAddress,
            PreviewBalanceDrops = currentBalanceDrops,
            UserNote = userNote,
            CreatedAt = DateTime.UtcNow,
            AppVersion = appVersion,
            ActionSource = actionSource,
            IsValid = true // Reconcile is always valid
        };
    }

    /// <summary>
    /// Creates a new BudgetFromXrp intent (plan budget allocations).
    /// </summary>
    public static XrplIntent CreateBudgetFromXrpIntent(
        Guid sourceAccountId,
        string sourceAddress,
        long amountDropsToAllocate,
        string? userNote = null,
        string? appVersion = null,
        string? actionSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceAddress);
        if (amountDropsToAllocate <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amountDropsToAllocate));

        return new XrplIntent
        {
            Id = Guid.NewGuid(),
            IntentType = XrplIntentType.BudgetFromXrp,
            Status = XrplIntentStatus.Draft,
            SourceAccountId = sourceAccountId,
            SourceAddress = sourceAddress,
            AmountDrops = amountDropsToAllocate,
            UserNote = userNote,
            CreatedAt = DateTime.UtcNow,
            AppVersion = appVersion,
            ActionSource = actionSource,
            IsValid = true // Budget planning is always valid
        };
    }

    /// <summary>
    /// Creates a TrackAddress intent (for audit trail of address tracking).
    /// </summary>
    public static XrplIntent CreateTrackAddressIntent(
        string addressToTrack,
        string? userNote = null,
        string? appVersion = null,
        string? actionSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(addressToTrack);

        return new XrplIntent
        {
            Id = Guid.NewGuid(),
            IntentType = XrplIntentType.TrackAddress,
            Status = XrplIntentStatus.Draft,
            DestinationAddress = addressToTrack, // The address being tracked
            UserNote = userNote,
            CreatedAt = DateTime.UtcNow,
            AppVersion = appVersion,
            ActionSource = actionSource,
            IsValid = true
        };
    }

    // --- Lifecycle Methods ---

    /// <summary>
    /// Sets the validation result and preview snapshot.
    /// </summary>
    public void SetValidation(
        bool isValid,
        long? previewBalanceDrops,
        long? projectedBalanceDrops,
        long? estimatedFeeDrops,
        long? reserveRequirementDrops,
        string? warnings)
    {
        IsValid = isValid;
        PreviewBalanceDrops = previewBalanceDrops;
        ProjectedBalanceDrops = projectedBalanceDrops;
        EstimatedFeeDrops = estimatedFeeDrops;
        ReserveRequirementDrops = reserveRequirementDrops;
        ValidationWarnings = warnings;
    }

    /// <summary>
    /// User approves the intent (acknowledges the plan).
    /// </summary>
    public void Approve()
    {
        if (Status != XrplIntentStatus.Draft)
            throw new InvalidOperationException($"Cannot approve intent in {Status} status");

        Status = XrplIntentStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// User cancels the intent.
    /// </summary>
    public void Cancel()
    {
        if (Status == XrplIntentStatus.Matched)
            throw new InvalidOperationException("Cannot cancel an intent that has already been matched to a transaction");

        Status = XrplIntentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Intent is matched to an on-chain transaction (detected external execution).
    /// </summary>
    public void MarkAsMatched(string transactionHash, long ledgerIndex)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionHash);

        if (Status == XrplIntentStatus.Cancelled)
            throw new InvalidOperationException("Cannot match a cancelled intent");

        Status = XrplIntentStatus.Matched;
        MatchedAt = DateTime.UtcNow;
        MatchedTransactionHash = transactionHash;
        MatchedLedgerIndex = ledgerIndex;
    }

    // --- Display Helpers ---

    /// <summary>
    /// Human-readable summary of the intent.
    /// </summary>
    public string GetSummary()
    {
        return IntentType switch
        {
            XrplIntentType.TrackAddress =>
                $"Track XRPL address: {TruncateAddress(DestinationAddress)}",

            XrplIntentType.Transfer =>
                $"Plan to send {AmountXrp:N6} XRP from {TruncateAddress(SourceAddress)} to {TruncateAddress(DestinationAddress)}",

            XrplIntentType.Reconcile =>
                $"Acknowledge balance of {(PreviewBalanceDrops ?? 0) / 1_000_000m:N6} XRP for {TruncateAddress(SourceAddress)}",

            XrplIntentType.BudgetFromXrp =>
                $"Plan budget allocation based on {AmountXrp:N6} XRP from {TruncateAddress(SourceAddress)}",

            _ => $"Intent: {IntentType}"
        };
    }

    private static string TruncateAddress(string? address)
    {
        if (string.IsNullOrEmpty(address) || address.Length <= 12)
            return address ?? "(none)";
        return $"{address[..6]}...{address[^4..]}";
    }

    // --- Dapper/SQLite Hydration ---

    /// <summary>
    /// Private constructor for Dapper hydration.
    /// </summary>
    private XrplIntent() { }

    /// <summary>
    /// Hydrates an intent from database values (for Dapper).
    /// </summary>
    public static XrplIntent Hydrate(
        Guid id,
        int intentType,
        int status,
        Guid? sourceAccountId,
        string? sourceAddress,
        string? destinationAddress,
        long? amountDrops,
        uint? destinationTag,
        string? memo,
        string network,
        string? userNote,
        DateTime createdAt,
        DateTime? approvedAt,
        DateTime? cancelledAt,
        DateTime? matchedAt,
        string? appVersion,
        string? actionSource,
        long? previewBalanceDrops,
        long? projectedBalanceDrops,
        long? estimatedFeeDrops,
        long? reserveRequirementDrops,
        string? validationWarnings,
        bool isValid,
        string? matchedTransactionHash,
        long? matchedLedgerIndex)
    {
        return new XrplIntent
        {
            Id = id,
            IntentType = (XrplIntentType)intentType,
            Status = (XrplIntentStatus)status,
            SourceAccountId = sourceAccountId,
            SourceAddress = sourceAddress,
            DestinationAddress = destinationAddress,
            AmountDrops = amountDrops,
            DestinationTag = destinationTag,
            Memo = memo,
            Network = network,
            UserNote = userNote,
            CreatedAt = createdAt,
            ApprovedAt = approvedAt,
            CancelledAt = cancelledAt,
            MatchedAt = matchedAt,
            AppVersion = appVersion,
            ActionSource = actionSource,
            PreviewBalanceDrops = previewBalanceDrops,
            ProjectedBalanceDrops = projectedBalanceDrops,
            EstimatedFeeDrops = estimatedFeeDrops,
            ReserveRequirementDrops = reserveRequirementDrops,
            ValidationWarnings = validationWarnings,
            IsValid = isValid,
            MatchedTransactionHash = matchedTransactionHash,
            MatchedLedgerIndex = matchedLedgerIndex
        };
    }
}
