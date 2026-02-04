using System.Data;
using Dapper;
using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;

namespace NextLedger.Infrastructure.Persistence;

/// <summary>
/// SQLite repository for XRPL intents.
/// Intents are plans, not executions — NextLedger never signs or submits.
/// </summary>
public sealed class XrplIntentRepository : IXrplIntentRepository
{
    private readonly IDbConnection _connection;

    public XrplIntentRepository(IDbConnection connection)
    {
        _connection = connection;
    }

    public async Task<XrplIntent> CreateAsync(XrplIntent intent, CancellationToken ct = default)
    {
        const string sql = """
            INSERT INTO XrplIntents (
                Id, IntentType, Status,
                SourceAccountId, SourceAddress, DestinationAddress,
                AmountDrops, DestinationTag, Memo, Network,
                UserNote,
                CreatedAt, ApprovedAt, CancelledAt, MatchedAt,
                AppVersion, ActionSource,
                PreviewBalanceDrops, ProjectedBalanceDrops, EstimatedFeeDrops,
                ReserveRequirementDrops, ValidationWarnings, IsValid,
                MatchedTransactionHash, MatchedLedgerIndex
            ) VALUES (
                @Id, @IntentType, @Status,
                @SourceAccountId, @SourceAddress, @DestinationAddress,
                @AmountDrops, @DestinationTag, @Memo, @Network,
                @UserNote,
                @CreatedAt, @ApprovedAt, @CancelledAt, @MatchedAt,
                @AppVersion, @ActionSource,
                @PreviewBalanceDrops, @ProjectedBalanceDrops, @EstimatedFeeDrops,
                @ReserveRequirementDrops, @ValidationWarnings, @IsValid,
                @MatchedTransactionHash, @MatchedLedgerIndex
            )
            """;

        var parameters = new
        {
            intent.Id,
            IntentType = (int)intent.IntentType,
            Status = (int)intent.Status,
            intent.SourceAccountId,
            intent.SourceAddress,
            intent.DestinationAddress,
            intent.AmountDrops,
            intent.DestinationTag,
            intent.Memo,
            intent.Network,
            intent.UserNote,
            CreatedAt = intent.CreatedAt.ToString("O"),
            ApprovedAt = intent.ApprovedAt?.ToString("O"),
            CancelledAt = intent.CancelledAt?.ToString("O"),
            MatchedAt = intent.MatchedAt?.ToString("O"),
            intent.AppVersion,
            intent.ActionSource,
            intent.PreviewBalanceDrops,
            intent.ProjectedBalanceDrops,
            intent.EstimatedFeeDrops,
            intent.ReserveRequirementDrops,
            intent.ValidationWarnings,
            IsValid = intent.IsValid ? 1 : 0,
            intent.MatchedTransactionHash,
            intent.MatchedLedgerIndex
        };

        await _connection.ExecuteAsync(sql, parameters);
        return intent;
    }

    public async Task<XrplIntent?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        const string sql = "SELECT * FROM XrplIntents WHERE Id = @Id";
        var row = await _connection.QuerySingleOrDefaultAsync<dynamic>(sql, new { Id = id.ToString() });
        return row is null ? null : HydrateIntent(row);
    }

    public async Task UpdateAsync(XrplIntent intent, CancellationToken ct = default)
    {
        const string sql = """
            UPDATE XrplIntents SET
                Status = @Status,
                ApprovedAt = @ApprovedAt,
                CancelledAt = @CancelledAt,
                MatchedAt = @MatchedAt,
                PreviewBalanceDrops = @PreviewBalanceDrops,
                ProjectedBalanceDrops = @ProjectedBalanceDrops,
                EstimatedFeeDrops = @EstimatedFeeDrops,
                ReserveRequirementDrops = @ReserveRequirementDrops,
                ValidationWarnings = @ValidationWarnings,
                IsValid = @IsValid,
                MatchedTransactionHash = @MatchedTransactionHash,
                MatchedLedgerIndex = @MatchedLedgerIndex
            WHERE Id = @Id
            """;

        var parameters = new
        {
            Id = intent.Id.ToString(),
            Status = (int)intent.Status,
            ApprovedAt = intent.ApprovedAt?.ToString("O"),
            CancelledAt = intent.CancelledAt?.ToString("O"),
            MatchedAt = intent.MatchedAt?.ToString("O"),
            intent.PreviewBalanceDrops,
            intent.ProjectedBalanceDrops,
            intent.EstimatedFeeDrops,
            intent.ReserveRequirementDrops,
            intent.ValidationWarnings,
            IsValid = intent.IsValid ? 1 : 0,
            intent.MatchedTransactionHash,
            intent.MatchedLedgerIndex
        };

        await _connection.ExecuteAsync(sql, parameters);
    }

    public async Task<IReadOnlyList<XrplIntent>> GetByAccountIdAsync(
        Guid accountId,
        int? limit = null,
        CancellationToken ct = default)
    {
        var sql = "SELECT * FROM XrplIntents WHERE SourceAccountId = @AccountId ORDER BY CreatedAt DESC";
        if (limit.HasValue)
            sql += $" LIMIT {limit.Value}";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { AccountId = accountId.ToString() });
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<IReadOnlyList<XrplIntent>> GetByStatusAsync(
        XrplIntentStatus status,
        int? limit = null,
        CancellationToken ct = default)
    {
        var sql = "SELECT * FROM XrplIntents WHERE Status = @Status ORDER BY CreatedAt DESC";
        if (limit.HasValue)
            sql += $" LIMIT {limit.Value}";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { Status = (int)status });
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<IReadOnlyList<XrplIntent>> GetByTypeAsync(
        XrplIntentType type,
        int? limit = null,
        CancellationToken ct = default)
    {
        var sql = "SELECT * FROM XrplIntents WHERE IntentType = @IntentType ORDER BY CreatedAt DESC";
        if (limit.HasValue)
            sql += $" LIMIT {limit.Value}";

        var rows = await _connection.QueryAsync<dynamic>(sql, new { IntentType = (int)type });
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<IReadOnlyList<XrplIntent>> GetRecentAsync(int limit = 50, CancellationToken ct = default)
    {
        var sql = $"SELECT * FROM XrplIntents ORDER BY CreatedAt DESC LIMIT {limit}";
        var rows = await _connection.QueryAsync<dynamic>(sql);
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<IReadOnlyList<XrplIntent>> GetPendingIntentsAsync(
        Guid accountId,
        CancellationToken ct = default)
    {
        const string sql = """
            SELECT * FROM XrplIntents
            WHERE SourceAccountId = @AccountId
              AND Status = @ApprovedStatus
            ORDER BY CreatedAt DESC
            """;

        var rows = await _connection.QueryAsync<dynamic>(sql, new
        {
            AccountId = accountId.ToString(),
            ApprovedStatus = (int)XrplIntentStatus.Approved
        });
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<IReadOnlyList<XrplIntent>> FindPotentialMatchesAsync(
        string sourceAddress,
        string? destinationAddress,
        long? amountDrops,
        CancellationToken ct = default)
    {
        // Find approved Transfer intents that could match this transaction
        var sql = """
            SELECT * FROM XrplIntents
            WHERE IntentType = @TransferType
              AND Status = @ApprovedStatus
              AND SourceAddress = @SourceAddress
            """;

        var parameters = new DynamicParameters();
        parameters.Add("TransferType", (int)XrplIntentType.Transfer);
        parameters.Add("ApprovedStatus", (int)XrplIntentStatus.Approved);
        parameters.Add("SourceAddress", sourceAddress);

        if (!string.IsNullOrEmpty(destinationAddress))
        {
            sql += " AND DestinationAddress = @DestinationAddress";
            parameters.Add("DestinationAddress", destinationAddress);
        }

        if (amountDrops.HasValue)
        {
            // Allow some tolerance for fee variations (within 1000 drops = 0.001 XRP)
            sql += " AND AmountDrops BETWEEN @MinAmount AND @MaxAmount";
            parameters.Add("MinAmount", amountDrops.Value - 1000);
            parameters.Add("MaxAmount", amountDrops.Value + 1000);
        }

        sql += " ORDER BY CreatedAt DESC";

        var rows = await _connection.QueryAsync<dynamic>(sql, parameters);
        return rows.Select(HydrateIntent).ToList();
    }

    public async Task<XrplIntentStats> GetStatsAsync(CancellationToken ct = default)
    {
        const string sql = """
            SELECT
                COUNT(*) AS TotalCount,
                SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS DraftCount,
                SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS ApprovedCount,
                SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS CancelledCount,
                SUM(CASE WHEN Status = 4 THEN 1 ELSE 0 END) AS MatchedCount,
                SUM(CASE WHEN IntentType = 2 THEN 1 ELSE 0 END) AS TransferCount,
                SUM(CASE WHEN IntentType = 3 THEN 1 ELSE 0 END) AS ReconcileCount,
                SUM(CASE WHEN IntentType = 4 THEN 1 ELSE 0 END) AS BudgetCount,
                SUM(CASE WHEN IntentType = 1 THEN 1 ELSE 0 END) AS TrackCount
            FROM XrplIntents
            """;

        var row = await _connection.QuerySingleAsync<dynamic>(sql);

        return new XrplIntentStats
        {
            TotalCount = (int)(row.TotalCount ?? 0),
            DraftCount = (int)(row.DraftCount ?? 0),
            ApprovedCount = (int)(row.ApprovedCount ?? 0),
            CancelledCount = (int)(row.CancelledCount ?? 0),
            MatchedCount = (int)(row.MatchedCount ?? 0),
            TransferCount = (int)(row.TransferCount ?? 0),
            ReconcileCount = (int)(row.ReconcileCount ?? 0),
            BudgetCount = (int)(row.BudgetCount ?? 0),
            TrackCount = (int)(row.TrackCount ?? 0)
        };
    }

    private static XrplIntent HydrateIntent(dynamic row)
    {
        return XrplIntent.Hydrate(
            id: Guid.Parse((string)row.Id),
            intentType: (int)row.IntentType,
            status: (int)row.Status,
            sourceAccountId: row.SourceAccountId is string s1 ? Guid.Parse(s1) : null,
            sourceAddress: (string?)row.SourceAddress,
            destinationAddress: (string?)row.DestinationAddress,
            amountDrops: (long?)row.AmountDrops,
            destinationTag: row.DestinationTag is long dt ? (uint?)dt : null,
            memo: (string?)row.Memo,
            network: (string)row.Network,
            userNote: (string?)row.UserNote,
            createdAt: DateTime.Parse((string)row.CreatedAt),
            approvedAt: row.ApprovedAt is string s2 ? DateTime.Parse(s2) : null,
            cancelledAt: row.CancelledAt is string s3 ? DateTime.Parse(s3) : null,
            matchedAt: row.MatchedAt is string s4 ? DateTime.Parse(s4) : null,
            appVersion: (string?)row.AppVersion,
            actionSource: (string?)row.ActionSource,
            previewBalanceDrops: (long?)row.PreviewBalanceDrops,
            projectedBalanceDrops: (long?)row.ProjectedBalanceDrops,
            estimatedFeeDrops: (long?)row.EstimatedFeeDrops,
            reserveRequirementDrops: (long?)row.ReserveRequirementDrops,
            validationWarnings: (string?)row.ValidationWarnings,
            isValid: ((long?)row.IsValid ?? 0) == 1,
            matchedTransactionHash: (string?)row.MatchedTransactionHash,
            matchedLedgerIndex: (long?)row.MatchedLedgerIndex
        );
    }
}
