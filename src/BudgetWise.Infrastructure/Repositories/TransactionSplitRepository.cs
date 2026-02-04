using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using Dapper;

namespace BudgetWise.Infrastructure.Repositories;

public sealed class TransactionSplitRepository : BaseRepository<TransactionSplitLine>, ITransactionSplitRepository
{
    protected override string TableName => "TransactionSplits";

    public TransactionSplitRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<IReadOnlyList<TransactionSplitLine>> GetByTransactionIdAsync(Guid transactionId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE TransactionId = @TransactionId ORDER BY SortOrder ASC";
        var rows = await connection.QueryAsync(sql, new { TransactionId = ToDbString(transactionId) });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, IReadOnlyList<TransactionSplitLine>>> GetByTransactionIdsAsync(
        IReadOnlyList<Guid> transactionIds,
        CancellationToken ct = default)
    {
        if (transactionIds.Count == 0)
            return new Dictionary<Guid, IReadOnlyList<TransactionSplitLine>>();

        var connection = await GetConnectionAsync(ct);
        var ids = transactionIds.Select(ToDbString).ToArray();
        var sql = $"SELECT * FROM {TableName} WHERE TransactionId IN @Ids ORDER BY TransactionId, SortOrder ASC";
        var rows = await connection.QueryAsync(sql, new { Ids = ids });

        return rows
            .Select(MapToEntity)
            .GroupBy(l => l.TransactionId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<TransactionSplitLine>)g.ToList());
    }

    public async Task ReplaceAsync(Guid transactionId, IReadOnlyList<TransactionSplitLine> lines, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);

        // Replace = delete then insert.
        await DeleteForTransactionAsync(transactionId, ct);

        if (lines.Count == 0)
            return;

        var now = DateTime.UtcNow;

        var sql = $"""
            INSERT INTO {TableName}
            (Id, TransactionId, EnvelopeId, Amount, Currency, SortOrder, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @TransactionId, @EnvelopeId, @Amount, @Currency, @SortOrder, @CreatedAt, @UpdatedAt)
            """;

        var parameters = lines.Select(l => new
        {
            Id = ToDbString(l.Id),
            TransactionId = ToDbString(transactionId),
            EnvelopeId = ToDbString(l.EnvelopeId),
            Amount = l.Amount.Amount,
            Currency = l.Amount.Currency,
            l.SortOrder,
            CreatedAt = ToDbString(l.CreatedAt == default ? now : l.CreatedAt),
            UpdatedAt = ToDbString(now)
        });

        await connection.ExecuteAsync(sql, parameters);
    }

    public async Task DeleteForTransactionAsync(Guid transactionId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"DELETE FROM {TableName} WHERE TransactionId = @TransactionId";
        await connection.ExecuteAsync(sql, new { TransactionId = ToDbString(transactionId) });
    }

    private static TransactionSplitLine MapToEntity(dynamic row)
    {
        var entity = TransactionSplitLine.Create(
            ToGuid((string)row.TransactionId),
            ToGuid((string)row.EnvelopeId),
            new Money((decimal)row.Amount, (string)row.Currency),
            (int)row.SortOrder);

        var entityType = typeof(TransactionSplitLine);
        entityType.GetProperty("Id")!.SetValue(entity, ToGuid((string)row.Id));
        entityType.GetProperty("CreatedAt")!.SetValue(entity, ToDateTime((string)row.CreatedAt));
        entityType.GetProperty("UpdatedAt")!.SetValue(entity, ToDateTime((string)row.UpdatedAt));
        return entity;
    }
}
