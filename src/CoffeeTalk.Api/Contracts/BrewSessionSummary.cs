namespace CoffeeTalk.Api.Contracts;

public sealed record BrewSessionSummary(Guid Id, string Name, DateTimeOffset StartedAt);
