namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record BrewCycleResource(
    Guid Id,
    Guid SessionId,
    Guid IngredientId,
    string VideoId,
    DateTimeOffset StartedAt,
    DateTimeOffset? RevealedAt,
    bool IsActive,
    IReadOnlyList<VoteResource> Votes,
    IReadOnlyList<Guid> SubmitterIds);
