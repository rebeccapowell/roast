using System.Collections.Generic;
using System.Linq;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class BrewSessionMapper
{
    public static BrewSessionEntity ToEntity(BrewSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        var entity = new BrewSessionEntity
        {
            Id = session.Id,
            CoffeeBarId = session.CoffeeBarId,
            StartedAt = session.StartedAt
        };

        entity.Cycles = session.Cycles
            .Select(BrewCycleMapper.ToEntity)
            .ToList();

        return entity;
    }

    public static BrewSession ToDomain(BrewSessionEntity entity, IEnumerable<BrewCycle> cycles)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return BrewSession.FromState(entity.Id, entity.CoffeeBarId, entity.StartedAt, cycles);
    }
}
