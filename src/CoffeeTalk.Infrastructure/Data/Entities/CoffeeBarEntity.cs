using CoffeeTalk.Domain.CoffeeBars;

namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class CoffeeBarEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Theme { get; set; } = string.Empty;

    public int DefaultMaxIngredientsPerHipster { get; set; }

    public SubmissionPolicy SubmissionPolicy { get; set; }

    public bool SubmissionsLocked { get; set; }

    public ICollection<HipsterEntity> Hipsters { get; set; } = new List<HipsterEntity>();

    public ICollection<IngredientEntity> Ingredients { get; set; } = new List<IngredientEntity>();

    public ICollection<SubmissionEntity> Submissions { get; set; } = new List<SubmissionEntity>();

    public ICollection<BrewSessionEntity> Sessions { get; set; } = new List<BrewSessionEntity>();
}
