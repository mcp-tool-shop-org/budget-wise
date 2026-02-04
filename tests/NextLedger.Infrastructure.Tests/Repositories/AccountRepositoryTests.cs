using NextLedger.Domain.Entities;
using NextLedger.Domain.Enums;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace NextLedger.Infrastructure.Tests.Repositories;

public class AccountRepositoryTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly AccountRepository _repository;

    public AccountRepositoryTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _repository = new AccountRepository(_connectionFactory);
    }

    [Fact]
    public async Task AddAsync_CreatesAccount()
    {
        var account = Account.Create("Test Checking", AccountType.Checking, new Money(500m));

        var id = await _repository.AddAsync(account);

        id.Should().Be(account.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsAccount()
    {
        var account = Account.Create("Test Account", AccountType.Savings, new Money(1000m));
        await _repository.AddAsync(account);

        var result = await _repository.GetByIdAsync(account.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Account");
        result.Type.Should().Be(AccountType.Savings);
        result.Balance.Amount.Should().Be(1000m);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllAccounts()
    {
        await _repository.AddAsync(Account.Create("Account 1", AccountType.Checking));
        await _repository.AddAsync(Account.Create("Account 2", AccountType.Savings));

        var results = await _repository.GetAllAsync();

        results.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAccount()
    {
        var account = Account.Create("Original Name", AccountType.Checking);
        await _repository.AddAsync(account);

        account.Rename("New Name");
        account.SetNote("Test note");
        await _repository.UpdateAsync(account);

        var result = await _repository.GetByIdAsync(account.Id);
        result!.Name.Should().Be("New Name");
        result.Note.Should().Be("Test note");
    }

    [Fact]
    public async Task DeleteAsync_RemovesAccount()
    {
        var account = Account.Create("To Delete", AccountType.Checking);
        await _repository.AddAsync(account);

        await _repository.DeleteAsync(account.Id);

        var result = await _repository.GetByIdAsync(account.Id);
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetActiveAccountsAsync_ReturnsOnlyActive()
    {
        var active = Account.Create("Active", AccountType.Checking);
        var inactive = Account.Create("Inactive", AccountType.Savings);
        inactive.Close(); // Need zero balance to close

        await _repository.AddAsync(active);
        await _repository.AddAsync(inactive);

        var results = await _repository.GetActiveAccountsAsync();

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetByTypeAsync_FiltersCorrectly()
    {
        await _repository.AddAsync(Account.Create("Checking 1", AccountType.Checking));
        await _repository.AddAsync(Account.Create("Checking 2", AccountType.Checking));
        await _repository.AddAsync(Account.Create("Savings 1", AccountType.Savings));

        var results = await _repository.GetByTypeAsync(AccountType.Checking);

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(a => a.Type.Should().Be(AccountType.Checking));
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive()
    {
        var account = Account.Create("My Checking", AccountType.Checking);
        await _repository.AddAsync(account);

        var result = await _repository.GetByNameAsync("my checking");

        result.Should().NotBeNull();
        result!.Id.Should().Be(account.Id);
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_WhenExists()
    {
        var account = Account.Create("Test", AccountType.Checking);
        await _repository.AddAsync(account);

        var exists = await _repository.ExistsAsync(account.Id);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenNotExists()
    {
        var exists = await _repository.ExistsAsync(Guid.NewGuid());

        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _connectionFactory.Dispose();
    }
}
