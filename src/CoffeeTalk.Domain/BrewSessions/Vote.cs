namespace CoffeeTalk.Domain.BrewSessions;

public sealed class Vote
{
    internal Vote(Guid id, Guid voterHipsterId, Guid targetHipsterId, DateTimeOffset castAt)
    {
        Id = id;
        VoterHipsterId = voterHipsterId;
        TargetHipsterId = targetHipsterId;
        CastAt = castAt;
    }

    public Guid Id { get; }

    public Guid VoterHipsterId { get; }

    public Guid TargetHipsterId { get; private set; }

    public DateTimeOffset CastAt { get; }

    public bool? IsCorrect { get; private set; }

    internal void MarkCorrectness(bool isCorrect)
    {
        IsCorrect = isCorrect;
    }

    internal static Vote FromState(Guid id, Guid voterHipsterId, Guid targetHipsterId, DateTimeOffset castAt, bool? isCorrect)
    {
        var vote = new Vote(id, voterHipsterId, targetHipsterId, castAt);
        if (isCorrect is bool correctness)
        {
            vote.MarkCorrectness(correctness);
        }

        return vote;
    }
}
