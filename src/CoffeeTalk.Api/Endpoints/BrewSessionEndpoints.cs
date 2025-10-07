using CoffeeTalk.Api.Contracts;
using CoffeeTalk.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace CoffeeTalk.Api.Endpoints;

public static class BrewSessionEndpoints
{
    public static IEndpointRouteBuilder MapBrewSessionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/brew-sessions")
            .WithTags("Brew Sessions");

        group.MapGet("/", (IBrewSessionSummaryProvider provider) =>
            Results.Ok(provider.GetSummaries()))
            .WithName("GetBrewSessions")
            .WithSummary("Gets the available brew sessions.")
            .Produces<IReadOnlyList<BrewSessionSummary>>();

        group.MapGet("/{id:guid}", Results<Ok<BrewSessionSummary>, NotFound> (
            Guid id,
            IBrewSessionSummaryProvider provider) =>
        {
            var summary = provider.GetById(id);
            return summary is null ? TypedResults.NotFound() : TypedResults.Ok(summary);
        })
        .WithName("GetBrewSessionById")
        .WithSummary("Gets a single brew session by identifier.")
        .Produces<BrewSessionSummary>()
        .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
