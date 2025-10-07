namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record SubmitIngredientResponse(
    CoffeeBarResource CoffeeBar,
    IngredientResource Ingredient,
    Guid SubmissionId);
