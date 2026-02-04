using System.Collections.Concurrent;

namespace NextLedger.App.Services;

public sealed record EngineCallMetric
{
    public required DateTimeOffset Timestamp { get; init; }
    public required string Operation { get; init; }
    public required TimeSpan Elapsed { get; init; }
    public required bool Success { get; init; }
    public string? ErrorCode { get; init; }
}

public interface IEngineMetricsSink
{
    void Record(EngineCallMetric metric);
    IReadOnlyList<EngineCallMetric> GetRecent(int max = 100);
}

public sealed class InMemoryEngineMetricsSink : IEngineMetricsSink
{
    private readonly ConcurrentQueue<EngineCallMetric> _queue = new();
    private readonly int _capacity;

    public InMemoryEngineMetricsSink(int capacity = 500)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void Record(EngineCallMetric metric)
    {
        _queue.Enqueue(metric);

        while (_queue.Count > _capacity)
            _queue.TryDequeue(out _);
    }

    public IReadOnlyList<EngineCallMetric> GetRecent(int max = 100)
    {
        if (max <= 0)
            return Array.Empty<EngineCallMetric>();

        var snapshot = _queue.ToArray();
        if (snapshot.Length == 0)
            return Array.Empty<EngineCallMetric>();

        return snapshot
            .OrderByDescending(m => m.Timestamp)
            .Take(max)
            .ToList();
    }
}
