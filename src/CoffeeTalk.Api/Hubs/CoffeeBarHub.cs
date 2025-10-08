using CoffeeTalk.Api.Contracts.CoffeeBars;
using CoffeeTalk.Domain;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Domain.CoffeeBars;
using Microsoft.AspNetCore.SignalR;

namespace CoffeeTalk.Api.Hubs;

public interface ICoffeeBarClient
{
    Task CoffeeBarUpdated(CoffeeBarResource coffeeBar);

    Task SessionUpdated(SessionStateResource session);

    Task CycleRevealed(RevealCycleResponse response);
}

public sealed class CoffeeBarHub(ICoffeeBarRepository repository, ILogger<CoffeeBarHub> logger) : Hub<ICoffeeBarClient>
{
    private readonly ICoffeeBarRepository _repository = repository;
    private readonly ILogger<CoffeeBarHub> _logger = logger;

    public static string GetGroupName(string code) => $"coffee-bar:{NormalizeCode(code)}";

    public async Task JoinCoffeeBar(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            _logger.LogWarning(
                "Connection {ConnectionId} attempted to join a coffee bar without providing a code.",
                Context.ConnectionId);
            return;
        }

        var normalized = NormalizeCode(code);

        CoffeeBar? coffeeBar;

        try
        {
            coffeeBar = await _repository.GetByCodeAsync(normalized).ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain error while connection {ConnectionId} joined coffee bar {Code}.", Context.ConnectionId, normalized);
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while loading coffee bar {Code} for connection {ConnectionId}.",
                normalized,
                Context.ConnectionId);
            return;
        }

        if (coffeeBar is null)
        {
            return;
        }

        try
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(normalized))
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to add connection {ConnectionId} to coffee bar group {Code}.",
                Context.ConnectionId,
                normalized);
            return;
        }

        try
        {
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

            var reveal = TryCreateRevealResource(coffeeBar, latestCycle);
            if (reveal is null)
            {
                return;
            }

            await Clients.Caller.CycleRevealed(new RevealCycleResponse(sessionResource, reveal)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to send coffee bar state to connection {ConnectionId} after joining coffee bar {Code}.",
                Context.ConnectionId,
                normalized);
        }
    }

    public Task LeaveCoffeeBar(string code) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(code));

    private static string NormalizeCode(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        return code.Trim().ToUpperInvariant();
    }

    private static RevealResultResource? TryCreateRevealResource(CoffeeBar coffeeBar, BrewCycle cycle)
    {
        var ingredient = coffeeBar.Ingredients.FirstOrDefault(ingredient => ingredient.Id == cycle.IngredientId);

        if (ingredient is null)
        {
            return null;
        }

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
