namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record BrewCycleResource(
    Guid Id,
    Guid SessionId,
    Guid IngredientId,
    string VideoId,
    string? VideoTitle,
    string? ThumbnailUrl,
    DateTimeOffset StartedAt,
    DateTimeOffset? RevealedAt,
    bool IsActive,
    IReadOnlyList<VoteResource> Votes,
    IReadOnlyList<Guid> SubmitterIds);
