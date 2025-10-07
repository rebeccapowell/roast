using System.Collections.Generic;
using System.Linq;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Entities;

namespace CoffeeTalk.Infrastructure.Data.Mappings;

public static class CoffeeBarMapper
{
    public static CoffeeBarEntity ToEntity(CoffeeBar coffeeBar)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var entity = new CoffeeBarEntity
        {
            Id = coffeeBar.Id,
            Code = coffeeBar.Code.Value,
            Theme = coffeeBar.Theme,
            DefaultMaxIngredientsPerHipster = coffeeBar.DefaultMaxIngredientsPerHipster,
            SubmissionPolicy = coffeeBar.SubmissionPolicy,
            SubmissionsLocked = coffeeBar.SubmissionsLocked
        };

        entity.Hipsters = coffeeBar.Hipsters
            .Select(hipster => HipsterMapper.ToEntity(coffeeBar.Id, hipster))
            .ToList();

        entity.Ingredients = coffeeBar.Ingredients
            .Select(ingredient => IngredientMapper.ToEntity(coffeeBar.Id, ingredient))
            .ToList();

        entity.Submissions = coffeeBar.Submissions
            .Select(submission => SubmissionMapper.ToEntity(coffeeBar.Id, submission))
            .ToList();

        entity.Sessions = coffeeBar.Sessions
            .Select(BrewSessionMapper.ToEntity)
            .ToList();

        return entity;
    }

    public static CoffeeBar ToDomain(CoffeeBarEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        var hipsters = entity.Hipsters
            .Select(HipsterMapper.ToDomain)
            .ToList();

        var submissions = entity.Submissions
            .Select(SubmissionMapper.ToDomain)
            .ToList();

        var submitterLookup = submissions
            .GroupBy(submission => submission.IngredientId)
            .ToDictionary(group => group.Key, group => group.Select(submission => submission.HipsterId).ToList());

        var ingredients = entity.Ingredients
            .Select(ingredient =>
            {
                submitterLookup.TryGetValue(ingredient.Id, out var submitterIds);
                return IngredientMapper.ToDomain(ingredient, submitterIds ?? Enumerable.Empty<Guid>());
            })
            .ToList();

        var submittersByIngredient = ingredients
            .ToDictionary(ingredient => ingredient.Id, ingredient => (IEnumerable<Guid>)ingredient.SubmitterIds);

        var sessions = entity.Sessions
            .Select(session =>
            {
                var cycles = session.Cycles
                    .Select(cycle =>
                    {
                        var votes = cycle.Votes
                            .Select(VoteMapper.ToDomain)
                            .ToList();
                        submittersByIngredient.TryGetValue(cycle.IngredientId, out var submitterIds);
                        return BrewCycleMapper.ToDomain(cycle, votes, submitterIds ?? Enumerable.Empty<Guid>());
                    })
                    .ToList();

                return BrewSessionMapper.ToDomain(session, cycles);
            })
            .ToList();

        return CoffeeBar.FromState(
            entity.Id,
            entity.Code,
            entity.Theme,
            entity.DefaultMaxIngredientsPerHipster,
            entity.SubmissionPolicy,
            entity.SubmissionsLocked,
            hipsters,
            ingredients,
            submissions,
            sessions);
    }
}
