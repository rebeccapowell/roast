using CoffeeTalk.Api.Contracts.CoffeeBars;
using CoffeeTalk.Api.Services;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeTalk.Api.Endpoints;

public static class CoffeeBarEndpoints
{
    private const int DefaultIngredientQuota = 5;

    public static IEndpointRouteBuilder MapCoffeeBarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/coffee-bars")
            .WithTags("Coffee Bars");

        group.MapPost("/", CreateCoffeeBarAsync)
            .WithName("CreateCoffeeBar")
            .WithSummary("Creates a new coffee bar.")
            .Produces<CoffeeBarResource>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{code}", GetCoffeeBarAsync)
            .WithName("GetCoffeeBar")
            .WithSummary("Gets a coffee bar by its code.")
            .Produces<CoffeeBarResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/hipsters", JoinCoffeeBarAsync)
            .WithName("JoinCoffeeBar")
            .WithSummary("Joins a coffee bar with a username.")
            .Produces<JoinCoffeeBarResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/ingredients", SubmitIngredientAsync)
            .WithName("SubmitIngredient")
            .WithSummary("Submits a YouTube video to a coffee bar.")
            .Produces<SubmitIngredientResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<Results<Created<CoffeeBarResource>, BadRequest<ProblemDetails>>> CreateCoffeeBarAsync(
        CreateCoffeeBarRequest request,
        ICoffeeBarCodeGenerator codeGenerator,
        ICoffeeBarRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = await codeGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);
            var quota = request.DefaultMaxIngredientsPerHipster ?? DefaultIngredientQuota;

            var coffeeBar = CoffeeBar.Create(
                Guid.NewGuid(),
                code,
                request.Theme,
                quota,
                request.SubmissionPolicy);

            await repository.AddAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var resource = CoffeeBarContractsMapper.ToResource(coffeeBar);
            return TypedResults.Created($"/coffee-bars/{resource.Code}", resource);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<CoffeeBarResource>, BadRequest<ProblemDetails>, NotFound>> GetCoffeeBarAsync(
        string code,
        ICoffeeBarRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            return TypedResults.Ok(CoffeeBarContractsMapper.ToResource(coffeeBar));
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<JoinCoffeeBarResponse>, BadRequest<ProblemDetails>, NotFound>> JoinCoffeeBarAsync(
        string code,
        JoinCoffeeBarRequest request,
        ICoffeeBarRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            var hipster = coffeeBar.AddHipster(Guid.NewGuid(), request.Username);
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var response = new JoinCoffeeBarResponse(
                CoffeeBarContractsMapper.ToResource(coffeeBar),
                CoffeeBarContractsMapper.ToResource(hipster));

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<SubmitIngredientResponse>, BadRequest<ProblemDetails>, NotFound>> SubmitIngredientAsync(
        string code,
        SubmitIngredientRequest request,
        ICoffeeBarRepository repository,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            if (!YouTubeVideoIdParser.TryParse(request.Url, out var videoId))
            {
                return TypedResults.BadRequest(CreateProblemDetails("Unable to extract a YouTube video identifier from the provided URL."));
            }

            var submission = coffeeBar.SubmitIngredient(Guid.NewGuid(), request.HipsterId, videoId, timeProvider.GetUtcNow());
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var ingredient = coffeeBar.Ingredients.First(i => i.Id == submission.IngredientId);
            var response = new SubmitIngredientResponse(
                CoffeeBarContractsMapper.ToResource(coffeeBar),
                CoffeeBarContractsMapper.ToResource(ingredient),
                submission.Id);

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static ProblemDetails CreateProblemDetails(string message) => new()
    {
        Title = "Request validation failed",
        Detail = message,
        Status = StatusCodes.Status400BadRequest
    };
}
