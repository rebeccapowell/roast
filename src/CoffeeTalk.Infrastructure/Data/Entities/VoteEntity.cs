namespace CoffeeTalk.Infrastructure.Data.Entities;

public sealed class VoteEntity
{
    public Guid Id { get; set; }

    public Guid BrewCycleId { get; set; }

    public Guid VoterHipsterId { get; set; }

    public Guid TargetHipsterId { get; set; }

    public DateTimeOffset CastAt { get; set; }

    public bool? IsCorrect { get; set; }

    public BrewCycleEntity? BrewCycle { get; set; }

    public HipsterEntity? Voter { get; set; }
}
