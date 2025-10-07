using System;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record LeaderboardEntryResource(
    Guid HipsterId,
    string Username,
    int Score,
    int Rank,
    int? PreviousRank,
    LeaderboardTrend Trend);
