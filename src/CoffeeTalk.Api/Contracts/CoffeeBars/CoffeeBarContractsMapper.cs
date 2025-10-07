using CoffeeTalk.Domain.CoffeeBars;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public static class CoffeeBarContractsMapper
{
    public static CoffeeBarResource ToResource(CoffeeBar coffeeBar)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var hipsters = coffeeBar.Hipsters
            .OrderBy(hipster => hipster.Username, StringComparer.OrdinalIgnoreCase)
            .Select(ToResource)
            .ToList();

        var ingredients = coffeeBar.Ingredients
            .OrderBy(ingredient => ingredient.CreatedAt)
            .Select(ToResource)
            .ToList();

        return new CoffeeBarResource(
            coffeeBar.Id,
            coffeeBar.Code.Value,
            coffeeBar.Theme,
            coffeeBar.DefaultMaxIngredientsPerHipster,
            coffeeBar.SubmissionPolicy,
            coffeeBar.SubmissionsLocked,
            coffeeBar.IsClosed,
            hipsters,
            ingredients);
    }

    public static HipsterResource ToResource(Hipster hipster)
    {
        ArgumentNullException.ThrowIfNull(hipster);

        return new HipsterResource(hipster.Id, hipster.Username, hipster.MaxIngredientQuota);
    }

    public static IngredientResource ToResource(Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        return new IngredientResource(
            ingredient.Id,
            ingredient.VideoId,
            ingredient.IsConsumed,
            ingredient.SubmitterIds.ToList());
    }
}
