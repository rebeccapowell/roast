using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class VoteMapper
{
    public static VoteEntity ToEntity(Guid brewCycleId, Vote vote)
    {
        ArgumentNullException.ThrowIfNull(vote);

        return new VoteEntity
        {
            Id = vote.Id,
            BrewCycleId = brewCycleId,
            VoterHipsterId = vote.VoterHipsterId,
            TargetHipsterId = vote.TargetHipsterId,
            CastAt = vote.CastAt,
            IsCorrect = vote.IsCorrect
        };
    }

    public static Vote ToDomain(VoteEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Vote.FromState(entity.Id, entity.VoterHipsterId, entity.TargetHipsterId, entity.CastAt, entity.IsCorrect);
    }
}
