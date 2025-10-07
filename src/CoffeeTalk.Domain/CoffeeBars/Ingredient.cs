using System;

namespace CoffeeTalk.Domain.CoffeeBars;

public sealed class Ingredient
{
    private readonly HashSet<Guid> _submitterIds = new();

    internal Ingredient(Guid id, string videoId, DateTimeOffset createdAt, string? title = null, string? thumbnailUrl = null)
    {
        Id = id;
        VideoId = videoId;
        CreatedAt = createdAt;
        ApplyMetadata(title, thumbnailUrl);
    }

    public Guid Id { get; }

    public string VideoId { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsConsumed { get; private set; }

    public string? Title { get; private set; }

    public string? ThumbnailUrl { get; private set; }

    public IReadOnlyCollection<Guid> SubmitterIds => _submitterIds;

    internal void RegisterSubmission(Guid hipsterId)
    {
        _submitterIds.Add(hipsterId);
    }

    internal void ApplyMetadata(string? title, string? thumbnailUrl)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            Title = title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(thumbnailUrl) && Uri.TryCreate(thumbnailUrl.Trim(), UriKind.Absolute, out var uri))
        {
            if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                || uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
            {
                ThumbnailUrl = uri.ToString();
            }
        }
    }

    internal void RemoveSubmission(Guid hipsterId)
    {
        if (!_submitterIds.Remove(hipsterId))
        {
            throw new DomainException("Hipster has not submitted this ingredient.");
        }
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
        IEnumerable<Guid> submitterIds,
        string? title,
        string? thumbnailUrl)
    {
        ArgumentNullException.ThrowIfNull(submitterIds);

        var ingredient = new Ingredient(id, videoId, createdAt, title, thumbnailUrl);

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
