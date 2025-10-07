using CoffeeTalk.Domain.CoffeeBars;
using System.Linq;

namespace CoffeeTalk.Api.Services;

public sealed class RandomNextIngredientSelector : INextIngredientSelector
{
    public Ingredient? PickNext(IReadOnlyCollection<Ingredient> candidates)
    {
        ArgumentNullException.ThrowIfNull(candidates);

        if (candidates.Count == 0)
        {
            return null;
        }

        var index = Random.Shared.Next(candidates.Count);
        return candidates.ElementAt(index);
    }
}
