using System.Collections.Generic;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record CoffeeBarLeaderboardResource(
    IReadOnlyList<LeaderboardEntryResource> Overall,
    IReadOnlyList<SessionLeaderboardResource> Sessions);
