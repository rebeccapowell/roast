using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class HipsterMapper
{
    public static HipsterEntity ToEntity(Guid coffeeBarId, Hipster hipster)
    {
        ArgumentNullException.ThrowIfNull(hipster);

        return new HipsterEntity
        {
            Id = hipster.Id,
            CoffeeBarId = coffeeBarId,
            Username = hipster.Username,
            NormalizedUsername = hipster.NormalizedUsername,
            MaxIngredientQuota = hipster.MaxIngredientQuota
        };
    }

    public static Hipster ToDomain(HipsterEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Hipster.FromState(entity.Id, entity.Username, entity.NormalizedUsername, entity.MaxIngredientQuota);
    }
}
