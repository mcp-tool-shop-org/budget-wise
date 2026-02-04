using BudgetWise.Application.DTOs;

namespace BudgetWise.Application.Interfaces;

/// <summary>
/// Single orchestration entry point for budgeting operations.
/// Phase 2 introduces the contract; implementation lands in a later commit.
/// </summary>
public interface IBudgetEngine
{
    // --- Read models (UI support) ---

    Task<IReadOnlyList<AccountDto>> GetActiveAccountsAsync(CancellationToken ct = default);

    Task<IReadOnlyList<EnvelopeDto>> GetActiveEnvelopesAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Returns a lightweight snapshot (totals/ready-to-assign) for the period.
    /// </summary>
    Task<BudgetSnapshotDto> GetSnapshotAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Returns the full budget summary (includes envelope balances) for the period.
    /// </summary>
    Task<BudgetSummaryDto> GetBudgetSummaryAsync(int year, int month, CancellationToken ct = default);

    /// <summary>
    /// Sets an envelope's allocation to an absolute amount for the period.
    /// </summary>
    Task<BudgetOperationResult> SetEnvelopeAllocationAsync(
        AllocateToEnvelopeRequest request,
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Adjusts an envelope's allocation by a delta (positive or negative).
    /// </summary>
    Task<BudgetOperationResult> AdjustEnvelopeAllocationAsync(
        AdjustEnvelopeAllocationRequest request,
        int year,
        int month,
        CancellationToken ct = default);

    Task<BudgetOperationResult> MoveAsync(
        MoveMoneyRequest request,
        int year,
        int month,
        CancellationToken ct = default);

    /// <summary>
    /// Sets or updates an envelope savings goal.
    /// </summary>
    Task<BudgetOperationResult> SetGoalAsync(SetGoalRequest request, CancellationToken ct = default);

    /// <summary>
    /// Auto-assigns ReadyToAssign toward envelope goals.
    /// </summary>
    Task<BudgetOperationResult> AutoAssignToGoalsAsync(
        AutoAssignToGoalsRequest request,
        int year,
        int month,
        CancellationToken ct = default);

    Task<BudgetOperationResult> RecalculateAsync(int year, int month, CancellationToken ct = default);

    Task<BudgetOperationResult> RolloverAsync(int year, int month, CancellationToken ct = default);

    // --- Transactions (Phase 4) ---

    Task<BudgetOperationResult<TransactionDto>> CreateOutflowAsync(CreateOutflowRequest request, CancellationToken ct = default);

    Task<BudgetOperationResult<TransactionDto>> CreateInflowAsync(CreateInflowRequest request, CancellationToken ct = default);

    Task<BudgetOperationResult<TransferResultDto>> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default);

    Task<BudgetOperationResult<TransactionDto>> UpdateTransactionAsync(UpdateTransactionRequest request, CancellationToken ct = default);

    Task<BudgetOperationResult> DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default);

    Task<BudgetOperationResult> MarkTransactionClearedAsync(Guid transactionId, CancellationToken ct = default);

    Task<BudgetOperationResult> MarkTransactionUnclearedAsync(Guid transactionId, CancellationToken ct = default);

    Task<IReadOnlyList<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, int year, int month, CancellationToken ct = default);

    Task<IReadOnlyList<TransactionDto>> GetUnassignedTransactionsAsync(CancellationToken ct = default);

    // --- Reconciliation (Phase 4) ---

    Task<BudgetOperationResult<ReconcileAccountResultDto>> ReconcileAccountAsync(ReconcileAccountRequest request, CancellationToken ct = default);

    // --- Import (Phase 4) ---

    Task<BudgetOperationResult<CsvImportPreviewResultDto>> PreviewCsvImportAsync(CsvImportPreviewRequest request, CancellationToken ct = default);

    Task<BudgetOperationResult<CsvImportCommitResultDto>> CommitCsvImportAsync(CsvImportCommitRequest request, CancellationToken ct = default);
}
