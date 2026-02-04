using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.Services;

/// <summary>
/// Service for managing accounts and reconciliation.
/// </summary>
public sealed class AccountService
{
    private readonly IUnitOfWork _unitOfWork;

    public AccountService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Create a new account.
    /// </summary>
    public async Task<Account> CreateAccountAsync(
        CreateAccountRequest request,
        CancellationToken ct = default)
    {
        // Check for duplicate name
        var existing = await _unitOfWork.Accounts.GetByNameAsync(request.Name, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Account with name '{request.Name}' already exists.");

        var account = Account.Create(
            request.Name,
            request.Type,
            request.InitialBalance,
            request.IsOnBudget
        );

        await _unitOfWork.Accounts.AddAsync(account, ct);

        // If there's an initial balance, create an opening balance transaction
        if (request.InitialBalance.HasValue && !request.InitialBalance.Value.IsZero)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);

            if (request.InitialBalance.Value.IsPositive)
            {
                var tx = Transaction.CreateInflow(
                    account.Id,
                    today,
                    request.InitialBalance.Value,
                    "Starting Balance",
                    null,
                    "Opening balance"
                );
                tx.MarkCleared();
                await _unitOfWork.Transactions.AddAsync(tx, ct);
            }
            else
            {
                var tx = Transaction.CreateOutflow(
                    account.Id,
                    today,
                    request.InitialBalance.Value.Abs(),
                    "Starting Balance",
                    null,
                    "Opening balance (debt)"
                );
                tx.MarkCleared();
                await _unitOfWork.Transactions.AddAsync(tx, ct);
            }
        }

        return account;
    }

    /// <summary>
    /// Update an account.
    /// </summary>
    public async Task<Account> UpdateAccountAsync(
        UpdateAccountRequest request,
        CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Account {request.Id} not found.");

        if (request.Name is not null)
        {
            // Check for duplicate name
            var existing = await _unitOfWork.Accounts.GetByNameAsync(request.Name, ct);
            if (existing is not null && existing.Id != request.Id)
                throw new InvalidOperationException($"Account with name '{request.Name}' already exists.");

            account.Rename(request.Name);
        }

        if (request.IsOnBudget.HasValue)
            account.SetOnBudget(request.IsOnBudget.Value);

        if (request.Note is not null)
            account.SetNote(request.Note);

        await _unitOfWork.Accounts.UpdateAsync(account, ct);
        return account;
    }

    /// <summary>
    /// Get all active accounts.
    /// </summary>
    public async Task<IReadOnlyList<AccountDto>> GetActiveAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _unitOfWork.Accounts.GetActiveAccountsAsync(ct);
        return accounts.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get account by ID.
    /// </summary>
    public async Task<AccountDto?> GetAccountAsync(Guid id, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(id, ct);
        return account is null ? null : MapToDto(account);
    }

    /// <summary>
    /// Get summary of all accounts with net worth calculation.
    /// </summary>
    public async Task<AccountsSummaryDto> GetAccountsSummaryAsync(CancellationToken ct = default)
    {
        var accounts = await _unitOfWork.Accounts.GetActiveAccountsAsync(ct);

        var onBudget = accounts.Where(a => a.IsOnBudget).ToList();
        var offBudget = accounts.Where(a => !a.IsOnBudget).ToList();

        var totalOnBudget = onBudget.Aggregate(Money.Zero, (sum, a) => sum + a.Balance);
        var totalOffBudget = offBudget.Aggregate(Money.Zero, (sum, a) => sum + a.Balance);

        // Assets = positive balances (checking, savings, cash)
        // Liabilities = credit cards, loans (negative balances or credit accounts with debt)
        var assets = accounts
            .Where(a => !a.IsCreditType && a.Balance.IsPositive)
            .Aggregate(Money.Zero, (sum, a) => sum + a.Balance);

        var liabilities = accounts
            .Where(a => a.IsCreditType || a.Balance.IsNegative)
            .Aggregate(Money.Zero, (sum, a) => sum + a.Balance.Abs());

        return new AccountsSummaryDto
        {
            TotalOnBudget = totalOnBudget,
            TotalOffBudget = totalOffBudget,
            TotalAssets = assets,
            TotalLiabilities = liabilities,
            NetWorth = assets - liabilities,
            Accounts = accounts.Select(MapToDto).ToList()
        };
    }

    /// <summary>
    /// Start reconciliation for an account.
    /// </summary>
    public async Task<ReconciliationDto> StartReconciliationAsync(
        Guid accountId,
        Money statementBalance,
        CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        var unclearedTx = await _unitOfWork.Transactions.GetUnclearedAsync(accountId, ct);

        return new ReconciliationDto
        {
            AccountId = account.Id,
            AccountName = account.Name,
            StatementBalance = statementBalance,
            ClearedBalance = account.ClearedBalance,
            Difference = statementBalance - account.ClearedBalance,
            UnclearedCount = unclearedTx.Count
        };
    }

    /// <summary>
    /// Complete reconciliation for an account.
    /// </summary>
    public async Task CompleteReconciliationAsync(
        Guid accountId,
        Money statementBalance,
        CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        // Mark all cleared transactions as reconciled
        var transactions = await _unitOfWork.Transactions.GetByAccountAsync(accountId, ct);
        foreach (var tx in transactions.Where(t => t.IsCleared && !t.IsReconciled))
        {
            tx.MarkReconciled();
            await _unitOfWork.Transactions.UpdateAsync(tx, ct);
        }

        // Check if balanced
        var difference = statementBalance - account.ClearedBalance;
        if (!difference.IsZero)
        {
            // Create adjustment transaction
            var today = DateOnly.FromDateTime(DateTime.Today);
            if (difference.IsPositive)
            {
                var tx = Transaction.CreateInflow(
                    accountId,
                    today,
                    difference,
                    "Reconciliation Adjustment",
                    null,
                    $"Adjustment to match statement balance of {statementBalance.ToFormattedString()}"
                );
                tx.MarkCleared();
                tx.MarkReconciled();
                await _unitOfWork.Transactions.AddAsync(tx, ct);
            }
            else
            {
                var tx = Transaction.CreateOutflow(
                    accountId,
                    today,
                    difference.Abs(),
                    "Reconciliation Adjustment",
                    null,
                    $"Adjustment to match statement balance of {statementBalance.ToFormattedString()}"
                );
                tx.MarkCleared();
                tx.MarkReconciled();
                await _unitOfWork.Transactions.AddAsync(tx, ct);
            }
        }

        // Update account
        account.MarkReconciled(statementBalance, DateTime.UtcNow);
        await _unitOfWork.Accounts.UpdateAsync(account, ct);
    }

    /// <summary>
    /// Close an account (must have zero balance).
    /// </summary>
    public async Task CloseAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        account.Close();
        await _unitOfWork.Accounts.UpdateAsync(account, ct);
    }

    /// <summary>
    /// Reopen a closed account.
    /// </summary>
    public async Task ReopenAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        account.Reopen();
        await _unitOfWork.Accounts.UpdateAsync(account, ct);
    }

    /// <summary>
    /// Get accounts by type.
    /// </summary>
    public async Task<IReadOnlyList<AccountDto>> GetAccountsByTypeAsync(
        AccountType type,
        CancellationToken ct = default)
    {
        var accounts = await _unitOfWork.Accounts.GetByTypeAsync(type, ct);
        return accounts.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Reorder accounts.
    /// </summary>
    public async Task ReorderAccountsAsync(
        IEnumerable<Guid> accountIdsInOrder,
        CancellationToken ct = default)
    {
        var order = 0;
        foreach (var id in accountIdsInOrder)
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(id, ct);
            if (account is not null)
            {
                account.SetSortOrder(order++);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
            }
        }
    }

    /// <summary>
    /// Get all accounts including closed ones.
    /// </summary>
    public async Task<IReadOnlyList<AccountDto>> GetAllAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _unitOfWork.Accounts.GetAllAsync(ct);
        return accounts.Select(MapToDto).ToList();
    }

    /// <summary>
    /// Get only closed (archived) accounts.
    /// </summary>
    public async Task<IReadOnlyList<AccountDto>> GetClosedAccountsAsync(CancellationToken ct = default)
    {
        var accounts = await _unitOfWork.Accounts.GetAllAsync(ct);
        return accounts.Where(a => !a.IsActive).Select(MapToDto).ToList();
    }

    private static AccountDto MapToDto(Account account) => new()
    {
        Id = account.Id,
        Name = account.Name,
        Type = account.Type,
        Balance = account.Balance,
        ClearedBalance = account.ClearedBalance,
        UnclearedBalance = account.UnclearedBalance,
        IsActive = account.IsActive,
        IsOnBudget = account.IsOnBudget,
        LastReconciledAt = account.LastReconciledAt
    };
}
