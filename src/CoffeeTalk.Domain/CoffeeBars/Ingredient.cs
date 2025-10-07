namespace CoffeeTalk.Domain.CoffeeBars;

public sealed class Ingredient
{
    private readonly HashSet<Guid> _submitterIds = new();

    internal Ingredient(Guid id, string videoId, DateTimeOffset createdAt)
    {
        Id = id;
        VideoId = videoId;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }

    public string VideoId { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsConsumed { get; private set; }

    public IReadOnlyCollection<Guid> SubmitterIds => _submitterIds;

    internal void RegisterSubmission(Guid hipsterId)
    {
        _submitterIds.Add(hipsterId);
    }

    internal void MarkConsumed()
    {
        if (IsConsumed)
        {
            throw new DomainException("Ingredient has already been consumed.");
        }

        IsConsumed = true;
    }

    internal static Ingredient FromState(
        Guid id,
        string videoId,
        DateTimeOffset createdAt,
        bool isConsumed,
        IEnumerable<Guid> submitterIds)
    {
        ArgumentNullException.ThrowIfNull(submitterIds);

        var ingredient = new Ingredient(id, videoId, createdAt);

        foreach (var hipsterId in submitterIds.Distinct())
        {
            ingredient.RegisterSubmission(hipsterId);
        }

        if (isConsumed)
        {
            ingredient.MarkConsumed();
        }

        return ingredient;
    }
}
