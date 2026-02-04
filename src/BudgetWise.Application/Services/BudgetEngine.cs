using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;

namespace BudgetWise.Application.Services;

/// <summary>
/// Default orchestration implementation of <see cref="IBudgetEngine"/>.
/// Keeps operations consistent by performing post-operation recalculation.
/// </summary>
public sealed class BudgetEngine : IBudgetEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly EnvelopeService _envelopeService;
    private readonly BudgetPeriodRecalculationService _recalculationService;
    private readonly TransactionService _transactionService;
    private readonly AccountService _accountService;
    private readonly CsvImportService _csvImportService;

    public BudgetEngine(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _envelopeService = new EnvelopeService(_unitOfWork);
        _recalculationService = new BudgetPeriodRecalculationService(_unitOfWork);
        _transactionService = new TransactionService(_unitOfWork);
        _accountService = new AccountService(_unitOfWork);
        _csvImportService = new CsvImportService(_unitOfWork);
    }

    public Task<IReadOnlyList<AccountDto>> GetActiveAccountsAsync(CancellationToken ct = default)
        => _accountService.GetActiveAccountsAsync(ct);

    public async Task<IReadOnlyList<EnvelopeDto>> GetActiveEnvelopesAsync(int year, int month, CancellationToken ct = default)
    {
        // Use the existing summary contract so envelope balances stay engine-derived.
        var summary = await _envelopeService.GetBudgetSummaryAsync(year, month, ct);
        return summary.Envelopes;
    }

    public async Task<BudgetSnapshotDto> GetSnapshotAsync(int year, int month, CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);

        return new BudgetSnapshotDto
        {
            Year = year,
            Month = month,
            IsClosed = period.IsClosed,
            CarriedOver = period.CarriedOver,
            TotalIncome = period.TotalIncome,
            TotalAllocated = period.TotalAllocated,
            TotalSpent = period.TotalSpent,
            ReadyToAssign = period.ReadyToAssign
        };
    }

    public Task<BudgetSummaryDto> GetBudgetSummaryAsync(int year, int month, CancellationToken ct = default)
        => _envelopeService.GetBudgetSummaryAsync(year, month, ct);

    public async Task<BudgetOperationResult> SetEnvelopeAllocationAsync(
        AllocateToEnvelopeRequest request,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return await ExecuteInTransactionAsync(async () =>
        {
            var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
            var before = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.EnvelopeId, period.Id, ct);

            var after = await _envelopeService.AllocateAsync(request.EnvelopeId, request.Amount, year, month, ct);

            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);

            var envelope = await _unitOfWork.Envelopes.GetByIdAsync(request.EnvelopeId, ct);

            return BudgetOperationResult.Ok(
                snapshot,
                new[]
                {
                    new AllocationChangeDto
                    {
                        EnvelopeId = request.EnvelopeId,
                        EnvelopeName = envelope?.Name,
                        BeforeAllocated = before.Allocated,
                        AfterAllocated = after.Allocated
                    }
                });
        }, ct);
    }

    public async Task<BudgetOperationResult> AdjustEnvelopeAllocationAsync(
        AdjustEnvelopeAllocationRequest request,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return await ExecuteInTransactionAsync(async () =>
        {
            var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
            var before = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.EnvelopeId, period.Id, ct);

            var after = await _envelopeService.AddToAllocationAsync(request.EnvelopeId, request.Delta, year, month, ct);

            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);

            var envelope = await _unitOfWork.Envelopes.GetByIdAsync(request.EnvelopeId, ct);

            return BudgetOperationResult.Ok(
                snapshot,
                new[]
                {
                    new AllocationChangeDto
                    {
                        EnvelopeId = request.EnvelopeId,
                        EnvelopeName = envelope?.Name,
                        BeforeAllocated = before.Allocated,
                        AfterAllocated = after.Allocated
                    }
                });
        }, ct);
    }

    public async Task<BudgetOperationResult> MoveAsync(
        MoveMoneyRequest request,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return await ExecuteInTransactionAsync(async () =>
        {
            var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);

            var beforeFrom = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.FromEnvelopeId, period.Id, ct);
            var beforeTo = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.ToEnvelopeId, period.Id, ct);

            var fromNameTask = _unitOfWork.Envelopes.GetByIdAsync(request.FromEnvelopeId, ct);
            var toNameTask = _unitOfWork.Envelopes.GetByIdAsync(request.ToEnvelopeId, ct);

            await _envelopeService.MoveMoneyAsync(request.FromEnvelopeId, request.ToEnvelopeId, request.Amount, year, month, ct);

            var afterFrom = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.FromEnvelopeId, period.Id, ct);
            var afterTo = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(request.ToEnvelopeId, period.Id, ct);

            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);

            var fromEnvelope = await fromNameTask;
            var toEnvelope = await toNameTask;

            return BudgetOperationResult.Ok(
                snapshot,
                new[]
                {
                    new AllocationChangeDto
                    {
                        EnvelopeId = request.FromEnvelopeId,
                        EnvelopeName = fromEnvelope?.Name,
                        BeforeAllocated = beforeFrom.Allocated,
                        AfterAllocated = afterFrom.Allocated
                    },
                    new AllocationChangeDto
                    {
                        EnvelopeId = request.ToEnvelopeId,
                        EnvelopeName = toEnvelope?.Name,
                        BeforeAllocated = beforeTo.Allocated,
                        AfterAllocated = afterTo.Allocated
                    }
                });
        }, ct);
    }

    public Task<BudgetOperationResult> SetGoalAsync(SetGoalRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            await _envelopeService.SetGoalAsync(request.EnvelopeId, request.Amount, request.TargetDate, ct);
            return BudgetOperationResult.Ok();
        }, ct);
    }

    public Task<BudgetOperationResult> AutoAssignToGoalsAsync(
        AutoAssignToGoalsRequest request,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            // Ensure spent/totals are up to date so Available/Needed are deterministic.
            await _recalculationService.RecalculateAsync(year, month, ct);

            var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);

            var envelopes = await _unitOfWork.Envelopes.GetActiveEnvelopesAsync(ct);
            var goalEnvelopes = envelopes.Where(e => e.HasGoal).ToList();
            if (goalEnvelopes.Count == 0)
            {
                var snapshotNoGoals = await GetSnapshotAsync(year, month, ct);
                return BudgetOperationResult.Ok(snapshotNoGoals);
            }

            var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);
            var allocationByEnvelopeId = allocations.ToDictionary(a => a.EnvelopeId, a => a);

            Money GetAvailableFor(Envelope envelope)
            {
                if (!allocationByEnvelopeId.TryGetValue(envelope.Id, out var allocation))
                    return Money.Zero;
                return allocation.Available;
            }

            Money GetNeededFor(Envelope envelope)
            {
                var goalAmount = envelope.GoalAmount!.Value;
                var needed = goalAmount - GetAvailableFor(envelope);
                return needed.IsPositive ? needed : Money.Zero;
            }

            IOrderedEnumerable<Envelope> ordered = request.Mode switch
            {
                AutoAssignMode.SmallestGoalFirst => goalEnvelopes
                    .OrderBy(e => GetNeededFor(e), MoneyComparer.Instance)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase),

                _ => goalEnvelopes
                    .OrderBy(e => e.GoalDate ?? DateOnly.MaxValue)
                    .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            };

            var changes = new List<AllocationChangeDto>();
            var remaining = period.ReadyToAssign;

            foreach (var envelope in ordered)
            {
                if (!remaining.IsPositive)
                    break;

                var needed = GetNeededFor(envelope);
                if (!needed.IsPositive)
                    continue;

                var periodAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelope.Id, period.Id, ct);
                var beforeAllocated = periodAllocation.Allocated;

                var toAssign = needed <= remaining ? needed : remaining;
                if (!toAssign.IsPositive)
                    continue;

                var afterAllocation = await _envelopeService.AddToAllocationAsync(envelope.Id, toAssign, year, month, ct);

                // Update our in-memory view so subsequent envelopes see updated Available.
                allocationByEnvelopeId[envelope.Id] = afterAllocation;

                changes.Add(new AllocationChangeDto
                {
                    EnvelopeId = envelope.Id,
                    EnvelopeName = envelope.Name,
                    BeforeAllocated = beforeAllocated,
                    AfterAllocated = afterAllocation.Allocated
                });

                remaining = remaining - toAssign;
            }

            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);

            return BudgetOperationResult.Ok(snapshot, changes);
        }, ct);
    }

    public Task<BudgetOperationResult> RecalculateAsync(int year, int month, CancellationToken ct = default)
        => ExecuteInTransactionAsync(async () =>
        {
            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);
            return BudgetOperationResult.Ok(snapshot);
        }, ct);

    public Task<BudgetOperationResult> RolloverAsync(int year, int month, CancellationToken ct = default)
        => ExecuteInTransactionAsync(async () =>
        {
            // Recalculate first; rollover closes the period (closed periods cannot be recalculated).
            await _recalculationService.RecalculateAsync(year, month, ct);

            await _envelopeService.RolloverToNextMonthAsync(year, month, ct);

            var nextMonth = month == 12 ? 1 : month + 1;
            var nextYear = month == 12 ? year + 1 : year;

            await _recalculationService.RecalculateAsync(nextYear, nextMonth, ct);

            var snapshot = await GetSnapshotAsync(nextYear, nextMonth, ct);
            return BudgetOperationResult.Ok(snapshot);
        }, ct);

    public Task<BudgetOperationResult<TransactionDto>> CreateOutflowAsync(CreateOutflowRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            var tx = await _transactionService.CreateOutflowAsync(request, ct);

            await _recalculationService.RecalculateAsync(request.Date.Year, request.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(request.Date.Year, request.Date.Month, ct);

            var dto = await MapTransactionToDtoAsync(tx, ct);
            return BudgetOperationResult<TransactionDto>.Ok(dto, snapshot);
        }, ct);
    }

    public Task<BudgetOperationResult<TransactionDto>> CreateInflowAsync(CreateInflowRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            var tx = await _transactionService.CreateInflowAsync(request, ct);

            await _recalculationService.RecalculateAsync(request.Date.Year, request.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(request.Date.Year, request.Date.Month, ct);

            var dto = await MapTransactionToDtoAsync(tx, ct);
            return BudgetOperationResult<TransactionDto>.Ok(dto, snapshot);
        }, ct);
    }

    public Task<BudgetOperationResult<TransferResultDto>> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            var (from, to) = await _transactionService.CreateTransferAsync(request, ct);

            await _recalculationService.RecalculateAsync(request.Date.Year, request.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(request.Date.Year, request.Date.Month, ct);

            var payload = new TransferResultDto
            {
                From = await MapTransactionToDtoAsync(from, ct),
                To = await MapTransactionToDtoAsync(to, ct)
            };

            return BudgetOperationResult<TransferResultDto>.Ok(payload, snapshot);
        }, ct);
    }

    public Task<BudgetOperationResult<TransactionDto>> UpdateTransactionAsync(UpdateTransactionRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            var tx = await _transactionService.UpdateTransactionAsync(request, ct);

            await _recalculationService.RecalculateAsync(tx.Date.Year, tx.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(tx.Date.Year, tx.Date.Month, ct);

            var dto = await MapTransactionToDtoAsync(tx, ct);
            return BudgetOperationResult<TransactionDto>.Ok(dto, snapshot);
        }, ct);
    }

    public Task<BudgetOperationResult> DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => ExecuteInTransactionAsync(async () =>
        {
            var tx = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
                ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

            var year = tx.Date.Year;
            var month = tx.Date.Month;

            await _transactionService.DeleteTransactionAsync(transactionId, ct);

            await _recalculationService.RecalculateAsync(year, month, ct);
            var snapshot = await GetSnapshotAsync(year, month, ct);

            return BudgetOperationResult.Ok(snapshot);
        }, ct);

    public Task<BudgetOperationResult> MarkTransactionClearedAsync(Guid transactionId, CancellationToken ct = default)
        => ExecuteInTransactionAsync(async () =>
        {
            var tx = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
                ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

            await _transactionService.MarkClearedAsync(transactionId, ct);

            await _recalculationService.RecalculateAsync(tx.Date.Year, tx.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(tx.Date.Year, tx.Date.Month, ct);
            return BudgetOperationResult.Ok(snapshot);
        }, ct);

    public Task<BudgetOperationResult> MarkTransactionUnclearedAsync(Guid transactionId, CancellationToken ct = default)
        => ExecuteInTransactionAsync(async () =>
        {
            var tx = await _unitOfWork.Transactions.GetByIdAsync(transactionId, ct)
                ?? throw new InvalidOperationException($"Transaction {transactionId} not found.");

            await _transactionService.MarkUnclearedAsync(transactionId, ct);

            await _recalculationService.RecalculateAsync(tx.Date.Year, tx.Date.Month, ct);
            var snapshot = await GetSnapshotAsync(tx.Date.Year, tx.Date.Month, ct);
            return BudgetOperationResult.Ok(snapshot);
        }, ct);

    public Task<IReadOnlyList<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, int year, int month, CancellationToken ct = default)
        => _transactionService.GetAccountTransactionsAsync(accountId, DateRange.ForMonth(year, month), ct);

    public Task<IReadOnlyList<TransactionDto>> GetUnassignedTransactionsAsync(CancellationToken ct = default)
        => _transactionService.GetUnassignedTransactionsAsync(ct);

    public Task<BudgetOperationResult<ReconcileAccountResultDto>> ReconcileAccountAsync(ReconcileAccountRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.TransactionIdsToReconcile is null)
            throw new ArgumentNullException(nameof(request.TransactionIdsToReconcile));

        return ExecuteInTransactionAsync(async () =>
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct)
                ?? throw new InvalidOperationException($"Account {request.AccountId} not found.");

            async Task<(Money Total, Money Cleared, Money Difference)> RecomputeAsync()
            {
                var total = await _unitOfWork.Transactions.GetAccountBalanceAsync(request.AccountId, ct);
                var cleared = await _unitOfWork.Transactions.GetAccountClearedBalanceAsync(request.AccountId, ct);
                return (total, cleared, request.StatementEndingBalance - cleared);
            }

            async Task UpdateAccountBalancesAsync(Money total, Money cleared)
            {
                account.UpdateBalance(cleared, total - cleared);
                await _unitOfWork.Accounts.UpdateAsync(account, ct);
            }

            if (request.TransactionIdsToReconcile.Count == 0)
            {
                var (_, clearedBalanceEmpty, diffEmpty) = await RecomputeAsync();
                var payloadEmpty = new ReconcileAccountResultDto
                {
                    AccountId = request.AccountId,
                    StatementDate = request.StatementDate,
                    StatementEndingBalance = request.StatementEndingBalance,
                    ClearedBalance = clearedBalanceEmpty,
                    Difference = diffEmpty,
                    ReconciledTransactionCount = 0,
                    AdjustmentTransaction = null
                };

                var snapshotEmpty = await GetSnapshotAsync(request.StatementDate.Year, request.StatementDate.Month, ct);
                return BudgetOperationResult<ReconcileAccountResultDto>.Ok(payloadEmpty, snapshotEmpty);
            }

            // Load + validate all target transactions.
            var uniqueIds = request.TransactionIdsToReconcile.Distinct().ToArray();
            var transactions = new List<Transaction>(uniqueIds.Length);

            foreach (var id in uniqueIds)
            {
                var tx = await _unitOfWork.Transactions.GetByIdAsync(id, ct)
                    ?? throw new InvalidOperationException($"Transaction {id} not found.");

                if (tx.AccountId != request.AccountId)
                    throw new InvalidOperationException("All reconciled transactions must belong to the specified account.");

                if (tx.IsDeleted)
                    throw new InvalidOperationException("Cannot reconcile a deleted transaction.");

                if (tx.IsReconciled)
                    throw new InvalidOperationException("Cannot reconcile a transaction that is already reconciled.");

                transactions.Add(tx);
            }

            // Ensure selected transactions are cleared before reconciling.
            foreach (var tx in transactions)
            {
                if (!tx.IsCleared)
                {
                    tx.MarkCleared();
                    await _unitOfWork.Transactions.UpdateAsync(tx, ct);
                }
            }

            var (totalAfterClearing, clearedBalance, difference) = await RecomputeAsync();
            await UpdateAccountBalancesAsync(totalAfterClearing, clearedBalance);

            TransactionDto? adjustmentDto = null;
            var reconciledCount = 0;

            if (!difference.IsZero && request.CreateAdjustmentIfNeeded)
            {
                // Create an explicit adjustment transaction to force the cleared balance to match.
                // Positive difference => create inflow; negative difference => create outflow.
                var abs = difference.Abs();

                Transaction adjustment;
                if (difference.IsPositive)
                {
                    adjustment = Transaction.CreateInflow(
                        request.AccountId,
                        request.StatementDate,
                        abs,
                        "Reconciliation Adjustment",
                        envelopeId: null,
                        memo: "Auto-created to match statement ending balance");
                }
                else
                {
                    adjustment = Transaction.CreateOutflow(
                        request.AccountId,
                        request.StatementDate,
                        abs,
                        "Reconciliation Adjustment",
                        envelopeId: null,
                        memo: "Auto-created to match statement ending balance");
                }

                adjustment.MarkCleared();
                adjustment.MarkReconciled();
                await _unitOfWork.Transactions.AddAsync(adjustment, ct);

                var (totalAfterAdjustment, clearedAfterAdjustment, diffAfterAdjustment) = await RecomputeAsync();
                clearedBalance = clearedAfterAdjustment;
                difference = diffAfterAdjustment;
                await UpdateAccountBalancesAsync(totalAfterAdjustment, clearedAfterAdjustment);

                adjustmentDto = await MapTransactionToDtoAsync(adjustment, ct);
            }

            if (!difference.IsZero)
                throw new InvalidOperationException("Reconciliation difference must be zero to reconcile. Clear more transactions or create an adjustment.");

            // Mark selected transactions reconciled.
            foreach (var tx in transactions)
            {
                tx.MarkReconciled();
                await _unitOfWork.Transactions.UpdateAsync(tx, ct);
                reconciledCount++;
            }

            // Persist account balances + reconciliation timestamp.
            var (finalTotal, finalCleared, finalDiff) = await RecomputeAsync();
            await UpdateAccountBalancesAsync(finalTotal, finalCleared);
            account.MarkReconciled(finalCleared, DateTime.UtcNow);
            await _unitOfWork.Accounts.UpdateAsync(account, ct);

            clearedBalance = finalCleared;
            difference = finalDiff;

            // Recalculate budget and update balances.
            await _recalculationService.RecalculateAsync(request.StatementDate.Year, request.StatementDate.Month, ct);
            var snapshot = await GetSnapshotAsync(request.StatementDate.Year, request.StatementDate.Month, ct);

            var payload = new ReconcileAccountResultDto
            {
                AccountId = request.AccountId,
                StatementDate = request.StatementDate,
                StatementEndingBalance = request.StatementEndingBalance,
                ClearedBalance = clearedBalance,
                Difference = difference,
                ReconciledTransactionCount = reconciledCount,
                AdjustmentTransaction = adjustmentDto
            };

            return BudgetOperationResult<ReconcileAccountResultDto>.Ok(payload, snapshot);
        }, ct);
    }

    public Task<BudgetOperationResult<CsvImportPreviewResultDto>> PreviewCsvImportAsync(CsvImportPreviewRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return ExecuteInTransactionAsync(async () =>
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct)
                ?? throw new InvalidOperationException($"Account {request.AccountId} not found.");

            var preview = await _csvImportService.PreviewAsync(request, ct);
            return BudgetOperationResult<CsvImportPreviewResultDto>.Ok(preview);
        }, ct);
    }

    public Task<BudgetOperationResult<CsvImportCommitResultDto>> CommitCsvImportAsync(CsvImportCommitRequest request, CancellationToken ct = default)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        if (request.Rows is null)
            throw new ArgumentNullException(nameof(request.Rows));

        return ExecuteInTransactionAsync(async () =>
        {
            var account = await _unitOfWork.Accounts.GetByIdAsync(request.AccountId, ct)
                ?? throw new InvalidOperationException($"Account {request.AccountId} not found.");

            if (request.Rows.Count == 0)
            {
                var empty = new CsvImportCommitResultDto
                {
                    AccountId = request.AccountId,
                    InsertedCount = 0,
                    SkippedDuplicateCount = 0
                };

                return BudgetOperationResult<CsvImportCommitResultDto>.Ok(empty);
            }

            var minDate = request.Rows.Min(r => r.Date);
            var maxDate = request.Rows.Max(r => r.Date);
            var existing = await _csvImportService.GetExistingFingerprintsAsync(request.AccountId, new DateRange(minDate, maxDate), ct);

            var inserted = 0;
            var skipped = 0;

            // Avoid importing duplicates within the same commit batch.
            var seenInBatch = new HashSet<string>(StringComparer.Ordinal);

            foreach (var row in request.Rows.OrderBy(r => r.RowNumber))
            {
                if (!seenInBatch.Add(row.Fingerprint) || existing.Contains(row.Fingerprint))
                {
                    skipped++;
                    continue;
                }

                if (row.Amount.IsPositive)
                {
                    await _transactionService.CreateInflowAsync(new CreateInflowRequest
                    {
                        AccountId = request.AccountId,
                        Date = row.Date,
                        Amount = row.Amount.Abs(),
                        Payee = row.Payee,
                        Memo = row.Memo
                    }, ct);
                }
                else
                {
                    await _transactionService.CreateOutflowAsync(new CreateOutflowRequest
                    {
                        AccountId = request.AccountId,
                        Date = row.Date,
                        Amount = row.Amount.Abs(),
                        Payee = row.Payee,
                        EnvelopeId = null,
                        Memo = row.Memo
                    }, ct);
                }

                inserted++;
                existing.Add(row.Fingerprint);
            }

            // Recalculate each impacted period deterministically.
            foreach (var (year, month) in request.Rows
                         .Select(r => (r.Date.Year, r.Date.Month))
                         .Distinct()
                         .OrderBy(x => x.Year)
                         .ThenBy(x => x.Month))
            {
                await _recalculationService.RecalculateAsync(year, month, ct);
            }

            var snapshot = await GetSnapshotAsync(maxDate.Year, maxDate.Month, ct);

            var result = new CsvImportCommitResultDto
            {
                AccountId = request.AccountId,
                InsertedCount = inserted,
                SkippedDuplicateCount = skipped
            };

            return BudgetOperationResult<CsvImportCommitResultDto>.Ok(result, snapshot);
        }, ct);
    }

    private async Task<TransactionDto> MapTransactionToDtoAsync(Transaction tx, CancellationToken ct)
    {
        var account = await _unitOfWork.Accounts.GetByIdAsync(tx.AccountId, ct);
        var envelopeName = default(string?);
        var splitLines = await _unitOfWork.TransactionSplits.GetByTransactionIdAsync(tx.Id, ct);
        var splitDtos = new List<TransactionSplitLineDto>();

        if (splitLines.Count > 0)
        {
            var envelopes = await _unitOfWork.Envelopes.GetAllAsync(ct);
            var envelopeDict = envelopes.ToDictionary(e => e.Id, e => e.Name);

            splitDtos = splitLines
                .Select(s => new TransactionSplitLineDto
                {
                    EnvelopeId = s.EnvelopeId,
                    EnvelopeName = envelopeDict.GetValueOrDefault(s.EnvelopeId, "Unknown"),
                    Amount = s.Amount,
                    SortOrder = s.SortOrder
                })
                .OrderBy(s => s.SortOrder)
                .ToList();

            envelopeName = "Split";
        }
        else if (tx.EnvelopeId.HasValue)
        {
            var envelope = await _unitOfWork.Envelopes.GetByIdAsync(tx.EnvelopeId.Value, ct);
            envelopeName = envelope?.Name;
        }

        return new TransactionDto
        {
            Id = tx.Id,
            AccountId = tx.AccountId,
            AccountName = account?.Name ?? "Unknown",
            EnvelopeId = tx.EnvelopeId,
            EnvelopeName = envelopeName,
            Date = tx.Date,
            Amount = tx.Amount,
            Payee = tx.Payee,
            Memo = tx.Memo,
            Type = tx.Type,
            IsCleared = tx.IsCleared,
            IsReconciled = tx.IsReconciled,
            SplitLines = splitDtos
        };
    }

    private async Task<BudgetOperationResult<T>> ExecuteInTransactionAsync<T>(
        Func<Task<BudgetOperationResult<T>>> action,
        CancellationToken ct)
    {
        var transactionStarted = false;

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            transactionStarted = true;

            var result = await action();

            await _unitOfWork.CommitTransactionAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                try { await _unitOfWork.RollbackTransactionAsync(ct); }
                catch { /* best effort */ }
            }

            return BudgetOperationResult<T>.Fail(MapError(ex));
        }
    }

    private async Task<BudgetOperationResult> ExecuteInTransactionAsync(
        Func<Task<BudgetOperationResult>> action,
        CancellationToken ct)
    {
        var transactionStarted = false;

        try
        {
            await _unitOfWork.BeginTransactionAsync(ct);
            transactionStarted = true;

            var result = await action();

            await _unitOfWork.CommitTransactionAsync(ct);
            return result;
        }
        catch (Exception ex)
        {
            if (transactionStarted)
            {
                try { await _unitOfWork.RollbackTransactionAsync(ct); }
                catch { /* best effort */ }
            }

            return BudgetOperationResult.Fail(MapError(ex));
        }
    }

    private static BudgetOperationError MapError(Exception ex)
    {
        return ex switch
        {
            ArgumentNullException ane => BudgetOperationError.Create("VALIDATION", ane.Message, ane.ParamName),
            ArgumentException ae => BudgetOperationError.Create("VALIDATION", ae.Message, ae.ParamName),
            InvalidOperationException ioe => BudgetOperationError.Create("INVALID_OPERATION", ioe.Message),
            NotImplementedException nie => BudgetOperationError.Create("NOT_IMPLEMENTED", nie.Message),
            _ => BudgetOperationError.Create("UNEXPECTED", ex.Message)
        };
    }

    private sealed class MoneyComparer : IComparer<Money>
    {
        public static readonly MoneyComparer Instance = new();

        public int Compare(Money x, Money y) => x.Amount.CompareTo(y.Amount);
    }
}
