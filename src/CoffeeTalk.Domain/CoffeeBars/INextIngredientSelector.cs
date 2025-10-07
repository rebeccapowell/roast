namespace CoffeeTalk.Domain.CoffeeBars;

public interface INextIngredientSelector
{
    Ingredient? PickNext(IReadOnlyCollection<Ingredient> candidates);
}
