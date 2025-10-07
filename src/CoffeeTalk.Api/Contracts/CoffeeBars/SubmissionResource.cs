namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record SubmissionResource(
    Guid Id,
    Guid IngredientId,
    Guid HipsterId,
    DateTimeOffset SubmittedAt);
