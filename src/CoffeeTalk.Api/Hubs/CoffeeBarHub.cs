using System.Linq;
using CoffeeTalk.Api.Contracts.CoffeeBars;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace CoffeeTalk.Api.Hubs;

public interface ICoffeeBarClient
{
    Task CoffeeBarUpdated(CoffeeBarResource coffeeBar);

    Task SessionUpdated(SessionStateResource session);

    Task CycleRevealed(RevealCycleResponse response);
}

public sealed class CoffeeBarHub(ICoffeeBarRepository repository) : Hub<ICoffeeBarClient>
{
    private readonly ICoffeeBarRepository _repository = repository;

    public static string GetGroupName(string code) => $"coffee-bar:{NormalizeCode(code)}";

    public async Task JoinCoffeeBar(string code, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeCode(code);

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(normalized), cancellationToken)
            .ConfigureAwait(false);

        var coffeeBar = await _repository.GetByCodeAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (coffeeBar is null)
        {
            return;
        }

        var coffeeBarResource = CoffeeBarContractsMapper.ToResource(coffeeBar);
        await Clients.Caller.CoffeeBarUpdated(coffeeBarResource).ConfigureAwait(false);

        var latestSession = coffeeBar.Sessions
            .OrderByDescending(session => session.StartedAt)
            .FirstOrDefault();

        if (latestSession is null)
        {
            return;
        }

        var sessionResource = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, latestSession);
        await Clients.Caller.SessionUpdated(sessionResource).ConfigureAwait(false);

        var latestCycle = latestSession.Cycles
            .OrderBy(cycle => cycle.StartedAt)
            .LastOrDefault();

        if (latestCycle is null || latestCycle.IsActive)
        {
            return;
        }

        var reveal = CreateRevealResource(coffeeBar, latestCycle);
        await Clients.Caller.CycleRevealed(new RevealCycleResponse(sessionResource, reveal)).ConfigureAwait(false);
    }

    public Task LeaveCoffeeBar(string code, CancellationToken cancellationToken = default) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(code), cancellationToken);

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code.Trim().ToUpperInvariant();
    }

    private static RevealResultResource CreateRevealResource(CoffeeBar coffeeBar, BrewCycle cycle)
    {
        var ingredient = coffeeBar.Ingredients.First(ingredient => ingredient.Id == cycle.IngredientId);

        var tally = cycle.Votes
            .GroupBy(vote => vote.TargetHipsterId)
            .ToDictionary(group => group.Key, group => group.Count());

        var correctGuessers = cycle.Votes
            .Where(vote => vote.IsCorrect == true)
            .Select(vote => vote.VoterHipsterId)
            .ToList();

        return new RevealResultResource(
            cycle.Id,
            tally,
            ingredient.SubmitterIds.ToList(),
            correctGuessers);
    }
}
