using System.Linq;
using CoffeeTalk.Domain.BrewSessions;
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

        var submissions = coffeeBar.Submissions
            .OrderByDescending(submission => submission.SubmittedAt)
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
            ingredients,
            submissions);
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

    public static SubmissionResource ToResource(Submission submission)
    {
        ArgumentNullException.ThrowIfNull(submission);

        return new SubmissionResource(submission.Id, submission.IngredientId, submission.HipsterId, submission.SubmittedAt);
    }

    public static BrewSessionResource ToResource(BrewSession session, CoffeeBar coffeeBar)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var ingredientLookup = coffeeBar.Ingredients.ToDictionary(ingredient => ingredient.Id);

        var cycles = session.Cycles
            .OrderBy(cycle => cycle.StartedAt)
            .Select(cycle => ingredientLookup.TryGetValue(cycle.IngredientId, out var ingredient)
                ? ToResource(cycle, ingredient)
                : null)
            .Where(resource => resource is not null)
            .Select(resource => resource!)
            .ToList();

        return new BrewSessionResource(session.Id, session.StartedAt, session.EndedAt, cycles);
    }

    public static BrewCycleResource ToResource(BrewCycle cycle, Ingredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(ingredient);

        var votes = cycle.Votes
            .OrderBy(vote => vote.CastAt)
            .Select(ToResource)
            .ToList();

        return new BrewCycleResource(
            cycle.Id,
            cycle.SessionId,
            cycle.IngredientId,
            ingredient.VideoId,
            cycle.StartedAt,
            cycle.RevealedAt,
            cycle.IsActive,
            votes,
            ingredient.SubmitterIds.ToList());
    }

    public static VoteResource ToResource(Vote vote)
    {
        ArgumentNullException.ThrowIfNull(vote);

        return new VoteResource(vote.Id, vote.VoterHipsterId, vote.TargetHipsterId, vote.CastAt, vote.IsCorrect);
    }

    public static SessionStateResource ToSessionStateResource(CoffeeBar coffeeBar, BrewSession session)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);
        ArgumentNullException.ThrowIfNull(session);

        return new SessionStateResource(ToResource(coffeeBar), ToResource(session, coffeeBar));
    }

    public static RevealResultResource ToResource(RevealResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var tally = result.Tally.ToDictionary(pair => pair.Key, pair => pair.Value);
        var submitters = result.CorrectSubmitterIds.ToList();
        var guessers = result.CorrectGuessers.ToList();

        return new RevealResultResource(result.CycleId, tally, submitters, guessers);
    }
}
