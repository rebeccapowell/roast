namespace CoffeeTalk.Domain.CoffeeBars;

public sealed class Submission
{
    internal Submission(Guid id, Guid ingredientId, Guid hipsterId, DateTimeOffset submittedAt)
    {
        Id = id;
        IngredientId = ingredientId;
        HipsterId = hipsterId;
        SubmittedAt = submittedAt;
    }

    public Guid Id { get; }

    public Guid IngredientId { get; }

    public Guid HipsterId { get; }

    public DateTimeOffset SubmittedAt { get; }
}
