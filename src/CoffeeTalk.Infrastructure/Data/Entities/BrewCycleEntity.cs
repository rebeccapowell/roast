namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class BrewCycleEntity
{
    public Guid Id { get; set; }

    public Guid BrewSessionId { get; set; }

    public Guid IngredientId { get; set; }

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset? RevealedAt { get; set; }

    public BrewSessionEntity? BrewSession { get; set; }

    public IngredientEntity? Ingredient { get; set; }

    public ICollection<VoteEntity> Votes { get; set; } = new List<VoteEntity>();
}
