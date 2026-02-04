using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace NextLedger.Infrastructure.Tests.Repositories;

public class TransactionRepositoryTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly AccountRepository _accountRepo;
    private readonly EnvelopeRepository _envelopeRepo;
    private readonly TransactionRepository _transactionRepo;
    private readonly Guid _accountId;
    private readonly Guid _envelopeId;

    public TransactionRepositoryTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _accountRepo = new AccountRepository(_connectionFactory);
        _envelopeRepo = new EnvelopeRepository(_connectionFactory);
        _transactionRepo = new TransactionRepository(_connectionFactory);

        // Create test account and envelope
        var account = Account.Create("Test Account", AccountType.Checking);
        _accountRepo.AddAsync(account).GetAwaiter().GetResult();
        _accountId = account.Id;

        var envelope = Envelope.Create("Groceries");
        _envelopeRepo.AddAsync(envelope).GetAwaiter().GetResult();
        _envelopeId = envelope.Id;
    }

    [Fact]
    public async Task AddAsync_CreatesOutflow()
    {
        var tx = Transaction.CreateOutflow(
            _accountId,
            DateOnly.FromDateTime(DateTime.Today),
            new Money(50m),
            "Store",
            _envelopeId);

        var id = await _transactionRepo.AddAsync(tx);

        id.Should().Be(tx.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTransaction()
    {
        var tx = Transaction.CreateOutflow(
            _accountId,
            DateOnly.FromDateTime(DateTime.Today),
            new Money(75m),
            "Gas Station");
        await _transactionRepo.AddAsync(tx);

        var result = await _transactionRepo.GetByIdAsync(tx.Id);

        result.Should().NotBeNull();
        result!.Payee.Should().Be("Gas Station");
        result.Amount.Amount.Should().Be(-75m); // Outflows are negative
    }

    [Fact]
    public async Task GetByAccountAsync_ReturnsAccountTransactions()
    {
        var tx1 = Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(10m), "A");
        var tx2 = Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(20m), "B");

        await _transactionRepo.AddAsync(tx1);
        await _transactionRepo.AddAsync(tx2);

        var results = await _transactionRepo.GetByAccountAsync(_accountId);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByDateRangeAsync_FiltersCorrectly()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var lastWeek = today.AddDays(-7);
        var nextWeek = today.AddDays(7);

        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(50m), "Today"));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, lastWeek, new Money(30m), "Last Week"));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, nextWeek, new Money(40m), "Next Week"));

        var range = new DateRange(lastWeek, today);
        var results = await _transactionRepo.GetByDateRangeAsync(range);

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAccountBalanceAsync_SumsTransactions()
    {
        await _transactionRepo.AddAsync(Transaction.CreateInflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(1000m), "Income"));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(300m), "Expense"));

        var balance = await _transactionRepo.GetAccountBalanceAsync(_accountId);

        balance.Amount.Should().Be(700m); // 1000 - 300
    }

    [Fact]
    public async Task GetAccountClearedBalanceAsync_OnlySumsCleared()
    {
        var cleared = Transaction.CreateInflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(1000m), "Cleared");
        cleared.MarkCleared();
        await _transactionRepo.AddAsync(cleared);

        var uncleared = Transaction.CreateInflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(500m), "Uncleared");
        await _transactionRepo.AddAsync(uncleared);

        var balance = await _transactionRepo.GetAccountClearedBalanceAsync(_accountId);

        balance.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task GetEnvelopeSpentAsync_SumsOutflows()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(50m), "A", _envelopeId));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(30m), "B", _envelopeId));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(20m), "C")); // Different envelope

        var range = DateRange.ForMonth(today.Year, today.Month);
        var spent = await _transactionRepo.GetEnvelopeSpentAsync(_envelopeId, range);

        spent.Amount.Should().Be(80m);
    }

    [Fact]
    public async Task GetUnassignedAsync_ReturnsTransactionsWithoutEnvelope()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(50m), "Assigned", _envelopeId));
        await _transactionRepo.AddAsync(Transaction.CreateOutflow(_accountId, today, new Money(30m), "Unassigned"));

        var results = await _transactionRepo.GetUnassignedAsync();

        results.Should().ContainSingle();
        results[0].Payee.Should().Be("Unassigned");
    }

    [Fact]
    public async Task GetUnclearedAsync_ReturnsUnclearedOnly()
    {
        var cleared = Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(50m), "Cleared");
        cleared.MarkCleared();
        await _transactionRepo.AddAsync(cleared);

        var uncleared = Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(30m), "Uncleared");
        await _transactionRepo.AddAsync(uncleared);

        var results = await _transactionRepo.GetUnclearedAsync(_accountId);

        results.Should().ContainSingle();
        results[0].Payee.Should().Be("Uncleared");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesTransaction()
    {
        var tx = Transaction.CreateOutflow(_accountId, DateOnly.FromDateTime(DateTime.Today), new Money(50m), "Original");
        await _transactionRepo.AddAsync(tx);

        tx.SetPayee("Updated");
        tx.SetMemo("Test memo");
        await _transactionRepo.UpdateAsync(tx);

        var result = await _transactionRepo.GetByIdAsync(tx.Id);
        result!.Payee.Should().Be("Updated");
        result.Memo.Should().Be("Test memo");
    }

    public void Dispose()
    {
        _connectionFactory.Dispose();
    }
}
