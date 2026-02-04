using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using Dapper;

namespace NextLedger.Infrastructure.Repositories;

public sealed class EnvelopeRepository : BaseRepository<Envelope>, IEnvelopeRepository
{
    protected override string TableName => "Envelopes";

    public EnvelopeRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Envelope?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<Envelope>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} ORDER BY GroupName, SortOrder, Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(Envelope entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, Name, GroupName, Color, SortOrder, IsActive, IsHidden,
             GoalAmount, GoalCurrency, GoalDate, Note, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @Name, @GroupName, @Color, @SortOrder, @IsActive, @IsHidden,
             @GoalAmount, @GoalCurrency, @GoalDate, @Note, @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            entity.GroupName,
            entity.Color,
            entity.SortOrder,
            IsActive = entity.IsActive ? 1 : 0,
            IsHidden = entity.IsHidden ? 1 : 0,
            GoalAmount = entity.GoalAmount?.Amount,
            GoalCurrency = entity.GoalAmount?.Currency,
            GoalDate = entity.GoalDate.HasValue ? ToDbString(entity.GoalDate.Value) : null,
            entity.Note,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(Envelope entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                Name = @Name,
                GroupName = @GroupName,
                Color = @Color,
                SortOrder = @SortOrder,
                IsActive = @IsActive,
                IsHidden = @IsHidden,
                GoalAmount = @GoalAmount,
                GoalCurrency = @GoalCurrency,
                GoalDate = @GoalDate,
                Note = @Note,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            entity.GroupName,
            entity.Color,
            entity.SortOrder,
            IsActive = entity.IsActive ? 1 : 0,
            IsHidden = entity.IsHidden ? 1 : 0,
            GoalAmount = entity.GoalAmount?.Amount,
            GoalCurrency = entity.GoalAmount?.Currency,
            GoalDate = entity.GoalDate.HasValue ? ToDbString(entity.GoalDate.Value) : null,
            entity.Note,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public async Task<IReadOnlyList<Envelope>> GetActiveEnvelopesAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsActive = 1 ORDER BY GroupName, SortOrder, Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Envelope>> GetByGroupAsync(string groupName, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE GroupName = @GroupName ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql, new { GroupName = groupName });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT DISTINCT GroupName FROM {TableName} WHERE GroupName IS NOT NULL ORDER BY GroupName";
        var rows = await connection.QueryAsync<string>(sql);
        return rows.ToList();
    }

    public async Task<Envelope?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Name = @Name COLLATE NOCASE";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Name = name });
        return row is null ? null : MapToEntity(row);
    }

    private static Envelope MapToEntity(dynamic row)
    {
        var envelope = Envelope.Create(
            (string)row.Name,
            (string?)row.GroupName,
            (string)row.Color
        );

        var type = typeof(Envelope);
        type.GetProperty("Id")!.SetValue(envelope, ToGuid((string)row.Id));
        type.GetProperty("SortOrder")!.SetValue(envelope, (int)row.SortOrder);
        type.GetProperty("IsActive")!.SetValue(envelope, row.IsActive == 1);
        type.GetProperty("IsHidden")!.SetValue(envelope, row.IsHidden == 1);
        type.GetProperty("Note")!.SetValue(envelope, (string?)row.Note);
        type.GetProperty("CreatedAt")!.SetValue(envelope, ToDateTime((string)row.CreatedAt));
        type.GetProperty("UpdatedAt")!.SetValue(envelope, ToDateTime((string)row.UpdatedAt));

        if (row.GoalAmount is not null)
        {
            var goalAmount = new Money((decimal)row.GoalAmount, (string?)row.GoalCurrency ?? "USD");
            var goalDate = ToNullableDateOnly((string?)row.GoalDate);
            type.GetProperty("GoalAmount")!.SetValue(envelope, goalAmount);
            type.GetProperty("GoalDate")!.SetValue(envelope, goalDate);
        }

        return envelope;
    }
}
