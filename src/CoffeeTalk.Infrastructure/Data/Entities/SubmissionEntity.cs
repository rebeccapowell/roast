namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class SubmissionEntity
{
    public Guid Id { get; set; }

    public Guid CoffeeBarId { get; set; }

    public Guid IngredientId { get; set; }

    public Guid HipsterId { get; set; }

    public DateTimeOffset SubmittedAt { get; set; }

    public CoffeeBarEntity? CoffeeBar { get; set; }

    public IngredientEntity? Ingredient { get; set; }

    public HipsterEntity? Hipster { get; set; }
}
