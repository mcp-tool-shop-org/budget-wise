namespace NextLedger.Domain.Enums;

/// <summary>
/// The type of transaction.
/// </summary>
public enum TransactionType
{
    /// <summary>Money leaving an account (expense).</summary>
    Outflow = 0,

    /// <summary>Money entering an account (income).</summary>
    Inflow = 1,

    /// <summary>Transfer between accounts (creates two linked transactions).</summary>
    Transfer = 2
}
