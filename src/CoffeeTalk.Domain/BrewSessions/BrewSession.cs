namespace CoffeeTalk.Domain.BrewSessions;

public sealed class BrewSession
{
    private readonly List<BrewCycle> _cycles = new();

    internal BrewSession(Guid id, Guid coffeeBarId, DateTimeOffset startedAt)
    {
        Id = id;
        CoffeeBarId = coffeeBarId;
        StartedAt = startedAt;
    }

    public Guid Id { get; }

    public Guid CoffeeBarId { get; }

    public DateTimeOffset StartedAt { get; }

    public IReadOnlyCollection<BrewCycle> Cycles => _cycles;

    internal BrewCycle StartCycle(Guid cycleId, Guid ingredientId, DateTimeOffset startedAt)
    {
        if (_cycles.LastOrDefault()?.IsActive == true)
        {
            throw new DomainException("A brew cycle is already active for this session.");
        }

        var cycle = new BrewCycle(cycleId, Id, ingredientId, startedAt);
        _cycles.Add(cycle);
        return cycle;
    }

    internal BrewCycle GetCycle(Guid cycleId)
    {
        var cycle = _cycles.FirstOrDefault(cycle => cycle.Id == cycleId);
        return cycle ?? throw new DomainException("Brew cycle was not found in the session.");
    }

    internal static BrewSession FromState(Guid id, Guid coffeeBarId, DateTimeOffset startedAt, IEnumerable<BrewCycle> cycles)
    {
        ArgumentNullException.ThrowIfNull(cycles);

        var session = new BrewSession(id, coffeeBarId, startedAt);
        session._cycles.AddRange(cycles);
        return session;
    }
}
