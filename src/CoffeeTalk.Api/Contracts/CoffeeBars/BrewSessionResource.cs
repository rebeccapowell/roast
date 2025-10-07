namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record BrewSessionResource(
    Guid Id,
    DateTimeOffset StartedAt,
    IReadOnlyList<BrewCycleResource> Cycles);
