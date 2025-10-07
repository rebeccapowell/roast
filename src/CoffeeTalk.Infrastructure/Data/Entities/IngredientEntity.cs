namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class IngredientEntity
{
    public Guid Id { get; set; }

    public Guid CoffeeBarId { get; set; }

    public string VideoId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public bool IsConsumed { get; set; }

    public CoffeeBarEntity? CoffeeBar { get; set; }

    public ICollection<SubmissionEntity> Submissions { get; set; } = new List<SubmissionEntity>();

    public ICollection<BrewCycleEntity> BrewCycles { get; set; } = new List<BrewCycleEntity>();
}
