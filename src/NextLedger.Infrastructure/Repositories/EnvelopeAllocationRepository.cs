using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using Dapper;

namespace NextLedger.Infrastructure.Repositories;

public sealed class EnvelopeAllocationRepository : BaseRepository<EnvelopeAllocation>, IEnvelopeAllocationRepository
{
    protected override string TableName => "EnvelopeAllocations";

    public EnvelopeAllocationRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<EnvelopeAllocation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<EnvelopeAllocation>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName}";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(EnvelopeAllocation entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, EnvelopeId, BudgetPeriodId, Allocated, RolloverFromPrevious, Spent,
             Currency, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @EnvelopeId, @BudgetPeriodId, @Allocated, @RolloverFromPrevious, @Spent,
             @Currency, @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            EnvelopeId = ToDbString(entity.EnvelopeId),
            BudgetPeriodId = ToDbString(entity.BudgetPeriodId),
            Allocated = entity.Allocated.Amount,
            RolloverFromPrevious = entity.RolloverFromPrevious.Amount,
            Spent = entity.Spent.Amount,
            Currency = entity.Allocated.Currency,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(EnvelopeAllocation entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                EnvelopeId = @EnvelopeId,
                BudgetPeriodId = @BudgetPeriodId,
                Allocated = @Allocated,
                RolloverFromPrevious = @RolloverFromPrevious,
                Spent = @Spent,
                Currency = @Currency,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            EnvelopeId = ToDbString(entity.EnvelopeId),
            BudgetPeriodId = ToDbString(entity.BudgetPeriodId),
            Allocated = entity.Allocated.Amount,
            RolloverFromPrevious = entity.RolloverFromPrevious.Amount,
            Spent = entity.Spent.Amount,
            Currency = entity.Allocated.Currency,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public async Task<EnvelopeAllocation?> GetByEnvelopeAndPeriodAsync(Guid envelopeId, Guid budgetPeriodId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE EnvelopeId = @EnvelopeId AND BudgetPeriodId = @BudgetPeriodId";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new
        {
            EnvelopeId = envelopeId.ToString(),
            BudgetPeriodId = budgetPeriodId.ToString()
        });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<EnvelopeAllocation>> GetByPeriodAsync(Guid budgetPeriodId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE BudgetPeriodId = @BudgetPeriodId";
        var rows = await connection.QueryAsync(sql, new { BudgetPeriodId = budgetPeriodId.ToString() });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<EnvelopeAllocation>> GetByEnvelopeAsync(Guid envelopeId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE EnvelopeId = @EnvelopeId";
        var rows = await connection.QueryAsync(sql, new { EnvelopeId = envelopeId.ToString() });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<EnvelopeAllocation> GetOrCreateAsync(Guid envelopeId, Guid budgetPeriodId, CancellationToken ct = default)
    {
        var existing = await GetByEnvelopeAndPeriodAsync(envelopeId, budgetPeriodId, ct);
        if (existing is not null)
            return existing;

        var allocation = EnvelopeAllocation.Create(envelopeId, budgetPeriodId);
        await AddAsync(allocation, ct);
        return allocation;
    }

    public async Task<Money> GetTotalAllocatedForPeriodAsync(Guid budgetPeriodId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT COALESCE(SUM(Allocated), 0) FROM {TableName} WHERE BudgetPeriodId = @BudgetPeriodId";
        var sum = await connection.ExecuteScalarAsync<decimal>(sql, new { BudgetPeriodId = budgetPeriodId.ToString() });
        return new Money(sum);
    }

    private static EnvelopeAllocation MapToEntity(dynamic row)
    {
        var currency = (string)row.Currency;
        var allocation = EnvelopeAllocation.Create(
            ToGuid((string)row.EnvelopeId),
            ToGuid((string)row.BudgetPeriodId),
            new Money((decimal)row.Allocated, currency),
            new Money((decimal)row.RolloverFromPrevious, currency)
        );

        var type = typeof(EnvelopeAllocation);
        type.GetProperty("Id")!.SetValue(allocation, ToGuid((string)row.Id));
        type.GetProperty("Spent")!.SetValue(allocation, new Money((decimal)row.Spent, currency));
        type.GetProperty("CreatedAt")!.SetValue(allocation, ToDateTime((string)row.CreatedAt));
        type.GetProperty("UpdatedAt")!.SetValue(allocation, ToDateTime((string)row.UpdatedAt));

        return allocation;
    }
}
