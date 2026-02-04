using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Infrastructure.Database;
using Dapper;

namespace NextLedger.Infrastructure.Repositories;

public sealed class PayeeRepository : BaseRepository<Payee>, IPayeeRepository
{
    protected override string TableName => "Payees";

    public PayeeRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Payee?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<Payee>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} ORDER BY Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(Payee entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, Name, DefaultEnvelopeId, IsHidden, TransactionCount, LastUsedAt,
             CreatedAt, UpdatedAt)
            VALUES
            (@Id, @Name, @DefaultEnvelopeId, @IsHidden, @TransactionCount, @LastUsedAt,
             @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            DefaultEnvelopeId = entity.DefaultEnvelopeId.HasValue ? ToDbString(entity.DefaultEnvelopeId.Value) : null,
            IsHidden = entity.IsHidden ? 1 : 0,
            entity.TransactionCount,
            LastUsedAt = entity.LastUsedAt.HasValue ? ToDbString(entity.LastUsedAt.Value) : null,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(Payee entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                Name = @Name,
                DefaultEnvelopeId = @DefaultEnvelopeId,
                IsHidden = @IsHidden,
                TransactionCount = @TransactionCount,
                LastUsedAt = @LastUsedAt,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            DefaultEnvelopeId = entity.DefaultEnvelopeId.HasValue ? ToDbString(entity.DefaultEnvelopeId.Value) : null,
            IsHidden = entity.IsHidden ? 1 : 0,
            entity.TransactionCount,
            LastUsedAt = entity.LastUsedAt.HasValue ? ToDbString(entity.LastUsedAt.Value) : null,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public async Task<Payee?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Name = @Name COLLATE NOCASE";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Name = name });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<Payee> GetOrCreateAsync(string name, CancellationToken ct = default)
    {
        var existing = await GetByNameAsync(name, ct);
        if (existing is not null)
            return existing;

        var payee = Payee.Create(name);
        await AddAsync(payee, ct);
        return payee;
    }

    public async Task<IReadOnlyList<Payee>> SearchAsync(string query, int limit = 10, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            SELECT * FROM {TableName}
            WHERE Name LIKE @Query AND IsHidden = 0
            ORDER BY TransactionCount DESC, Name
            LIMIT @Limit
            """;
        var rows = await connection.QueryAsync(sql, new { Query = $"%{query}%", Limit = limit });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Payee>> GetRecentAsync(int limit = 10, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            SELECT * FROM {TableName}
            WHERE IsHidden = 0 AND LastUsedAt IS NOT NULL
            ORDER BY LastUsedAt DESC
            LIMIT @Limit
            """;
        var rows = await connection.QueryAsync(sql, new { Limit = limit });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Payee>> GetVisibleAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsHidden = 0 ORDER BY Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    private static Payee MapToEntity(dynamic row)
    {
        var payee = Payee.Create(
            (string)row.Name,
            ToNullableGuid((string?)row.DefaultEnvelopeId)
        );

        var type = typeof(Payee);
        type.GetProperty("Id")!.SetValue(payee, ToGuid((string)row.Id));
        type.GetProperty("IsHidden")!.SetValue(payee, row.IsHidden == 1);
        type.GetProperty("TransactionCount")!.SetValue(payee, (int)row.TransactionCount);
        type.GetProperty("LastUsedAt")!.SetValue(payee, ToNullableDateTime((string?)row.LastUsedAt));
        type.GetProperty("CreatedAt")!.SetValue(payee, ToDateTime((string)row.CreatedAt));
        type.GetProperty("UpdatedAt")!.SetValue(payee, ToDateTime((string)row.UpdatedAt));

        return payee;
    }
}
