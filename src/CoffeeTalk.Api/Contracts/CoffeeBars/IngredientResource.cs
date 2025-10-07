using System.Collections.Generic;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record IngredientResource(
    Guid Id,
    string VideoId,
    bool IsConsumed,
    IReadOnlyList<Guid> SubmitterIds,
    string? Title,
    string? ThumbnailUrl);
