using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Services;

/// <summary>
/// Service for managing transactions.
/// </summary>
public sealed class TransactionService
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Create an outflow (expense) transaction.
    /// </summary>
    public async Task<Transaction> CreateOutflowAsync(
        CreateOutflowRequest request,
        CancellationToken ct = default)
    {
        // Validate account exists
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct)
            ?? throw new InvalidOperationException($"Account {request.AccountId} not found.");

        var hasSplits = request.SplitLines is not null && request.SplitLines.Count > 0;

        if (hasSplits && request.EnvelopeId.HasValue)
            throw new InvalidOperationException("Outflow cannot specify both EnvelopeId and SplitLines.");

        // Validate envelopes
        if (hasSplits)
        {
            foreach (var split in request.SplitLines!)
            {
                _ = await _unitOfWork.Envelopes.GetByIdAsync(split.EnvelopeId, ct)
                    ?? throw new InvalidOperationException($"Envelope {split.EnvelopeId} not found.");
            }

            var splitSum = request.SplitLines!
                .Aggregate(Money.Zero, (sum, l) => sum + l.Amount.Abs());

            if (splitSum != request.Amount.Abs())
                throw new InvalidOperationException("Split line amounts must sum to the transaction amount.");
        }
        else if (request.EnvelopeId.HasValue)
        {
            _ = await _unitOfWork.Envelopes.GetByIdAsync(request.EnvelopeId.Value, ct)
                ?? throw new InvalidOperationException($"Envelope {request.EnvelopeId} not found.");
        }

        var transaction = Transaction.CreateOutflow(
            request.AccountId,
            request.Date,
            request.Amount,
            request.Payee,
            hasSplits ? null : request.EnvelopeId,
            request.Memo
        );

        await _unitOfWork.Transactions.AddAsync(transaction, ct);

        if (hasSplits)
        {
            var splitEntities = request.SplitLines!
                .Select((l, i) => TransactionSplitLine.Create(transaction.Id, l.EnvelopeId, l.Amount.Abs(), i))
                .ToList();

            await _unitOfWork.TransactionSplits.ReplaceAsync(transaction.Id, splitEntities, ct);
        }

        // Update payee usage
        var payee = await _unitOfWork.Payees.GetOrCreateAsync(request.Payee, ct);
        payee.RecordUsage();
        await _unitOfWork.Payees.UpdateAsync(payee, ct);

        // Recalculate account balance
        await UpdateAccountBalanceAsync(request.AccountId, ct);

        return transaction;
    }

    /// <summary>
    /// Create an inflow (income) transaction.
    /// </summary>
    public async Task<Transaction> CreateInflowAsync(
        CreateInflowRequest request,
        CancellationToken ct = default)
    {
        // Validate account exists
        var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct)
            ?? throw new InvalidOperationException($"Account {request.AccountId} not found.");

        var transaction = Transaction.CreateInflow(
            request.AccountId,
            request.Date,
            request.Amount,
            request.Payee,
            null, // Inflows don't get assigned to envelopes directly
            request.Memo
        );

        await _unitOfWork.Transactions.AddAsync(transaction, ct);

        // Update payee
        var payee = await _unitOfWork.Payees.GetOrCreateAsync(request.Payee, ct);
        payee.RecordUsage();
        await _unitOfWork.Payees.UpdateAsync(payee, ct);

        // Update account balance
        await UpdateAccountBalanceAsync(request.AccountId, ct);

        // Update budget period income
        await UpdatePeriodIncomeAsync(request.Date, ct);

        return transaction;
    }

    /// <summary>
    /// Create a transfer between two accounts.
    /// </summary>
    public async Task<(Transaction from, Transaction to)> CreateTransferAsync(
        CreateTransferRequest request,
        CancellationToken ct = default)
    {
        // Validate both accounts exist
        var fromAccount = await _unitOfWork.Accounts.GetByIdAsync(request.FromAccountId, ct)
            ?? throw new InvalidOperationException($"From account {request.FromAccountId} not found.");

        var toAccount = await _unitOfWork.Accounts.GetByIdAsync(request.ToAccountId, ct)
            ?? throw new InvalidOperationException($"To account {request.ToAccountId} not found.");

        var (fromTx, toTx) = Transaction.CreateTransfer(
            request.FromAccountId,
            request.ToAccountId,
            request.Date,
            request.Amount,
            request.Memo
        );

        await _unitOfWork.Transactions.AddAsync(fromTx, ct);
        await _unitOfWork.Transactions.AddAsync(toTx, ct);

        // Link after insert to satisfy SQLite FK constraints on LinkedTransactionId
        fromTx.LinkTo(toTx.Id);
        toTx.LinkTo(fromTx.Id);
        await _unitOfWork.Transactions.UpdateAsync(fromTx, ct);
        await _unitOfWork.Transactions.UpdateAsync(toTx, ct);

        // Update both account balances
        await UpdateAccountBalanceAsync(request.FromAccountId, ct);
        await UpdateAccountBalanceAsync(request.ToAccountId, ct);

        return (fromTx, toTx);
    }

    /// <summary>
    /// Update an existing transaction.
    /// </summary>
    public async Task<Transaction> UpdateTransactionAsync(
        UpdateTransactionRequest request,
        CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Transaction {request.Id} not found.");

        if (transaction.IsReconciled)
            throw new InvalidOperationException("Cannot modify reconciled transaction.");

        var existingSplits = await _unitOfWork.TransactionSplits.GetByTransactionIdAsync(transaction.Id, ct);
        var hasExistingSplits = existingSplits.Count > 0;

        if (transaction.IsTransfer)
        {
            if (request.SplitLines is not null)
                throw new InvalidOperationException("Transfers cannot have splits.");
        }

        if (request.SplitLines is not null && request.SplitLines.Count > 0 && request.EnvelopeId.HasValue)
            throw new InvalidOperationException("Cannot set EnvelopeId when SplitLines are provided.");

        if (request.Date.HasValue)
            transaction.SetDate(request.Date.Value);

        if (request.Amount.HasValue)
        {
            if (hasExistingSplits && request.SplitLines is null)
                throw new InvalidOperationException("Updating amount on a split transaction requires providing SplitLines.");

            transaction.SetAmount(request.Amount.Value);
        }

        if (request.Payee is not null)
            transaction.SetPayee(request.Payee);

        if (request.EnvelopeId.HasValue || request.EnvelopeId is null)
        {
            if (hasExistingSplits)
                throw new InvalidOperationException("Cannot assign a split transaction to an envelope. Clear splits first.");

            transaction.AssignToEnvelope(request.EnvelopeId);
        }

        if (request.Memo is not null)
            transaction.SetMemo(request.Memo);

        if (request.SplitLines is not null)
        {
            if (transaction.Type != Domain.Enums.TransactionType.Outflow)
                throw new InvalidOperationException("Splits are only supported for outflows.");

            var splits = request.SplitLines
                .Select((l, i) => TransactionSplitLine.Create(transaction.Id, l.EnvelopeId, l.Amount.Abs(), i))
                .ToList();

            var targetAmount = (request.Amount ?? transaction.AbsoluteAmount).Abs();
            var splitSum = splits.Aggregate(Money.Zero, (sum, l) => sum + l.Amount.Abs());

            if (splits.Count > 0 && splitSum != targetAmount)
                throw new InvalidOperationException("Split line amounts must sum to the transaction amount.");

            // Validate envelopes referenced by splits.
            foreach (var split in splits)
            {
                _ = await _unitOfWork.Envelopes.GetByIdAsync(split.EnvelopeId, ct)
                    ?? throw new InvalidOperationException($"Envelope {split.EnvelopeId} not found.");
            }

            // Split parent should not be envelope-assigned.
            transaction.AssignToEnvelope(null);

            await _unitOfWork.TransactionSplits.ReplaceAsync(transaction.Id, splits, ct);
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction, ct);
        await UpdateAccountBalanceAsync(transaction.AccountId, ct);

        return transaction;
    }

    /// <summary>
    /// Delete a transaction.
    /// </summary>
    public async Task DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        // Domain guard (also prevents deleting reconciled transactions)
        transaction.SoftDelete();

        var accountId = transaction.AccountId;

        // If transfer, delete linked transaction too
        if (transaction.IsTransfer && transaction.LinkedTransactionId.HasValue)
        {
            var linkedAccountId = transaction.TransferAccountId!.Value;
            var linked = await _unitOfWork.Transactions.GetByIdAsync(transaction.LinkedTransactionId.Value, ct);
            if (linked is not null)
            {
                linked.SoftDelete();
                await _unitOfWork.Transactions.UpdateAsync(linked, ct);
            }
            await UpdateAccountBalanceAsync(linkedAccountId, ct);
        }

        await _unitOfWork.Transactions.UpdateAsync(transaction, ct);
        await UpdateAccountBalanceAsync(accountId, ct);
    }

    /// <summary>
    /// Assign a transaction to an envelope.
    /// </summary>
    public async Task AssignToEnvelopeAsync(
        Guid transactionId,
        Guid envelopeId,
        CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        var existingSplits = await _unitOfWork.TransactionSplits.GetByTransactionIdAsync(transaction.Id, ct);
        if (existingSplits.Count > 0)
            throw new InvalidOperationException("Cannot assign a split transaction to an envelope.");

        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        transaction.AssignToEnvelope(envelopeId);
        await _unitOfWork.Transactions.UpdateAsync(transaction, ct);

        // Update payee default envelope
        var payee = await _unitOfWork.Payees.GetByNameAsync(transaction.Payee, ct);
        if (payee is not null && !payee.DefaultEnvelopeId.HasValue)
        {
            payee.SetDefaultEnvelope(envelopeId);
            await _unitOfWork.Payees.UpdateAsync(payee, ct);
        }
    }

    /// <summary>
    /// Mark a transaction as cleared.
    /// </summary>
    public async Task MarkClearedAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        transaction.MarkCleared();
        await _unitOfWork.Transactions.UpdateAsync(transaction, ct);
        await UpdateAccountBalanceAsync(transaction.AccountId, ct);
    }

    /// <summary>
    /// Mark a transaction as uncleared.
    /// </summary>
    public async Task MarkUnclearedAsync(Guid transactionId, CancellationToken ct = default)
    {
        var transaction = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
            ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

        transaction.MarkUncleared();
        await _unitOfWork.Transactions.UpdateAsync(transaction, ct);
        await UpdateAccountBalanceAsync(transaction.AccountId, ct);
    }

    /// <summary>
    /// Get transactions for an account.
    /// </summary>
    public async Task<IReadOnlyList<TransactionDto>> GetAccountTransactionsAsync(
        Guid accountId,
        DateRange? dateRange = null,
        CancellationToken ct = default)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct)
            ?? throw new InvalidOperationException($"Account {accountId} not found.");

        var transactions = dateRange.HasValue
            ? await _unitOfWork.Transactions.GetByAccountAndDateRangeAsync(accountId, dateRange.Value, ct)
            : await _unitOfWork.Transactions.GetByAccountAsync(accountId, ct);

        return await MapTransactionsToDtosAsync(transactions, ct);
    }

    /// <summary>
    /// Get unassigned transactions (outflows without an envelope).
    /// </summary>
    public async Task<IReadOnlyList<TransactionDto>> GetUnassignedTransactionsAsync(CancellationToken ct = default)
    {
        var transactions = await _unitOfWork.Transactions.GetUnassignedAsync(ct);
        return await MapTransactionsToDtosAsync(transactions, ct);
    }

    /// <summary>
    /// Search payees for autocomplete.
    /// </summary>
    public async Task<IReadOnlyList<Payee>> SearchPayeesAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default)
    {
        return await _unitOfWork.Payees.SearchAsync(query, limit, ct);
    }

    private async Task UpdateAccountBalanceAsync(Guid accountId, CancellationToken ct)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(accountId, ct);
        if (account is null) return;

        var balance = await _unitOfWork.Transactions.GetAccountBalanceAsync(accountId, ct);
        var clearedBalance = await _unitOfWork.Transactions.GetAccountClearedBalanceAsync(accountId, ct);
        var unclearedBalance = balance - clearedBalance;

        account.UpdateBalance(clearedBalance, unclearedBalance);
        await _unitOfWork.Accounts.UpdateAsync(account, ct);
    }

    private async Task UpdatePeriodIncomeAsync(DateOnly date, CancellationToken ct)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(date.Year, date.Month, ct);
        var dateRange = DateRange.ForMonth(date.Year, date.Month);

        // Sum all inflows for the period
        var transactions = await _unitOfWork.Transactions.GetByDateRangeAsync(dateRange, ct);
        var totalIncome = transactions
            .Where(t => t.IsInflow && !t.IsTransfer)
            .Aggregate(Money.Zero, (sum, t) => sum + t.Amount);

        period.UpdateIncome(totalIncome);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period, ct);
    }

    private async Task<IReadOnlyList<TransactionDto>> MapTransactionsToDtosAsync(
        IEnumerable<Transaction> transactions,
        CancellationToken ct)
    {
        // Get accounts and envelopes for mapping
        var accounts = await _unitOfWork.Accounts.GetAllAsync(ct);
        var envelopes = await _unitOfWork.Envelopes.GetAllAsync(ct);

        var accountDict = accounts.ToDictionary(a => a.Id, a => a.Name);
        var envelopeDict = envelopes.ToDictionary(e => e.Id, e => e.Name);

        var list = transactions.ToList();
        var splitDict = await _unitOfWork.TransactionSplits.GetByTransactionIdsAsync(list.Select(t => t.Id).ToList(), ct);

        return list.Select(t =>
        {
            var splits = splitDict.TryGetValue(t.Id, out var lines) ? lines : Array.Empty<TransactionSplitLine>();
            var splitDtos = splits
                .Select(s => new TransactionSplitLineDto
                {
                    EnvelopeId = s.EnvelopeId,
                    EnvelopeName = envelopeDict.GetValueOrDefault(s.EnvelopeId, "Unknown"),
                    Amount = s.Amount,
                    SortOrder = s.SortOrder
                })
                .OrderBy(s => s.SortOrder)
                .ToList();

            var envelopeName = t.EnvelopeId.HasValue ? envelopeDict.GetValueOrDefault(t.EnvelopeId.Value) : null;
            if (splitDtos.Count > 0)
                envelopeName = "Split";

            return new TransactionDto
            {
                Id = t.Id,
                AccountId = t.AccountId,
                AccountName = accountDict.GetValueOrDefault(t.AccountId, "Unknown"),
                EnvelopeId = t.EnvelopeId,
                EnvelopeName = envelopeName,
                Date = t.Date,
                Amount = t.Amount,
                Payee = t.Payee,
                Memo = t.Memo,
                Type = t.Type,
                IsCleared = t.IsCleared,
                IsReconciled = t.IsReconciled,
                SplitLines = splitDtos
            };
        }).ToList();
    }
}
