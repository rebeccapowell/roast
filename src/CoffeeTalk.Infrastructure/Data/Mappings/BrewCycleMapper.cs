using System.Collections.Generic;
using System.Linq;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class BrewCycleMapper
{
    public static BrewCycleEntity ToEntity(BrewCycle cycle)
    {
        ArgumentNullException.ThrowIfNull(cycle);

        var entity = new BrewCycleEntity
        {
            Id = cycle.Id,
            BrewSessionId = cycle.SessionId,
            IngredientId = cycle.IngredientId,
            StartedAt = cycle.StartedAt,
            RevealedAt = cycle.RevealedAt
        };

        entity.Votes = cycle.Votes
            .Select(vote => VoteMapper.ToEntity(cycle.Id, vote))
            .ToList();

        return entity;
    }

    public static BrewCycle ToDomain(BrewCycleEntity entity, IEnumerable<Vote> votes, IEnumerable<Guid> submitterIds)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return BrewCycle.FromState(entity.Id, entity.BrewSessionId, entity.IngredientId, entity.StartedAt, votes, entity.RevealedAt, submitterIds);
    }
}
