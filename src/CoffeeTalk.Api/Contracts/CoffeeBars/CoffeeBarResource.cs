using System.Collections.Generic;
using CoffeeTalk.Domain.CoffeeBars;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record CoffeeBarResource(
    Guid Id,
    string Code,
    string Theme,
    int DefaultMaxIngredientsPerHipster,
    SubmissionPolicy SubmissionPolicy,
    bool SubmissionsLocked,
    bool IsClosed,
    Guid? ActiveSessionId,
    IReadOnlyList<HipsterResource> Hipsters,
    IReadOnlyList<IngredientResource> Ingredients,
    IReadOnlyList<SubmissionResource> Submissions);
