using System.Diagnostics;
using BudgetWise.Application.DTOs;
using BudgetWise.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BudgetWise.App.Services;

/// <summary>
/// Decorator around <see cref="IBudgetEngine"/> that measures per-call latency.
///
/// Design goals:
/// - Zero behavior change: rethrows exceptions; returns inner results unchanged.
/// - Records metrics to an in-memory sink and logs a compact line for each call.
/// </summary>
public sealed class InstrumentedBudgetEngine : IBudgetEngine
{
    private readonly IBudgetEngine _inner;
    private readonly IEngineMetricsSink _sink;
    private readonly ILogger<InstrumentedBudgetEngine> _logger;

    public InstrumentedBudgetEngine(
        IBudgetEngine inner,
        IEngineMetricsSink sink,
        ILogger<InstrumentedBudgetEngine> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _sink = sink ?? throw new ArgumentNullException(nameof(sink));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<BudgetSnapshotDto> GetSnapshotAsync(int year, int month, CancellationToken ct = default)
        => MeasureAsync(
            operation: $"GetSnapshot({year:D4}-{month:D2})",
            action: () => _inner.GetSnapshotAsync(year, month, ct));

    public Task<IReadOnlyList<AccountDto>> GetActiveAccountsAsync(CancellationToken ct = default)
        => MeasureAsync(
            operation: "GetActiveAccounts",
            action: () => _inner.GetActiveAccountsAsync(ct));

    public Task<IReadOnlyList<EnvelopeDto>> GetActiveEnvelopesAsync(int year, int month, CancellationToken ct = default)
        => MeasureAsync(
            operation: $"GetActiveEnvelopes({year:D4}-{month:D2})",
            action: () => _inner.GetActiveEnvelopesAsync(year, month, ct));

    public Task<BudgetSummaryDto> GetBudgetSummaryAsync(int year, int month, CancellationToken ct = default)
        => MeasureAsync(
            operation: $"GetBudgetSummary({year:D4}-{month:D2})",
            action: () => _inner.GetBudgetSummaryAsync(year, month, ct));

    public Task<BudgetOperationResult> SetEnvelopeAllocationAsync(AllocateToEnvelopeRequest request, int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"SetEnvelopeAllocation({year:D4}-{month:D2})",
            action: () => _inner.SetEnvelopeAllocationAsync(request, year, month, ct));

    public Task<BudgetOperationResult> AdjustEnvelopeAllocationAsync(AdjustEnvelopeAllocationRequest request, int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"AdjustEnvelopeAllocation({year:D4}-{month:D2})",
            action: () => _inner.AdjustEnvelopeAllocationAsync(request, year, month, ct));

    public Task<BudgetOperationResult> MoveAsync(MoveMoneyRequest request, int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"Move({year:D4}-{month:D2})",
            action: () => _inner.MoveAsync(request, year, month, ct));

    public Task<BudgetOperationResult> SetGoalAsync(SetGoalRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "SetGoal",
            action: () => _inner.SetGoalAsync(request, ct));

    public Task<BudgetOperationResult> AutoAssignToGoalsAsync(AutoAssignToGoalsRequest request, int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"AutoAssignToGoals({year:D4}-{month:D2})",
            action: () => _inner.AutoAssignToGoalsAsync(request, year, month, ct));

    public Task<BudgetOperationResult> RecalculateAsync(int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"Recalculate({year:D4}-{month:D2})",
            action: () => _inner.RecalculateAsync(year, month, ct));

    public Task<BudgetOperationResult> RolloverAsync(int year, int month, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: $"Rollover({year:D4}-{month:D2})",
            action: () => _inner.RolloverAsync(year, month, ct));

    public Task<BudgetOperationResult<TransactionDto>> CreateOutflowAsync(CreateOutflowRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "CreateOutflow",
            action: () => _inner.CreateOutflowAsync(request, ct));

    public Task<BudgetOperationResult<TransactionDto>> CreateInflowAsync(CreateInflowRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "CreateInflow",
            action: () => _inner.CreateInflowAsync(request, ct));

    public Task<BudgetOperationResult<TransferResultDto>> CreateTransferAsync(CreateTransferRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "CreateTransfer",
            action: () => _inner.CreateTransferAsync(request, ct));

    public Task<BudgetOperationResult<TransactionDto>> UpdateTransactionAsync(UpdateTransactionRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "UpdateTransaction",
            action: () => _inner.UpdateTransactionAsync(request, ct));

    public Task<BudgetOperationResult> DeleteTransactionAsync(Guid transactionId, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "DeleteTransaction",
            action: () => _inner.DeleteTransactionAsync(transactionId, ct));

    public Task<BudgetOperationResult> MarkTransactionClearedAsync(Guid transactionId, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "MarkCleared",
            action: () => _inner.MarkTransactionClearedAsync(transactionId, ct));

    public Task<BudgetOperationResult> MarkTransactionUnclearedAsync(Guid transactionId, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "MarkUncleared",
            action: () => _inner.MarkTransactionUnclearedAsync(transactionId, ct));

    public Task<IReadOnlyList<TransactionDto>> GetAccountTransactionsAsync(Guid accountId, int year, int month, CancellationToken ct = default)
        => MeasureAsync(
            operation: $"GetAccountTransactions({year:D4}-{month:D2})",
            action: () => _inner.GetAccountTransactionsAsync(accountId, year, month, ct));

    public Task<IReadOnlyList<TransactionDto>> GetUnassignedTransactionsAsync(CancellationToken ct = default)
        => MeasureAsync(
            operation: "GetUnassignedTransactions",
            action: () => _inner.GetUnassignedTransactionsAsync(ct));

    public Task<BudgetOperationResult<ReconcileAccountResultDto>> ReconcileAccountAsync(ReconcileAccountRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "ReconcileAccount",
            action: () => _inner.ReconcileAccountAsync(request, ct));

    public Task<BudgetOperationResult<CsvImportPreviewResultDto>> PreviewCsvImportAsync(CsvImportPreviewRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "PreviewCsvImport",
            action: () => _inner.PreviewCsvImportAsync(request, ct));

    public Task<BudgetOperationResult<CsvImportCommitResultDto>> CommitCsvImportAsync(CsvImportCommitRequest request, CancellationToken ct = default)
        => MeasureBudgetOpAsync(
            operation: "CommitCsvImport",
            action: () => _inner.CommitCsvImportAsync(request, ct));

    private async Task<T> MeasureAsync<T>(string operation, Func<Task<T>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();

            Record(operation, stopwatch.Elapsed, success: true, errorCode: null);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Record(operation, stopwatch.Elapsed, success: false, errorCode: "EXCEPTION");
            _logger.LogError(ex, "Engine call failed: {Operation} ({ElapsedMs}ms)", operation, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private async Task<BudgetOperationResult> MeasureBudgetOpAsync(string operation, Func<Task<BudgetOperationResult>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();

            Record(operation, stopwatch.Elapsed, result.Success, result.Errors.FirstOrDefault()?.Code);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Record(operation, stopwatch.Elapsed, success: false, errorCode: "EXCEPTION");
            _logger.LogError(ex, "Engine call threw: {Operation} ({ElapsedMs}ms)", operation, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private async Task<BudgetOperationResult<T>> MeasureBudgetOpAsync<T>(string operation, Func<Task<BudgetOperationResult<T>>> action)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await action();
            stopwatch.Stop();

            Record(operation, stopwatch.Elapsed, result.Success, result.Errors.FirstOrDefault()?.Code);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Record(operation, stopwatch.Elapsed, success: false, errorCode: "EXCEPTION");
            _logger.LogError(ex, "Engine call threw: {Operation} ({ElapsedMs}ms)", operation, stopwatch.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private void Record(string operation, TimeSpan elapsed, bool success, string? errorCode)
    {
        _sink.Record(new EngineCallMetric
        {
            Timestamp = DateTimeOffset.UtcNow,
            Operation = operation,
            Elapsed = elapsed,
            Success = success,
            ErrorCode = errorCode
        });

        _logger.LogInformation(
            "Engine {Operation} -> {Success} in {ElapsedMs}ms{ErrorSuffix}",
            operation,
            success,
            elapsed.TotalMilliseconds,
            string.IsNullOrWhiteSpace(errorCode) ? string.Empty : $" (error={errorCode})");
    }
}
