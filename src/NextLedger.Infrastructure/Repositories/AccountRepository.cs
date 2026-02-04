using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using Dapper;

namespace NextLedger.Infrastructure.Repositories;

public sealed class AccountRepository : BaseRepository<Account>, IAccountRepository
{
    protected override string TableName => "Accounts";

    public AccountRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Account?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<Account>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(Account entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, Name, Type, Balance, ClearedBalance, UnclearedBalance, Currency,
             IsActive, IsOnBudget, SortOrder, Note, LastReconciledAt,
             ExternalAddress, ExternalNetwork, LastExternalSyncAt, ExternalReserveDrops,
             CreatedAt, UpdatedAt)
            VALUES
            (@Id, @Name, @Type, @Balance, @ClearedBalance, @UnclearedBalance, @Currency,
             @IsActive, @IsOnBudget, @SortOrder, @Note, @LastReconciledAt,
             @ExternalAddress, @ExternalNetwork, @LastExternalSyncAt, @ExternalReserveDrops,
             @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            Type = (int)entity.Type,
            Balance = entity.Balance.Amount,
            ClearedBalance = entity.ClearedBalance.Amount,
            UnclearedBalance = entity.UnclearedBalance.Amount,
            Currency = entity.Balance.Currency,
            IsActive = entity.IsActive ? 1 : 0,
            IsOnBudget = entity.IsOnBudget ? 1 : 0,
            entity.SortOrder,
            entity.Note,
            LastReconciledAt = entity.LastReconciledAt.HasValue ? ToDbString(entity.LastReconciledAt.Value) : null,
            entity.ExternalAddress,
            entity.ExternalNetwork,
            LastExternalSyncAt = entity.LastExternalSyncAt.HasValue ? ToDbString(entity.LastExternalSyncAt.Value) : null,
            entity.ExternalReserveDrops,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(Account entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                Name = @Name,
                Type = @Type,
                Balance = @Balance,
                ClearedBalance = @ClearedBalance,
                UnclearedBalance = @UnclearedBalance,
                Currency = @Currency,
                IsActive = @IsActive,
                IsOnBudget = @IsOnBudget,
                SortOrder = @SortOrder,
                Note = @Note,
                LastReconciledAt = @LastReconciledAt,
                ExternalAddress = @ExternalAddress,
                ExternalNetwork = @ExternalNetwork,
                LastExternalSyncAt = @LastExternalSyncAt,
                ExternalReserveDrops = @ExternalReserveDrops,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Name,
            Type = (int)entity.Type,
            Balance = entity.Balance.Amount,
            ClearedBalance = entity.ClearedBalance.Amount,
            UnclearedBalance = entity.UnclearedBalance.Amount,
            Currency = entity.Balance.Currency,
            IsActive = entity.IsActive ? 1 : 0,
            IsOnBudget = entity.IsOnBudget ? 1 : 0,
            entity.SortOrder,
            entity.Note,
            LastReconciledAt = entity.LastReconciledAt.HasValue ? ToDbString(entity.LastReconciledAt.Value) : null,
            entity.ExternalAddress,
            entity.ExternalNetwork,
            LastExternalSyncAt = entity.LastExternalSyncAt.HasValue ? ToDbString(entity.LastExternalSyncAt.Value) : null,
            entity.ExternalReserveDrops,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public async Task<IReadOnlyList<Account>> GetActiveAccountsAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsActive = 1 ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Account>> GetOnBudgetAccountsAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsActive = 1 AND IsOnBudget = 1 ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Account>> GetByTypeAsync(AccountType type, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Type = @Type ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql, new { Type = (int)type });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Account?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Name = @Name COLLATE NOCASE";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Name = name });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<Account>> GetXrplAccountsAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Type = @Type AND IsActive = 1 ORDER BY SortOrder, Name";
        var rows = await connection.QueryAsync(sql, new { Type = (int)AccountType.ExternalXrpl });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Account?> GetByExternalAddressAsync(string address, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE ExternalAddress = @Address COLLATE NOCASE";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Address = address });
        return row is null ? null : MapToEntity(row);
    }

    private static Account MapToEntity(dynamic row)
    {
        var accountType = (AccountType)(int)row.Type;

        Account account;
        if (accountType == AccountType.ExternalXrpl && row.ExternalAddress is not null)
        {
            // Recreate XRPL account
            account = Account.CreateXrplAccount(
                (string)row.Name,
                (string)row.ExternalAddress,
                (string?)row.ExternalNetwork ?? "mainnet"
            );
        }
        else
        {
            account = Account.Create(
                (string)row.Name,
                accountType,
                new Money((decimal)row.Balance, (string)row.Currency),
                row.IsOnBudget == 1
            );
        }

        // Use reflection to set private properties (for rehydration from DB)
        var type = typeof(Account);
        type.GetProperty("Id")!.SetValue(account, ToGuid((string)row.Id));
        type.GetProperty("Balance")!.SetValue(account, new Money((decimal)row.Balance, (string)row.Currency));
        type.GetProperty("ClearedBalance")!.SetValue(account, new Money((decimal)row.ClearedBalance, (string)row.Currency));
        type.GetProperty("UnclearedBalance")!.SetValue(account, new Money((decimal)row.UnclearedBalance, (string)row.Currency));
        type.GetProperty("IsActive")!.SetValue(account, row.IsActive == 1);
        type.GetProperty("IsOnBudget")!.SetValue(account, row.IsOnBudget == 1);
        type.GetProperty("SortOrder")!.SetValue(account, (int)row.SortOrder);
        type.GetProperty("Note")!.SetValue(account, (string?)row.Note);
        type.GetProperty("LastReconciledAt")!.SetValue(account, ToNullableDateTime((string?)row.LastReconciledAt));
        type.GetProperty("CreatedAt")!.SetValue(account, ToDateTime((string)row.CreatedAt));
        type.GetProperty("UpdatedAt")!.SetValue(account, ToDateTime((string)row.UpdatedAt));

        // XRPL-specific properties
        type.GetProperty("ExternalAddress")!.SetValue(account, (string?)row.ExternalAddress);
        type.GetProperty("ExternalNetwork")!.SetValue(account, (string?)row.ExternalNetwork);
        type.GetProperty("LastExternalSyncAt")!.SetValue(account, ToNullableDateTime((string?)row.LastExternalSyncAt));
        type.GetProperty("ExternalReserveDrops")!.SetValue(account, (long?)row.ExternalReserveDrops);

        return account;
    }
}
