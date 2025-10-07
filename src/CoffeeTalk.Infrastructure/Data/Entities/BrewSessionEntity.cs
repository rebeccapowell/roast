namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class BrewSessionEntity
{
    public Guid Id { get; set; }

    public Guid CoffeeBarId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public CoffeeBarEntity? CoffeeBar { get; set; }

    public ICollection<BrewCycleEntity> Cycles { get; set; } = new List<BrewCycleEntity>();
}
