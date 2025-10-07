using System.Collections.Generic;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class IngredientMapper
{
    public static IngredientEntity ToEntity(Guid coffeeBarId, Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        return new IngredientEntity
        {
            Id = ingredient.Id,
            CoffeeBarId = coffeeBarId,
            VideoId = ingredient.VideoId,
            CreatedAt = ingredient.CreatedAt,
            IsConsumed = ingredient.IsConsumed,
            Title = ingredient.Title,
            ThumbnailUrl = ingredient.ThumbnailUrl
        };
    }

    public static Ingredient ToDomain(IngredientEntity entity, IEnumerable<Guid> submitterIds)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return Ingredient.FromState(
            entity.Id,
            entity.VideoId,
            entity.CreatedAt,
            entity.IsConsumed,
            submitterIds,
            entity.Title,
            entity.ThumbnailUrl);
    }
}
