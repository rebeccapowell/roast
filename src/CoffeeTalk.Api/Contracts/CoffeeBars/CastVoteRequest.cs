namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record CastVoteRequest(Guid VoterHipsterId, Guid TargetHipsterId);
