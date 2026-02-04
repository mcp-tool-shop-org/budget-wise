using NextLedger.Domain.Entities;
using NextLedger.Domain.ValueObjects;
using NextLedger.Infrastructure.Database;
using NextLedger.Infrastructure.Repositories;
using FluentAssertions;
using Xunit;

namespace NextLedger.Infrastructure.Tests.Repositories;

public class EnvelopeRepositoryTests : IDisposable
{
    private readonly SqliteConnectionFactory _connectionFactory;
    private readonly EnvelopeRepository _repository;

    public EnvelopeRepositoryTests()
    {
        _connectionFactory = SqliteConnectionFactory.CreateInMemory();
        _connectionFactory.InitializeDatabaseAsync().GetAwaiter().GetResult();
        _repository = new EnvelopeRepository(_connectionFactory);
    }

    [Fact]
    public async Task AddAsync_CreatesEnvelope()
    {
        var envelope = Envelope.Create("Groceries", "Needs", "#FF5733");

        var id = await _repository.AddAsync(envelope);

        id.Should().Be(envelope.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsEnvelope()
    {
        var envelope = Envelope.Create("Utilities", "Bills");
        await _repository.AddAsync(envelope);

        var result = await _repository.GetByIdAsync(envelope.Id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("Utilities");
        result.GroupName.Should().Be("Bills");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllEnvelopes()
    {
        await _repository.AddAsync(Envelope.Create("Groceries"));
        await _repository.AddAsync(Envelope.Create("Gas"));
        await _repository.AddAsync(Envelope.Create("Entertainment"));

        var results = await _repository.GetAllAsync();

        results.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetActiveEnvelopesAsync_ExcludesArchived()
    {
        var active = Envelope.Create("Active");
        var archived = Envelope.Create("Archived");
        archived.Archive();

        await _repository.AddAsync(active);
        await _repository.AddAsync(archived);

        var results = await _repository.GetActiveEnvelopesAsync();

        results.Should().ContainSingle();
        results[0].Name.Should().Be("Active");
    }

    [Fact]
    public async Task GetByGroupAsync_FiltersCorrectly()
    {
        await _repository.AddAsync(Envelope.Create("Groceries", "Needs"));
        await _repository.AddAsync(Envelope.Create("Utilities", "Needs"));
        await _repository.AddAsync(Envelope.Create("Entertainment", "Wants"));

        var results = await _repository.GetByGroupAsync("Needs");

        results.Should().HaveCount(2);
        results.Should().AllSatisfy(e => e.GroupName.Should().Be("Needs"));
    }

    [Fact]
    public async Task GetGroupNamesAsync_ReturnsDistinctGroups()
    {
        await _repository.AddAsync(Envelope.Create("A", "Needs"));
        await _repository.AddAsync(Envelope.Create("B", "Needs"));
        await _repository.AddAsync(Envelope.Create("C", "Wants"));
        await _repository.AddAsync(Envelope.Create("D")); // No group

        var groups = await _repository.GetGroupNamesAsync();

        groups.Should().HaveCount(2);
        groups.Should().Contain("Needs");
        groups.Should().Contain("Wants");
    }

    [Fact]
    public async Task UpdateAsync_UpdatesEnvelope()
    {
        var envelope = Envelope.Create("Original", "Group1");
        await _repository.AddAsync(envelope);

        envelope.Rename("Updated");
        envelope.SetGroup("Group2");
        envelope.SetGoal(new Money(500m), DateOnly.FromDateTime(DateTime.Today.AddMonths(3)));
        await _repository.UpdateAsync(envelope);

        var result = await _repository.GetByIdAsync(envelope.Id);
        result!.Name.Should().Be("Updated");
        result.GroupName.Should().Be("Group2");
        result.GoalAmount!.Value.Amount.Should().Be(500m);
    }

    [Fact]
    public async Task GetByNameAsync_CaseInsensitive()
    {
        var envelope = Envelope.Create("Groceries");
        await _repository.AddAsync(envelope);

        var result = await _repository.GetByNameAsync("GROCERIES");

        result.Should().NotBeNull();
        result!.Id.Should().Be(envelope.Id);
    }

    public void Dispose()
    {
        _connectionFactory.Dispose();
    }
}
