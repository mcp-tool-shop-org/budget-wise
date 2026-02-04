namespace NextLedger.Domain.Enums;

/// <summary>
/// The type of financial account.
/// </summary>
public enum AccountType
{
    /// <summary>Standard checking/debit account.</summary>
    Checking = 0,

    /// <summary>Savings account.</summary>
    Savings = 1,

    /// <summary>Credit card account (balance typically negative).</summary>
    CreditCard = 2,

    /// <summary>Physical cash on hand.</summary>
    Cash = 3,

    /// <summary>Line of credit or loan.</summary>
    LineOfCredit = 4,

    /// <summary>Investment or brokerage account.</summary>
    Investment = 5,

    /// <summary>
    /// External ledger account (XRPL). Read-only, externally reconciled.
    /// NextLedger observes but never controls this account.
    /// </summary>
    ExternalXrpl = 10,

    /// <summary>Other/miscellaneous account type.</summary>
    Other = 99
}
