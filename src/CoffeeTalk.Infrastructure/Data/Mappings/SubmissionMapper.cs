using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

internal static class SubmissionMapper
{
    public static SubmissionEntity ToEntity(Guid coffeeBarId, Submission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        return new SubmissionEntity
        {
            Id = submission.Id,
            CoffeeBarId = coffeeBarId,
            IngredientId = submission.IngredientId,
            HipsterId = submission.HipsterId,
            SubmittedAt = submission.SubmittedAt
        };
    }

    public static Submission ToDomain(SubmissionEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        return new Submission(entity.Id, entity.IngredientId, entity.HipsterId, entity.SubmittedAt);
    }
}
