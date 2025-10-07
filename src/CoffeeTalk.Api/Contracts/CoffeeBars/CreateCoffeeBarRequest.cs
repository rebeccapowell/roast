using CoffeeTalk.Domain.CoffeeBars;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record CreateCoffeeBarRequest(
    string Theme,
    int? DefaultMaxIngredientsPerHipster,
    SubmissionPolicy SubmissionPolicy = SubmissionPolicy.LockOnFirstBrew);
