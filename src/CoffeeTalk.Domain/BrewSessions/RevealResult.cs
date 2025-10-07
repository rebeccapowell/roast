namespace CoffeeTalk.Domain.BrewSessions;

public sealed class RevealResult
{
    internal RevealResult(
        Guid cycleId,
        IReadOnlyDictionary<Guid, int> tally,
        IReadOnlyCollection<Guid> correctSubmitterIds,
        IReadOnlyCollection<Guid> correctGuessers)
    {
        CycleId = cycleId;
        Tally = tally;
        CorrectSubmitterIds = correctSubmitterIds;
        CorrectGuessers = correctGuessers;
    }

    public Guid CycleId { get; }

    public IReadOnlyDictionary<Guid, int> Tally { get; }

    public IReadOnlyCollection<Guid> CorrectSubmitterIds { get; }

    public IReadOnlyCollection<Guid> CorrectGuessers { get; }
}
