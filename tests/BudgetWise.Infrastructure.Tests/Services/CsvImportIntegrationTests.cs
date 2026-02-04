using BudgetWise.Application.DTOs;
using BudgetWise.Application.Services;
using BudgetWise.Domain.Entities;
using BudgetWise.Domain.Enums;
using BudgetWise.Domain.ValueObjects;
using BudgetWise.Infrastructure.Database;
using BudgetWise.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace BudgetWise.Infrastructure.Tests.Services;

public sealed class CsvImportIntegrationTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly UnitOfWork _unitOfWork;
    private readonly BudgetEngine _engine;
    private readonly TransactionService _transactions;

    public CsvImportIntegrationTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _unitOfWork = new UnitOfWork(_connectionFactory);
        _engine = new BudgetEngine(_unitOfWork);
        _transactions = new TransactionService(_unitOfWork);
    }

    [Fact]
    public async Task PreviewCsvImportAsync_FlagsExistingAndInFileDuplicates_AndInvalidRows()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        // Existing outflow that should be detected as a duplicate.
        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 5),
            Amount = new Money(10m),
            Payee = "Coffee Shop",
            Memo = "Latte"
        });

        var csv = string.Join("\n", new[]
        {
            "Date,Payee,Amount,Memo",
            // Existing duplicate (different casing/spacing)
            "2026-02-05,coffee   shop,-10.00,latte",
            // New row
            "2026-02-06,Grocery Store,-25.50,",
            // Duplicate within file (same as previous new row)
            "2026-02-06,Grocery Store,-25.50,",
            // Invalid date
            "not-a-date,Whatever,-1.00,",
        });

        var result = await _engine.PreviewCsvImportAsync(new CsvImportPreviewRequest
        {
            AccountId = account.Id,
            CsvText = csv,
            HasHeaderRow = true
        });

        result.Success.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var preview = result.Value!;
        preview.TotalRowCount.Should().Be(4);
        preview.ParsedRowCount.Should().Be(3);
        preview.NewCount.Should().Be(1);
        preview.DuplicateCount.Should().Be(2);
        preview.InvalidCount.Should().Be(1);

        preview.Rows.Should().Contain(r => r.Status == CsvImportRowStatus.Invalid);
        preview.Rows.Should().Contain(r => r.Status == CsvImportRowStatus.Duplicate);
        preview.Rows.Should().Contain(r => r.Status == CsvImportRowStatus.New);
    }

    [Fact]
    public async Task CommitCsvImportAsync_InsertsNewRows_SkipsDuplicates_IsIdempotent_AndRecalculates()
    {
        var account = Account.Create("Checking", AccountType.Checking);
        await _unitOfWork.Accounts.AddAsync(account);

        // Seed one existing transaction in Feb so preview/commit will treat it as a duplicate.
        await _transactions.CreateOutflowAsync(new CreateOutflowRequest
        {
            AccountId = account.Id,
            Date = new DateOnly(2026, 2, 1),
            Amount = new Money(5m),
            Payee = "Coffee",
            Memo = ""
        });

        var csv = string.Join("\n", new[]
        {
            "Date,Description,Deposit,Withdrawal,Notes",
            // Duplicate of seed
            "2026-02-01,Coffee,,5.00,",
            // New outflow
            "2026-02-02,Groceries,,40.00,",
            // New inflow
            "2026-02-03,Paycheck,100.00,,",
            // Duplicate within file (same inflow)
            "2026-02-03,Paycheck,100.00,,",
        });

        var preview = await _engine.PreviewCsvImportAsync(new CsvImportPreviewRequest
        {
            AccountId = account.Id,
            CsvText = csv,
            HasHeaderRow = true
        });

        preview.Success.Should().BeTrue();
        preview.Value!.NewCount.Should().Be(2);
        preview.Value.DuplicateCount.Should().Be(2);

        var commitRows = preview.Value.Rows
            .Where(r => r.Status == CsvImportRowStatus.New)
            .Select(r => new CsvImportCommitRowDto
            {
                RowNumber = r.RowNumber,
                Date = r.Date!.Value,
                Amount = r.Amount!.Value,
                Payee = r.Payee,
                Memo = r.Memo,
                Fingerprint = r.Fingerprint!
            })
            .ToArray();

        var commit = await _engine.CommitCsvImportAsync(new CsvImportCommitRequest
        {
            AccountId = account.Id,
            Rows = commitRows
        });

        commit.Success.Should().BeTrue();
        commit.Value!.InsertedCount.Should().Be(2);
        commit.Value.SkippedDuplicateCount.Should().Be(0);
        commit.Snapshot.Should().NotBeNull();

        // Verify transactions landed.
        var txs = await _unitOfWork.Transactions.GetByAccountAsync(account.Id);
        txs.Should().HaveCount(3); // seed + 2 new

        // Verify budget snapshot reflects imported inflow/spend in the impacted period.
        var snapshot = await _engine.GetSnapshotAsync(2026, 2);
        snapshot.TotalIncome.Should().Be(new Money(100m));
        snapshot.TotalSpent.Should().Be(new Money(45m)); // 5 seed + 40 groceries

        // Idempotency: running the same commit again should insert nothing.
        var commitAgain = await _engine.CommitCsvImportAsync(new CsvImportCommitRequest
        {
            AccountId = account.Id,
            Rows = commitRows
        });

        commitAgain.Success.Should().BeTrue();
        commitAgain.Value!.InsertedCount.Should().Be(0);
        commitAgain.Value.SkippedDuplicateCount.Should().Be(2);

        var txsAfter = await _unitOfWork.Transactions.GetByAccountAsync(account.Id);
        txsAfter.Should().HaveCount(3);
    }

    public void Dispose()
    {
        _unitOfWork.Dispose();
        _connectionFactory.Dispose();
    }
}
