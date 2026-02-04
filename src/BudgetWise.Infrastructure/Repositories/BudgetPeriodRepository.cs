using BudgetWise.Application.Interfaces;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using Dapper;

namespace BudgetWise.Infrastructure.Repositories;

public sealed class BudgetPeriodRepository : BaseRepository<BudgetPeriod>, IBudgetPeriodRepository
{
    protected override string TableName => "BudgetPeriods";

    public BudgetPeriodRepository(SqliteConnectionFactory connectionFactory) : base(connectionFactory) { }

    public async Task<BudgetPeriod?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Id = @Id";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Id = id.ToString() });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<IReadOnlyList<BudgetPeriod>> GetAllAsync(CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} ORDER BY Year DESC, Month DESC";
        var rows = await connection.QueryAsync(sql);
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<Guid> AddAsync(BudgetPeriod entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            INSERT INTO {TableName}
            (Id, Year, Month, TotalIncome, TotalAllocated, TotalSpent, CarriedOver,
             Currency, IsClosed, CreatedAt, UpdatedAt)
            VALUES
            (@Id, @Year, @Month, @TotalIncome, @TotalAllocated, @TotalSpent, @CarriedOver,
             @Currency, @IsClosed, @CreatedAt, @UpdatedAt)
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Year,
            entity.Month,
            TotalIncome = entity.TotalIncome.Amount,
            TotalAllocated = entity.TotalAllocated.Amount,
            TotalSpent = entity.TotalSpent.Amount,
            CarriedOver = entity.CarriedOver.Amount,
            Currency = entity.TotalIncome.Currency,
            IsClosed = entity.IsClosed ? 1 : 0,
            CreatedAt = ToDbString(entity.CreatedAt),
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });

        return entity.Id;
    }

    public async Task UpdateAsync(BudgetPeriod entity, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"""
            UPDATE {TableName} SET
                Year = @Year,
                Month = @Month,
                TotalIncome = @TotalIncome,
                TotalAllocated = @TotalAllocated,
                TotalSpent = @TotalSpent,
                CarriedOver = @CarriedOver,
                Currency = @Currency,
                IsClosed = @IsClosed,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = ToDbString(entity.Id),
            entity.Year,
            entity.Month,
            TotalIncome = entity.TotalIncome.Amount,
            TotalAllocated = entity.TotalAllocated.Amount,
            TotalSpent = entity.TotalSpent.Amount,
            CarriedOver = entity.CarriedOver.Amount,
            Currency = entity.TotalIncome.Currency,
            IsClosed = entity.IsClosed ? 1 : 0,
            UpdatedAt = ToDbString(entity.UpdatedAt)
        });
    }

    public async Task<BudgetPeriod?> GetByYearMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Year = @Year AND Month = @Month";
        var row = await connection.QueryFirstOrDefaultAsync(sql, new { Year = year, Month = month });
        return row is null ? null : MapToEntity(row);
    }

    public async Task<BudgetPeriod?> GetCurrentPeriodAsync(CancellationToken ct = default)
    {
        var now = DateTime.Today;
        return await GetByYearMonthAsync(now.Year, now.Month, ct);
    }

    public async Task<BudgetPeriod?> GetPreviousPeriodAsync(int year, int month, CancellationToken ct = default)
    {
        var prevMonth = month == 1 ? 12 : month - 1;
        var prevYear = month == 1 ? year - 1 : year;
        return await GetByYearMonthAsync(prevYear, prevMonth, ct);
    }

    public async Task<IReadOnlyList<BudgetPeriod>> GetByYearAsync(int year, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT * FROM {TableName} WHERE Year = @Year ORDER BY Month";
        var rows = await connection.QueryAsync(sql, new { Year = year });
        return rows.Select(MapToEntity).ToList();
    }

    public async Task<BudgetPeriod> GetOrCreateAsync(int year, int month, CancellationToken ct = default)
    {
        var existing = await GetByYearMonthAsync(year, month, ct);
        if (existing is not null)
            return existing;

        // Check for carryover from previous period
        var prevPeriod = await GetPreviousPeriodAsync(year, month, ct);
        // Carry forward unassigned cash only; envelope balances roll forward via EnvelopeAllocation.RolloverFromPrevious.
        var carryover = prevPeriod?.ReadyToAssign ?? Money.Zero;

        var newPeriod = BudgetPeriod.Create(year, month, carryover);
        await AddAsync(newPeriod, ct);
        return newPeriod;
    }

    private static BudgetPeriod MapToEntity(dynamic row)
    {
        var currency = (string)row.Currency;
        var period = BudgetPeriod.Create(
            (int)row.Year,
            (int)row.Month,
            new Money((decimal)row.CarriedOver, currency)
        );

        var type = typeof(BudgetPeriod);
        type.GetProperty("Id")!.SetValue(period, ToGuid((string)row.Id));
        type.GetProperty("TotalIncome")!.SetValue(period, new Money((decimal)row.TotalIncome, currency));
        type.GetProperty("TotalAllocated")!.SetValue(period, new Money((decimal)row.TotalAllocated, currency));
        type.GetProperty("TotalSpent")!.SetValue(period, new Money((decimal)row.TotalSpent, currency));
        type.GetProperty("IsClosed")!.SetValue(period, row.IsClosed == 1);
        type.GetProperty("CreatedAt")!.SetValue(period, ToDateTime((string)row.CreatedAt));
        type.GetProperty("UpdatedAt")!.SetValue(period, ToDateTime((string)row.UpdatedAt));

        return period;
    }
}
