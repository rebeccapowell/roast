namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record VoteResource(
    Guid Id,
    Guid VoterHipsterId,
    Guid TargetHipsterId,
    DateTimeOffset CastAt,
    bool? IsCorrect);
