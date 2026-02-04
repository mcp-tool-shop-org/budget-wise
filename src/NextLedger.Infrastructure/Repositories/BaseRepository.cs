using NextLedger.Domain.Common;
using NextLedger.Infrastructure.Database;
using Dapper;
using Microsoft.Data.Sqlite;

namespace NextLedger.Infrastructure.Repositories;

/// <summary>
/// Base repository with common CRUD operations.
/// </summary>
public abstract class BaseRepository<T> where T : Entity
{
    protected readonly SqliteConnectionFactory ConnectionFactory;
    protected abstract string TableName { get; }

    protected BaseRepository(SqliteConnectionFactory connectionFactory)
    {
        ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
    }

    protected async Task<SqliteConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        return await ConnectionFactory.GetConnectionAsync(ct);
    }

    protected SqliteConnection GetConnection()
    {
        return ConnectionFactory.GetConnection();
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"SELECT COUNT(1) FROM {TableName} WHERE Id = @Id";
        var count = await connection.ExecuteScalarAsync<int>(sql, new { Id = id.ToString() });
        return count > 0;
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var connection = await GetConnectionAsync(ct);
        var sql = $"DELETE FROM {TableName} WHERE Id = @Id";
        await connection.ExecuteAsync(sql, new { Id = id.ToString() });
    }

    protected static string ToDbString(Guid id) => id.ToString();
    protected static string ToDbString(DateTime dt) => dt.ToString("O");
    protected static string ToDbString(DateOnly date) => date.ToString("yyyy-MM-dd");
    protected static Guid ToGuid(string s) => Guid.Parse(s);
    protected static Guid? ToNullableGuid(string? s) => string.IsNullOrEmpty(s) ? null : Guid.Parse(s);
    protected static DateTime ToDateTime(string s) => DateTime.Parse(s);
    protected static DateTime? ToNullableDateTime(string? s) => string.IsNullOrEmpty(s) ? null : DateTime.Parse(s);
    protected static DateOnly ToDateOnly(string s) => DateOnly.Parse(s);
    protected static DateOnly? ToNullableDateOnly(string? s) => string.IsNullOrEmpty(s) ? null : DateOnly.Parse(s);
}
