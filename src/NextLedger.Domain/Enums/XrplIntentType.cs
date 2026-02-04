namespace NextLedger.Domain.Enums;

/// <summary>
/// Types of XRPL intents that users can express.
/// These are plans, not executions — NextLedger never signs or submits.
/// </summary>
public enum XrplIntentType
{
    /// <summary>
    /// Intent to track a new XRPL address.
    /// Already implemented in Observation layer, but recorded here for audit trail.
    /// </summary>
    TrackAddress = 1,

    /// <summary>
    /// Intent to transfer XRP to another address.
    /// Creates a plan with before/after preview. User executes externally.
    /// </summary>
    Transfer = 2,

    /// <summary>
    /// Intent to acknowledge and record a reconciliation event.
    /// Snapshots the state and records user acknowledgment.
    /// </summary>
    Reconcile = 3,

    /// <summary>
    /// Intent to plan budget allocations informed by XRP holdings.
    /// Helps users plan spending without moving funds.
    /// </summary>
    BudgetFromXrp = 4
}

/// <summary>
/// Status of an XRPL intent through its lifecycle.
/// </summary>
public enum XrplIntentStatus
{
    /// <summary>
    /// Intent is being drafted (user hasn't confirmed yet).
    /// </summary>
    Draft = 1,

    /// <summary>
    /// User has approved the intent (acknowledged the plan).
    /// For non-executing intents, this is the "I understand" confirmation.
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Intent was cancelled by the user.
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Intent was detected as matched to an on-chain transaction.
    /// This happens when user executes externally and we detect it.
    /// </summary>
    Matched = 4
}
