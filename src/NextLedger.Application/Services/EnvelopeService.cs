using NextLedger.Application.DTOs;
using NextLedger.Application.Interfaces;
using NextLedger.Domain.Entities;
using NextLedger.Domain.Services;
using NextLedger.Domain.ValueObjects;

namespace NextLedger.Application.Services;

/// <summary>
/// Service for managing envelopes and allocations.
/// Core of the envelope budgeting system.
/// </summary>
public sealed class EnvelopeService
{
    private readonly IUnitOfWork _unitOfWork;

    public EnvelopeService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    /// <summary>
    /// Create a new envelope (budget category).
    /// </summary>
    public async Task<Envelope> CreateEnvelopeAsync(
        string name,
        string? groupName = null,
        string? color = null,
        CancellationToken ct = default)
    {
        // Check for duplicate name
        var existing = await _unitOfWork.Envelopes.GetByNameAsync(name, ct);
        if (existing is not null)
            throw new InvalidOperationException($"Envelope with name '{name}' already exists.");

        var envelope = Envelope.Create(name, groupName, color);
        await _unitOfWork.Envelopes.AddAsync(envelope, ct);
        return envelope;
    }

    /// <summary>
    /// Get all active envelopes with their current allocations.
    /// </summary>
    public async Task<IReadOnlyList<EnvelopeDto>> GetEnvelopesForPeriodAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var envelopes = await _unitOfWork.Envelopes.GetActiveEnvelopesAsync(ct);
        var allocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(period.Id, ct);

        var result = new List<EnvelopeDto>();
        var dateRange = DateRange.ForMonth(year, month);

        foreach (var envelope in envelopes)
        {
            var allocation = allocations.FirstOrDefault(a => a.EnvelopeId == envelope.Id);
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(envelope.Id, dateRange, ct);

            var allocated = allocation?.Allocated ?? Money.Zero;
            var rollover = allocation?.RolloverFromPrevious ?? Money.Zero;
            var totalBudgeted = allocated + rollover;
            var available = totalBudgeted - spent;

            result.Add(new EnvelopeDto
            {
                Id = envelope.Id,
                Name = envelope.Name,
                GroupName = envelope.GroupName,
                Color = envelope.Color,
                Allocated = totalBudgeted,
                Spent = spent,
                Available = available,
                GoalAmount = envelope.GoalAmount,
                GoalDate = envelope.GoalDate
            });
        }

        return result;
    }

    /// <summary>
    /// Allocate money to an envelope for the current period.
    /// </summary>
    public async Task<EnvelopeAllocation> AllocateAsync(
        Guid envelopeId,
        Money amount,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Allocation amount cannot be negative.", nameof(amount));

        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        if (period.IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");
        var allocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelopeId, period.Id, ct);

        // Guardrail: only allow increasing allocations up to ReadyToAssign.
        var delta = amount - allocation.Allocated;
        if (delta.IsPositive)
        {
            var currentTotalAllocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);
            var readyToAssign = BudgetMath.ComputeReadyToAssign(period.TotalIncome, period.CarriedOver, currentTotalAllocated);

            if (delta > readyToAssign)
                throw new InvalidOperationException($"Insufficient ReadyToAssign. Available: {readyToAssign.ToFormattedString()}");
        }

        allocation.SetAllocation(amount);
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(allocation, ct);

        // Update period totals
        var updatedTotalAllocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);
        period.UpdateAllocated(updatedTotalAllocated);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period, ct);

        return allocation;
    }

    /// <summary>
    /// Add to an envelope's allocation (without replacing).
    /// </summary>
    public async Task<EnvelopeAllocation> AddToAllocationAsync(
        Guid envelopeId,
        Money amount,
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        if (period.IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");
        var allocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(envelopeId, period.Id, ct);

        // Guardrail: allow decreases, but block increases above ReadyToAssign.
        if (amount.IsPositive)
        {
            var currentTotalAllocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);
            var readyToAssign = BudgetMath.ComputeReadyToAssign(period.TotalIncome, period.CarriedOver, currentTotalAllocated);

            if (amount > readyToAssign)
                throw new InvalidOperationException($"Insufficient ReadyToAssign. Available: {readyToAssign.ToFormattedString()}");
        }

        allocation.AddToAllocation(amount);
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(allocation, ct);

        // Update period totals
        var updatedTotalAllocated = await _unitOfWork.EnvelopeAllocations.GetTotalAllocatedForPeriodAsync(period.Id, ct);
        period.UpdateAllocated(updatedTotalAllocated);
        await _unitOfWork.BudgetPeriods.UpdateAsync(period, ct);

        return allocation;
    }

    /// <summary>
    /// Move money between envelopes within the same period.
    /// </summary>
    public async Task MoveMoneyAsync(
        Guid fromEnvelopeId,
        Guid toEnvelopeId,
        Money amount,
        int year,
        int month,
        CancellationToken ct = default)
    {
        if (amount.IsNegative)
            throw new ArgumentException("Amount to move must be positive.", nameof(amount));

        if (fromEnvelopeId == toEnvelopeId)
            throw new ArgumentException("Cannot move money to the same envelope.");

        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        if (period.IsClosed)
            throw new InvalidOperationException("Cannot modify closed budget period.");

        var fromAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(fromEnvelopeId, period.Id, ct);
        var toAllocation = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(toEnvelopeId, period.Id, ct);

        // Get current available to check if move is valid
        var dateRange = DateRange.ForMonth(year, month);
        var fromSpent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(fromEnvelopeId, dateRange, ct);
        var fromAvailable = fromAllocation.Allocated + fromAllocation.RolloverFromPrevious - fromSpent;

        if (amount > fromAvailable)
            throw new InvalidOperationException($"Insufficient funds. Available: {fromAvailable.ToFormattedString()}");

        // Perform the move
        fromAllocation.MoveTo(toAllocation, amount);

        await _unitOfWork.EnvelopeAllocations.UpdateAsync(fromAllocation, ct);
        await _unitOfWork.EnvelopeAllocations.UpdateAsync(toAllocation, ct);
    }

    /// <summary>
    /// Roll over envelope balances to the next month.
    /// </summary>
    public async Task RolloverToNextMonthAsync(
        int fromYear,
        int fromMonth,
        CancellationToken ct = default)
    {
        var currentPeriod = await _unitOfWork.BudgetPeriods.GetByYearMonthAsync(fromYear, fromMonth, ct)
            ?? throw new InvalidOperationException($"Budget period {fromYear}-{fromMonth} not found.");

        if (currentPeriod.IsClosed)
            throw new InvalidOperationException("Cannot rollover a closed budget period.");

        var nextMonth = fromMonth == 12 ? 1 : fromMonth + 1;
        var nextYear = fromMonth == 12 ? fromYear + 1 : fromYear;

        var nextPeriod = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(nextYear, nextMonth, ct);

        var currentAllocations = await _unitOfWork.EnvelopeAllocations.GetByPeriodAsync(currentPeriod.Id, ct);
        var dateRange = DateRange.ForMonth(fromYear, fromMonth);

        foreach (var currentAlloc in currentAllocations)
        {
            var spent = await _unitOfWork.Transactions.GetEnvelopeSpentAsync(currentAlloc.EnvelopeId, dateRange, ct);
            currentAlloc.UpdateSpent(spent);
            await _unitOfWork.EnvelopeAllocations.UpdateAsync(currentAlloc, ct);

            var rollover = currentAlloc.CalculateRollover();

            var nextAlloc = await _unitOfWork.EnvelopeAllocations.GetOrCreateAsync(currentAlloc.EnvelopeId, nextPeriod.Id, ct);
            nextAlloc.SetRollover(rollover);
            await _unitOfWork.EnvelopeAllocations.UpdateAsync(nextAlloc, ct);
        }

        // Mark current period as closed
        currentPeriod.Close();
        await _unitOfWork.BudgetPeriods.UpdateAsync(currentPeriod, ct);
    }

    /// <summary>
    /// Set a savings goal for an envelope.
    /// </summary>
    public async Task SetGoalAsync(
        Guid envelopeId,
        Money goalAmount,
        DateOnly? targetDate = null,
        CancellationToken ct = default)
    {
        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        envelope.SetGoal(goalAmount, targetDate);
        await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
    }

    /// <summary>
    /// Get budget summary for a period.
    /// </summary>
    public async Task<BudgetSummaryDto> GetBudgetSummaryAsync(
        int year,
        int month,
        CancellationToken ct = default)
    {
        var period = await _unitOfWork.BudgetPeriods.GetOrCreateAsync(year, month, ct);
        var envelopes = await GetEnvelopesForPeriodAsync(year, month, ct);

        var totalAllocated = envelopes.Aggregate(Money.Zero, (sum, e) => sum + e.Allocated);
        var totalSpent = envelopes.Aggregate(Money.Zero, (sum, e) => sum + e.Spent);

        return new BudgetSummaryDto
        {
            Year = year,
            Month = month,
            IsClosed = period.IsClosed,
            CarriedOver = period.CarriedOver,
            TotalIncome = period.TotalIncome,
            TotalAllocated = totalAllocated,
            TotalSpent = totalSpent,
            ReadyToAssign = period.TotalIncome + period.CarriedOver - totalAllocated,
            Envelopes = envelopes
        };
    }

    /// <summary>
    /// Archive an envelope (soft delete).
    /// </summary>
    public async Task ArchiveEnvelopeAsync(Guid envelopeId, CancellationToken ct = default)
    {
        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        envelope.Archive();
        await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
    }

    /// <summary>
    /// Unarchive an envelope (restore from archive).
    /// </summary>
    public async Task UnarchiveEnvelopeAsync(Guid envelopeId, CancellationToken ct = default)
    {
        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        envelope.Unarchive();
        await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
    }

    /// <summary>
    /// Update an envelope's properties (name, group, color, note, visibility).
    /// </summary>
    public async Task<Envelope> UpdateEnvelopeAsync(
        UpdateEnvelopeRequest request,
        CancellationToken ct = default)
    {
        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(request.Id, ct)
            ?? throw new InvalidOperationException($"Envelope {request.Id} not found.");

        if (request.Name is not null)
        {
            // Check for duplicate name
            var existing = await _unitOfWork.Envelopes.GetByNameAsync(request.Name, ct);
            if (existing is not null && existing.Id != request.Id)
                throw new InvalidOperationException($"Envelope with name '{request.Name}' already exists.");

            envelope.Rename(request.Name);
        }

        if (request.GroupName is not null)
            envelope.SetGroup(request.GroupName == "" ? null : request.GroupName);

        if (request.Color is not null)
            envelope.SetColor(request.Color);

        if (request.Note is not null)
            envelope.SetNote(request.Note == "" ? null : request.Note);

        if (request.IsHidden.HasValue)
        {
            if (request.IsHidden.Value)
                envelope.Hide();
            else
                envelope.Show();
        }

        await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
        return envelope;
    }

    /// <summary>
    /// Get all envelopes including archived ones.
    /// </summary>
    public async Task<IReadOnlyList<Envelope>> GetAllEnvelopesAsync(CancellationToken ct = default)
    {
        return await _unitOfWork.Envelopes.GetAllAsync(ct);
    }

    /// <summary>
    /// Get all unique group names.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetGroupNamesAsync(CancellationToken ct = default)
    {
        return await _unitOfWork.Envelopes.GetGroupNamesAsync(ct);
    }

    /// <summary>
    /// Reorder envelopes.
    /// </summary>
    public async Task ReorderEnvelopesAsync(
        IEnumerable<Guid> envelopeIdsInOrder,
        CancellationToken ct = default)
    {
        var order = 0;
        foreach (var id in envelopeIdsInOrder)
        {
            var envelope = await _unitOfWork.Envelopes.GetByIdAsync(id, ct);
            if (envelope is not null)
            {
                envelope.SetSortOrder(order++);
                await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
            }
        }
    }

    /// <summary>
    /// Clear a savings goal from an envelope.
    /// </summary>
    public async Task ClearGoalAsync(Guid envelopeId, CancellationToken ct = default)
    {
        var envelope = await _unitOfWork.Envelopes.GetByIdAsync(envelopeId, ct)
            ?? throw new InvalidOperationException($"Envelope {envelopeId} not found.");

        envelope.ClearGoal();
        await _unitOfWork.Envelopes.UpdateAsync(envelope, ct);
    }
}
