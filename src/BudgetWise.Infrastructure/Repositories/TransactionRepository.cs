using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using Dapper;

namespace BudgetWise.Infrastructure.Repositories;

public sealed class TransactionRepository : BaseRepository<Transaction>, ITransactionRepository
{
    protected override string TableName => "Transactions";

    public TransactionRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<Transaction?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id AND IsDeleted = 0";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<Transaction>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsDeleted = 0 ORDER BY Date DESC, CreatedAt DESC";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(Transaction entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, AccountId, EnvelopeId, TransferAccountId, LinkedTransactionId, Date,
             Amount, Currency, Payee, Memo, Type, IsCleared, IsReconciled, IsApproved,
             IsDeleted, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @AccountId, @EnvelopeId, @TransferAccountId, @LinkedTransactionId, @Date,
             @Amount, @Currency, @Payee, @Memo, @Type, @IsCleared, @IsReconciled, @IsApproved,
             @IsDeleted, @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            AccountId = ToDbString(entity.AccountId),
            EnvelopeId = entity.EnvelopeId.HasValue ? ToDbString(entity.EnvelopeId.Value) : null,
            TransferAccountId = entity.TransferAccountId.HasValue ? ToDbString(entity.TransferAccountId.Value) : null,
            LinkedTransactionId = entity.LinkedTransactionId.HasValue ? ToDbString(entity.LinkedTransactionId.Value) : null,
            Date = ToDbString(entity.Date),
            Amount = entity.Amount.Amount,
            Currency = entity.Amount.Currency,
            entity.Payee,
            entity.Memo,
            Type = (int)entity.Type,
            IsCleared = entity.IsCleared ? 1 : 0,
            IsReconciled = entity.IsReconciled ? 1 : 0,
            IsApproved = entity.IsApproved ? 1 : 0,
            IsDeleted = entity.IsDeleted ? 1 : 0,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(Transaction entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                AccountId = @AccountId,
                EnvelopeId = @EnvelopeId,
                TransferAccountId = @TransferAccountId,
                LinkedTransactionId = @LinkedTransactionId,
                Date = @Date,
                Amount = @Amount,
                Currency = @Currency,
                Payee = @Payee,
                Memo = @Memo,
                Type = @Type,
                IsCleared = @IsCleared,
                IsReconciled = @IsReconciled,
                IsApproved = @IsApproved,
                IsDeleted = @IsDeleted,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            AccountId = ToDbString(entity.AccountId),
            EnvelopeId = entity.EnvelopeId.HasValue ? ToDbString(entity.EnvelopeId.Value) : null,
            TransferAccountId = entity.TransferAccountId.HasValue ? ToDbString(entity.TransferAccountId.Value) : null,
            LinkedTransactionId = entity.LinkedTransactionId.HasValue ? ToDbString(entity.LinkedTransactionId.Value) : null,
            Date = ToDbString(entity.Date),
            Amount = entity.Amount.Amount,
            Currency = entity.Amount.Currency,
            entity.Payee,
            entity.Memo,
            Type = (int)entity.Type,
            IsCleared = entity.IsCleared ? 1 : 0,
            IsReconciled = entity.IsReconciled ? 1 : 0,
            IsApproved = entity.IsApproved ? 1 : 0,
            IsDeleted = entity.IsDeleted ? 1 : 0,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public override async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"UPDATE {TableName} SET IsDeleted = 1, UpdatedAt = @UpdatedAt WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new
        {
            Id = id.ToString(),
            UpdatedAt = ToDbString(DateTime.UtcNow)
        });
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountAsync(Guid accountId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE AccountId = @AccountId AND IsDeleted = 0 ORDER BY Date DESC, CreatedAt DESC";
        var rows = await connection.QueryAsync(sql, new { AccountId = accountId.ToString() });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetByEnvelopeAsync(Guid envelopeId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE EnvelopeId = @EnvelopeId AND IsDeleted = 0 ORDER BY Date DESC, CreatedAt DESC";
        var rows = await connection.QueryAsync(sql, new { EnvelopeId = envelopeId.ToString() });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetByDateRangeAsync(DateRange range, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE IsDeleted = 0 AND Date >= @Start AND Date <= @End ORDER BY Date DESC, CreatedAt DESC";
        var rows = await connection.QueryAsync(sql, new
        {
            Start = ToDbString(range.Start),
            End = ToDbString(range.End)
        });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetByAccountAndDateRangeAsync(Guid accountId, DateRange range, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            SELECT * FROM {TableName}
            WHERE AccountId = @AccountId AND IsDeleted = 0 AND Date >= @Start AND Date <= @End
            ORDER BY Date DESC, CreatedAt DESC
            """;
        var rows = await connection.QueryAsync(sql, new
        {
            AccountId = accountId.ToString(),
            Start = ToDbString(range.Start),
            End = ToDbString(range.End)
        });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetUnclearedAsync(Guid accountId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE AccountId = @AccountId AND IsDeleted = 0 AND IsCleared = 0 ORDER BY Date DESC";
        var rows = await connection.QueryAsync(sql, new { AccountId = accountId.ToString() });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<IReadOnlyList<Transaction>> GetUnassignedAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            SELECT * FROM {TableName}
            WHERE IsDeleted = 0
              AND EnvelopeId IS NULL
              AND Type != @TransferType
              AND NOT EXISTS (
                  SELECT 1 FROM TransactionSplits s
                  WHERE s.TransactionId = {TableName}.Id
              )
            ORDER BY Date DESC
            """;
        var rows = await connection.QueryAsync(sql, new { TransferType = (int)TransactionType.Transfer });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Money> GetAccountBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT COALESCE(SUM(Amount), 0) FROM {TableName} WHERE AccountId = @AccountId AND IsDeleted = 0";
        var sum = await connection.ExecuteScalarAsync<decimal>(sql, new { AccountId = accountId.ToString() });
        return new Money(sum);
    }

    public async Task<Money> GetAccountClearedBalanceAsync(Guid accountId, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT COALESCE(SUM(Amount), 0) FROM {TableName} WHERE AccountId = @AccountId AND IsDeleted = 0 AND IsCleared = 1";
        var sum = await connection.ExecuteScalarAsync<decimal>(sql, new { AccountId = accountId.ToString() });
        return new Money(sum);
    }

    public async Task<Money> GetEnvelopeSpentAsync(Guid envelopeId, DateRange range, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            SELECT
                (
                    SELECT COALESCE(SUM(ABS(t.Amount)), 0)
                    FROM {TableName} t
                    WHERE t.EnvelopeId = @EnvelopeId
                      AND t.IsDeleted = 0
                      AND t.Date >= @Start AND t.Date <= @End
                      AND t.Amount < 0
                      AND NOT EXISTS (
                          SELECT 1 FROM TransactionSplits s
                          WHERE s.TransactionId = t.Id
                      )
                )
                +
                (
                    SELECT COALESCE(SUM(s.Amount), 0)
                    FROM TransactionSplits s
                    INNER JOIN {TableName} t ON t.Id = s.TransactionId
                    WHERE s.EnvelopeId = @EnvelopeId
                      AND t.IsDeleted = 0
                      AND t.Date >= @Start AND t.Date <= @End
                      AND t.Amount < 0
                )
            ;
            """;

        var sum = await connection.ExecuteScalarAsync<decimal>(sql, new
        {
            EnvelopeId = envelopeId.ToString(),
            Start = ToDbString(range.Start),
            End = ToDbString(range.End)
        });

        return new Money(sum);
    }

    public async Task<(Money Income, Money Spent)> GetTotalsForDateRangeAsync(DateRange range, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);

        // Income = sum of inflows (positive amounts)
        var incomeSql = $"SELECT COALESCE(SUM(Amount), 0) FROM {TableName} WHERE IsDeleted = 0 AND Type = @Type AND Date >= @Start AND Date <= @End";
        var income = await connection.ExecuteScalarAsync<decimal>(incomeSql, new
        {
            Type = (int)TransactionType.Inflow,
            Start = ToDbString(range.Start),
            End = ToDbString(range.End)
        });

        // Spent = sum of outflows as a positive value
        var spentSql = $"SELECT COALESCE(SUM(ABS(Amount)), 0) FROM {TableName} WHERE IsDeleted = 0 AND Type = @Type AND Date >= @Start AND Date <= @End";
        var spent = await connection.ExecuteScalarAsync<decimal>(spentSql, new
        {
            Type = (int)TransactionType.Outflow,
            Start = ToDbString(range.Start),
            End = ToDbString(range.End)
        });

        return (new Money(income), new Money(spent));
    }

    private static Transaction MapToEntity(dynamic row)
    {
        var type = (TransactionType)(int)row.Type;
        var amount = new Money((decimal)row.Amount, (string)row.Currency);

        Transaction transaction;
        if (type == TransactionType.Inflow)
        {
            transaction = Transaction.CreateInflow(
                ToGuid((string)row.AccountId),
                ToDateOnly((string)row.Date),
                amount,
                (string)row.Payee,
                ToNullableGuid((string?)row.EnvelopeId),
                (string?)row.Memo
            );
        }
        else if (type == TransactionType.Outflow)
        {
            transaction = Transaction.CreateOutflow(
                ToGuid((string)row.AccountId),
                ToDateOnly((string)row.Date),
                amount.Abs(),
                (string)row.Payee,
                ToNullableGuid((string?)row.EnvelopeId),
                (string?)row.Memo
            );
        }
        else
        {
            // Transfer - create a partial transaction
            transaction = Transaction.CreateInflow(
                ToGuid((string)row.AccountId),
                ToDateOnly((string)row.Date),
                amount.Abs(),
                "Transfer",
                null,
                (string?)row.Memo
            );
        }

        // Set remaining properties via reflection
        var entityType = typeof(Transaction);
        entityType.GetProperty("Id")!.SetValue(transaction, ToGuid((string)row.Id));
        entityType.GetProperty("Amount")!.SetValue(transaction, amount);
        entityType.GetProperty("Type")!.SetValue(transaction, type);
        entityType.GetProperty("TransferAccountId")!.SetValue(transaction, ToNullableGuid((string?)row.TransferAccountId));
        entityType.GetProperty("LinkedTransactionId")!.SetValue(transaction, ToNullableGuid((string?)row.LinkedTransactionId));
        entityType.GetProperty("IsCleared")!.SetValue(transaction, row.IsCleared == 1);
        entityType.GetProperty("IsReconciled")!.SetValue(transaction, row.IsReconciled == 1);
        entityType.GetProperty("IsApproved")!.SetValue(transaction, row.IsApproved == 1);
        entityType.GetProperty("IsDeleted")!.SetValue(transaction, row.IsDeleted == 1);
        entityType.GetProperty("CreatedAt")!.SetValue(transaction, ToDateTime((string)row.CreatedAt));
        entityType.GetProperty("UpdatedAt")!.SetValue(transaction, ToDateTime((string)row.UpdatedAt));

        return transaction;
    }
}
