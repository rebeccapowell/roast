using System;
using System.Collections.Generic;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record SessionLeaderboardResource(
    Guid SessionId,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    IReadOnlyList<LeaderboardEntryResource> Entries);
