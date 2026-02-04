using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for XRPL intent persistence.
/// Intents are plans, not executions — NextLedger never signs or submits.
/// </summary>
public interface IXrplIntentRepository
{
    /// <summary>
    /// Creates a new intent in Draft status.
    /// </summary>
    Task<XrplIntent> CreateAsync(XrplIntent intent, CancellationToken ct = default);

    /// <summary>
    /// Gets an intent by ID.
    /// </summary>
    Task<XrplIntent?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing intent (status changes, validation results, matching).
    /// </summary>
    Task UpdateAsync(XrplIntent intent, CancellationToken ct = default);

    /// <summary>
    /// Gets all intents for an account, ordered by creation date descending.
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> GetByAccountIdAsync(
        Guid accountId,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets intents by status (e.g., all Draft intents for review).
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> GetByStatusAsync(
        XrplIntentStatus status,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets intents by type (e.g., all Transfer intents).
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> GetByTypeAsync(
        XrplIntentType type,
        int? limit = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets recent intents across all accounts for the intent history view.
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> GetRecentAsync(
        int limit = 50,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all approved (pending execution) intents for an account.
    /// These are intents the user has acknowledged but not yet matched to a transaction.
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> GetPendingIntentsAsync(
        Guid accountId,
        CancellationToken ct = default);

    /// <summary>
    /// Finds intents that might match a given on-chain transaction.
    /// Used for detecting external execution.
    /// </summary>
    Task<IReadOnlyList<XrplIntent>> FindPotentialMatchesAsync(
        string sourceAddress,
        string? destinationAddress,
        long? amountDrops,
        CancellationToken ct = default);

    /// <summary>
    /// Gets statistics about intents (counts by status, type, etc.).
    /// </summary>
    Task<XrplIntentStats> GetStatsAsync(CancellationToken ct = default);
}

/// <summary>
/// Statistics about XRPL intents.
/// </summary>
public sealed record XrplIntentStats
{
    public int TotalCount { get; init; }
    public int DraftCount { get; init; }
    public int ApprovedCount { get; init; }
    public int CancelledCount { get; init; }
    public int MatchedCount { get; init; }
    public int TransferCount { get; init; }
    public int ReconcileCount { get; init; }
    public int BudgetCount { get; init; }
    public int TrackCount { get; init; }
}
