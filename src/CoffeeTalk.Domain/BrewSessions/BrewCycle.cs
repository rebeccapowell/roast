namespace CoffeeTalk.Domain.BrewSessions;

public sealed class BrewCycle
{
    private readonly List<Vote> _votes = new();

    internal BrewCycle(Guid id, Guid sessionId, Guid ingredientId, DateTimeOffset startedAt)
    {
        Id = id;
        SessionId = sessionId;
        IngredientId = ingredientId;
        StartedAt = startedAt;
    }

    public Guid Id { get; }

    public Guid SessionId { get; }

    public Guid IngredientId { get; }

    public DateTimeOffset StartedAt { get; }

    public DateTimeOffset? RevealedAt { get; private set; }

    public IReadOnlyCollection<Vote> Votes => _votes;

    public bool IsActive => RevealedAt is null;

    internal Vote CastVote(Guid voteId, Guid voterHipsterId, Guid targetHipsterId, DateTimeOffset castAt)
    {
        if (!IsActive)
        {
            throw new DomainException("Cannot cast a vote on a cycle that has already been revealed.");
        }

        if (voterHipsterId == targetHipsterId)
        {
            throw new DomainException("Hipsters cannot vote for themselves.");
        }

        if (_votes.Any(vote => vote.VoterHipsterId == voterHipsterId))
        {
            throw new DomainException("A hipster may cast only one vote per cycle.");
        }

        var vote = new Vote(voteId, voterHipsterId, targetHipsterId, castAt);
        _votes.Add(vote);
        return vote;
    }

    internal RevealResult Reveal(IEnumerable<Guid> submitterIds, DateTimeOffset revealedAt)
    {
        if (!IsActive)
        {
            throw new DomainException("Cycle has already been revealed.");
        }

        var submitterSet = submitterIds.ToHashSet();

        foreach (var vote in _votes)
        {
            var isCorrect = submitterSet.Contains(vote.TargetHipsterId);
            vote.MarkCorrectness(isCorrect);
        }

        RevealedAt = revealedAt;

        var tally = _votes
            .GroupBy(vote => vote.TargetHipsterId)
            .ToDictionary(group => group.Key, group => group.Count());

        var correctGuessers = _votes
            .Where(vote => vote.IsCorrect == true)
            .Select(vote => vote.VoterHipsterId)
            .ToList();

        return new RevealResult(Id, tally, submitterSet, correctGuessers);
    }

    internal static BrewCycle FromState(
        Guid id,
        Guid sessionId,
        Guid ingredientId,
        DateTimeOffset startedAt,
        IEnumerable<Vote> votes,
        DateTimeOffset? revealedAt,
        IEnumerable<Guid>? submitterIds)
    {
        ArgumentNullException.ThrowIfNull(votes);

        var cycle = new BrewCycle(id, sessionId, ingredientId, startedAt);
        cycle._votes.AddRange(votes);

        if (revealedAt is DateTimeOffset revealMoment)
        {
            ArgumentNullException.ThrowIfNull(submitterIds);
            cycle.Reveal(submitterIds, revealMoment);
        }

        return cycle;
    }
}
