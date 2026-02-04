using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;

namespace NextLedger.Application.Interfaces;

/// <summary>
/// Repository for Account entities with specialized queries.
/// </summary>
public interface IAccountRepository : IRepository<Account>
{
    Task<IReadOnlyList<Account>> GetActiveAccountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetOnBudgetAccountsAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Account>> GetByTypeAsync(AccountType type, CancellationToken ct = default);
    Task<Account?> GetByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Gets all active XRPL external ledger accounts.
    /// </summary>
    Task<IReadOnlyList<Account>> GetXrplAccountsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets an account by its external ledger address (e.g., XRPL r-address).
    /// </summary>
    Task<Account?> GetByExternalAddressAsync(string address, CancellationToken ct = default);
}
