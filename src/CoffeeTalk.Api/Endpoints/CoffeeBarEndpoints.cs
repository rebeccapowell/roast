using System.Collections.Generic;
using System.Linq;
using CoffeeTalk.Api.Contracts.CoffeeBars;
using CoffeeTalk.Api.Hubs;
using CoffeeTalk.Api.Services;
using CoffeeTalk.Domain;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Domain.CoffeeBars;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

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
            .Produces<CreateCoffeeBarResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{code}", GetCoffeeBarAsync)
            .WithName("GetCoffeeBar")
            .WithSummary("Gets a coffee bar by its code.")
            .Produces<CoffeeBarResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{code}/leaderboard", GetLeaderboardAsync)
            .WithName("GetCoffeeBarLeaderboard")
            .WithSummary("Gets leaderboard standings for a coffee bar.")
            .Produces<CoffeeBarLeaderboardResource>()
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

        group.MapDelete("/{code}/submissions/{submissionId:guid}", RemoveSubmissionAsync)
            .WithName("RemoveSubmission")
            .WithSummary("Removes a submission for a hipster.")
            .Produces<CoffeeBarResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/sessions", StartSessionAsync)
            .WithName("StartSession")
            .WithSummary("Starts a brew session and the first cycle for a coffee bar.")
            .Produces<SessionStateResource>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet("/{code}/sessions/{sessionId:guid}", GetSessionAsync)
            .WithName("GetSession")
            .WithSummary("Gets the state of a brew session.")
            .Produces<SessionStateResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/sessions/{sessionId:guid}/end", EndSessionAsync)
            .WithName("EndSession")
            .WithSummary("Ends the active brew session for a coffee bar.")
            .Produces<SessionStateResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/sessions/{sessionId:guid}/cycles/{cycleId:guid}/votes", CastVoteAsync)
            .WithName("CastVote")
            .WithSummary("Casts a vote for the active brew cycle.")
            .Produces<SessionStateResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/sessions/{sessionId:guid}/cycles", StartNextCycleAsync)
            .WithName("StartNextCycle")
            .WithSummary("Starts the next brew cycle for a session.")
            .Produces<SessionStateResource>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost("/{code}/sessions/{sessionId:guid}/cycles/{cycleId:guid}/reveal", RevealCycleAsync)
            .WithName("RevealCycle")
            .WithSummary("Reveals the results for a brew cycle and closes voting.")
            .Produces<RevealCycleResponse>()
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static CoffeeBarLeaderboardResource BuildLeaderboard(CoffeeBar coffeeBar)
    {
        ArgumentNullException.ThrowIfNull(coffeeBar);

        var hipsters = coffeeBar.Hipsters
            .OrderBy(hipster => hipster.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var overallScores = hipsters.ToDictionary(hipster => hipster.Id, _ => 0);
        var sessions = coffeeBar.Sessions
            .OrderBy(session => session.StartedAt)
            .ToList();

        var sessionResources = new List<SessionLeaderboardResource>(sessions.Count);

        foreach (var session in sessions)
        {
            var sessionScores = hipsters.ToDictionary(hipster => hipster.Id, _ => 0);
            var cycles = session.Cycles
                .OrderBy(cycle => cycle.RevealedAt ?? cycle.StartedAt)
                .ThenBy(cycle => cycle.StartedAt)
                .ToList();

            foreach (var cycle in cycles)
            {
                foreach (var vote in cycle.Votes)
                {
                    if (vote.IsCorrect == true)
                    {
                        IncrementScore(sessionScores, vote.VoterHipsterId);
                        IncrementScore(overallScores, vote.VoterHipsterId);
                    }
                }
            }

            var sessionEntries = CreateLeaderboardEntries(sessionScores, hipsters);
            sessionResources.Add(new SessionLeaderboardResource(
                session.Id,
                session.StartedAt,
                session.EndedAt,
                sessionEntries));
        }

        var previousRanks = CalculatePreviousRanks(hipsters, sessions);
        var overallEntries = CreateLeaderboardEntries(overallScores, hipsters, previousRanks);

        return new CoffeeBarLeaderboardResource(overallEntries, sessionResources);
    }

    private static IReadOnlyList<LeaderboardEntryResource> CreateLeaderboardEntries(
        IReadOnlyDictionary<Guid, int> scores,
        IReadOnlyList<Hipster> hipsters,
        IReadOnlyDictionary<Guid, int?>? previousRanks = null)
    {
        ArgumentNullException.ThrowIfNull(scores);
        ArgumentNullException.ThrowIfNull(hipsters);

        var ordered = hipsters
            .Select(hipster =>
            {
                scores.TryGetValue(hipster.Id, out var score);
                return new
                {
                    hipster.Id,
                    hipster.Username,
                    Score = score
                };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entries = new List<LeaderboardEntryResource>(ordered.Count);
        var currentRank = 0;
        int? lastScore = null;

        foreach (var item in ordered)
        {
            if (lastScore is null || item.Score != lastScore)
            {
                currentRank++;
                lastScore = item.Score;
            }

            int? previousRank = null;
            if (previousRanks is not null && previousRanks.TryGetValue(item.Id, out var priorRank))
            {
                previousRank = priorRank;
            }

            var trend = CalculateTrend(currentRank, previousRank);
            entries.Add(new LeaderboardEntryResource(item.Id, item.Username, item.Score, currentRank, previousRank, trend));
        }

        return entries;
    }

    private static IReadOnlyDictionary<Guid, int?> CalculatePreviousRanks(
        IReadOnlyList<Hipster> hipsters,
        IEnumerable<BrewSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(hipsters);
        ArgumentNullException.ThrowIfNull(sessions);

        var cumulativeScores = hipsters.ToDictionary(hipster => hipster.Id, _ => 0);
        Dictionary<Guid, int>? lastRanks = null;
        Dictionary<Guid, int>? previousRanks = null;

        var revealedCycles = sessions
            .SelectMany(session => session.Cycles)
            .Where(cycle => cycle.RevealedAt is not null)
            .OrderBy(cycle => cycle.RevealedAt ?? cycle.StartedAt)
            .ThenBy(cycle => cycle.StartedAt)
            .ToList();

        foreach (var cycle in revealedCycles)
        {
            foreach (var vote in cycle.Votes)
            {
                if (vote.IsCorrect == true)
                {
                    IncrementScore(cumulativeScores, vote.VoterHipsterId);
                }
            }

            previousRanks = lastRanks;
            lastRanks = CalculateRankDictionary(cumulativeScores, hipsters);
        }

        if (previousRanks is null)
        {
            return hipsters.ToDictionary(hipster => hipster.Id, _ => (int?)null);
        }

        var result = new Dictionary<Guid, int?>(hipsters.Count);
        foreach (var hipster in hipsters)
        {
            previousRanks.TryGetValue(hipster.Id, out var rank);
            result[hipster.Id] = rank;
        }

        return result;
    }

    private static Dictionary<Guid, int> CalculateRankDictionary(
        IReadOnlyDictionary<Guid, int> scores,
        IReadOnlyList<Hipster> hipsters)
    {
        var ordered = hipsters
            .Select(hipster =>
            {
                scores.TryGetValue(hipster.Id, out var score);
                return new
                {
                    hipster.Id,
                    hipster.Username,
                    Score = score
                };
            })
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ranks = new Dictionary<Guid, int>(ordered.Count);
        var currentRank = 0;
        int? lastScore = null;

        foreach (var item in ordered)
        {
            if (lastScore is null || item.Score != lastScore)
            {
                currentRank++;
                lastScore = item.Score;
            }

            ranks[item.Id] = currentRank;
        }

        return ranks;
    }

    private static LeaderboardTrend CalculateTrend(int currentRank, int? previousRank)
    {
        if (previousRank is null)
        {
            return LeaderboardTrend.Stable;
        }

        if (currentRank < previousRank.Value)
        {
            return LeaderboardTrend.Up;
        }

        if (currentRank > previousRank.Value)
        {
            return LeaderboardTrend.Down;
        }

        return LeaderboardTrend.Stable;
    }

    private static void IncrementScore(IDictionary<Guid, int> scores, Guid hipsterId)
    {
        if (hipsterId == Guid.Empty)
        {
            return;
        }

        if (scores.TryGetValue(hipsterId, out var current))
        {
            scores[hipsterId] = current + 1;
        }
        else
        {
            scores[hipsterId] = 1;
        }
    }

    private static async Task<Results<Ok<CoffeeBarLeaderboardResource>, BadRequest<ProblemDetails>, NotFound>> GetLeaderboardAsync(
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

            var leaderboard = BuildLeaderboard(coffeeBar);
            return TypedResults.Ok(leaderboard);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Created<CreateCoffeeBarResponse>, BadRequest<ProblemDetails>>> CreateCoffeeBarAsync(
        CreateCoffeeBarRequest request,
        ICoffeeBarCodeGenerator codeGenerator,
        ICoffeeBarRepository repository,
        CancellationToken cancellationToken)
    {
        try
        {
            var code = await codeGenerator.GenerateAsync(cancellationToken).ConfigureAwait(false);
            var quota = request.DefaultMaxIngredientsPerHipster ?? DefaultIngredientQuota;

            if (string.IsNullOrWhiteSpace(request.CreatorUsername))
            {
                throw new DomainException("Creator username is required.");
            }

            var coffeeBar = CoffeeBar.Create(
                Guid.NewGuid(),
                code,
                request.Theme,
                quota,
                request.SubmissionPolicy);

            var hipster = coffeeBar.AddHipster(Guid.NewGuid(), request.CreatorUsername);

            await repository.AddAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var response = new CreateCoffeeBarResponse(
                CoffeeBarContractsMapper.ToResource(coffeeBar),
                CoffeeBarContractsMapper.ToResource(hipster));

            return TypedResults.Created($"/coffee-bars/{response.CoffeeBar.Code}", response);
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
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
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

            var coffeeBarResource = CoffeeBarContractsMapper.ToResource(coffeeBar);
            var response = new JoinCoffeeBarResponse(
                coffeeBarResource,
                CoffeeBarContractsMapper.ToResource(hipster));

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(coffeeBarResource.Code))
                .CoffeeBarUpdated(coffeeBarResource)
                .ConfigureAwait(false);

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<CoffeeBarResource>, BadRequest<ProblemDetails>, NotFound>> RemoveSubmissionAsync(
        string code,
        Guid submissionId,
        [FromQuery] Guid hipsterId,
        ICoffeeBarRepository repository,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (hipsterId == Guid.Empty)
            {
                throw new DomainException("Hipster identifier is required.");
            }

            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            coffeeBar.RemoveSubmission(hipsterId, submissionId);
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var coffeeBarResource = CoffeeBarContractsMapper.ToResource(coffeeBar);

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(coffeeBarResource.Code))
                .CoffeeBarUpdated(coffeeBarResource)
                .ConfigureAwait(false);

            return TypedResults.Ok(coffeeBarResource);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Created<SessionStateResource>, BadRequest<ProblemDetails>, NotFound>> StartSessionAsync(
        string code,
        ICoffeeBarRepository repository,
        INextIngredientSelector ingredientSelector,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            var session = coffeeBar.StartSession(Guid.NewGuid(), timeProvider.GetUtcNow());
            coffeeBar.StartNextCycle(session.Id, Guid.NewGuid(), timeProvider.GetUtcNow(), ingredientSelector);

            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var response = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session);
            var group = CoffeeBarHub.GetGroupName(response.CoffeeBar.Code);

            await hubContext
                .Clients
                .Group(group)
                .CoffeeBarUpdated(response.CoffeeBar)
                .ConfigureAwait(false);

            await hubContext
                .Clients
                .Group(group)
                .SessionUpdated(response)
                .ConfigureAwait(false);

            return TypedResults.Created($"/coffee-bars/{code}/sessions/{session.Id}", response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<SessionStateResource>, BadRequest<ProblemDetails>, NotFound>> GetSessionAsync(
        string code,
        Guid sessionId,
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

            var session = coffeeBar.Sessions.FirstOrDefault(session => session.Id == sessionId);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            var response = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session);
            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<SessionStateResource>, BadRequest<ProblemDetails>, NotFound>> EndSessionAsync(
        string code,
        Guid sessionId,
        ICoffeeBarRepository repository,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            var session = coffeeBar.EndSession(sessionId, timeProvider.GetUtcNow());
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var response = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session);

            var group = CoffeeBarHub.GetGroupName(response.CoffeeBar.Code);
            await hubContext
                .Clients
                .Group(group)
                .CoffeeBarUpdated(response.CoffeeBar)
                .ConfigureAwait(false);

            await hubContext
                .Clients
                .Group(group)
                .SessionUpdated(response)
                .ConfigureAwait(false);

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<SessionStateResource>, BadRequest<ProblemDetails>, NotFound>> CastVoteAsync(
        string code,
        Guid sessionId,
        Guid cycleId,
        CastVoteRequest request,
        ICoffeeBarRepository repository,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            coffeeBar.CastVote(cycleId, Guid.NewGuid(), request.VoterHipsterId, request.TargetHipsterId, timeProvider.GetUtcNow());
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var session = coffeeBar.Sessions.FirstOrDefault(session => session.Id == sessionId);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            var response = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session);

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(response.CoffeeBar.Code))
                .SessionUpdated(response)
                .ConfigureAwait(false);

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<SessionStateResource>, BadRequest<ProblemDetails>, NotFound>> StartNextCycleAsync(
        string code,
        Guid sessionId,
        StartNextCycleRequest request,
        ICoffeeBarRepository repository,
        INextIngredientSelector ingredientSelector,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.HipsterId == Guid.Empty)
            {
                throw new DomainException("Hipster identifier is required.");
            }

            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            if (coffeeBar.Hipsters.All(hipster => hipster.Id != request.HipsterId))
            {
                throw new DomainException("Hipster must be part of the coffee bar.");
            }

            coffeeBar.StartNextCycle(sessionId, Guid.NewGuid(), timeProvider.GetUtcNow(), ingredientSelector);
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var session = coffeeBar.Sessions.FirstOrDefault(session => session.Id == sessionId);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            var response = CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session);

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(response.CoffeeBar.Code))
                .SessionUpdated(response)
                .ConfigureAwait(false);

            return TypedResults.Ok(response);
        }
        catch (DomainException ex)
        {
            return TypedResults.BadRequest(CreateProblemDetails(ex.Message));
        }
    }

    private static async Task<Results<Ok<RevealCycleResponse>, BadRequest<ProblemDetails>, NotFound>> RevealCycleAsync(
        string code,
        Guid sessionId,
        Guid cycleId,
        RevealCycleRequest request,
        ICoffeeBarRepository repository,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.HipsterId == Guid.Empty)
            {
                throw new DomainException("Hipster identifier is required.");
            }

            var coffeeBar = await repository.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (coffeeBar is null)
            {
                return TypedResults.NotFound();
            }

            if (coffeeBar.Hipsters.All(hipster => hipster.Id != request.HipsterId))
            {
                throw new DomainException("Hipster must be part of the coffee bar.");
            }

            var result = coffeeBar.Reveal(cycleId, timeProvider.GetUtcNow());
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var session = coffeeBar.Sessions.FirstOrDefault(session => session.Id == sessionId);
            if (session is null)
            {
                return TypedResults.NotFound();
            }

            var response = new RevealCycleResponse(
                CoffeeBarContractsMapper.ToSessionStateResource(coffeeBar, session),
                CoffeeBarContractsMapper.ToResource(result));

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(response.Session.CoffeeBar.Code))
                .CycleRevealed(response)
                .ConfigureAwait(false);

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
        IYouTubeMetadataProvider metadataProvider,
        TimeProvider timeProvider,
        IHubContext<CoffeeBarHub, ICoffeeBarClient> hubContext,
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

            var metadata = await metadataProvider
                .TryGetMetadataAsync(videoId, cancellationToken)
                .ConfigureAwait(false);

            var submission = coffeeBar.SubmitIngredient(
                Guid.NewGuid(),
                request.HipsterId,
                videoId,
                timeProvider.GetUtcNow(),
                metadata?.Title,
                metadata?.ThumbnailUrl);
            await repository.UpdateAsync(coffeeBar, cancellationToken).ConfigureAwait(false);

            var ingredient = coffeeBar.Ingredients.First(i => i.Id == submission.IngredientId);
            var coffeeBarResource = CoffeeBarContractsMapper.ToResource(coffeeBar);
            var response = new SubmitIngredientResponse(
                coffeeBarResource,
                CoffeeBarContractsMapper.ToResource(ingredient),
                submission.Id);

            await hubContext
                .Clients
                .Group(CoffeeBarHub.GetGroupName(coffeeBarResource.Code))
                .CoffeeBarUpdated(coffeeBarResource)
                .ConfigureAwait(false);

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
