using CoffeeTalk.Api.Contracts;

namespace CoffeeTalk.Api.Services;

public sealed class InMemoryBrewSessionSummaryProvider : IBrewSessionSummaryProvider
{
    private static readonly IReadOnlyList<BrewSessionSummary> Sessions =
    [
        new BrewSessionSummary(
            Guid.Parse("7a4f0a23-5274-4a36-b3a2-33f72491f062"),
            "Latte Art Throwdown",
            new DateTimeOffset(2024, 10, 15, 18, 30, 0, TimeSpan.Zero)),
        new BrewSessionSummary(
            Guid.Parse("8f0af2fd-a824-4e6f-8a5f-97a84246929f"),
            "Single Origin Showdown",
            new DateTimeOffset(2024, 11, 5, 15, 0, 0, TimeSpan.Zero))
    ];

    public IReadOnlyList<BrewSessionSummary> GetSummaries() => Sessions;

    public BrewSessionSummary? GetById(Guid id) => Sessions.FirstOrDefault(summary => summary.Id == id);
}
