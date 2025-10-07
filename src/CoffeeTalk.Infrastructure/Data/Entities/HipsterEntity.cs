namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class HipsterEntity
{
    public Guid Id { get; set; }

    public Guid CoffeeBarId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string NormalizedUsername { get; set; } = string.Empty;

    public int MaxIngredientQuota { get; set; }

    public CoffeeBarEntity? CoffeeBar { get; set; }

    public ICollection<SubmissionEntity> Submissions { get; set; } = new List<SubmissionEntity>();

    public ICollection<VoteEntity> Votes { get; set; } = new List<VoteEntity>();
}
