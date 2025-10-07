using CoffeeTalk.Api.Contracts;

namespace CoffeeTalk.Api.Services;

public interface IBrewSessionSummaryProvider
{
    IReadOnlyList<BrewSessionSummary> GetSummaries();
    BrewSessionSummary? GetById(Guid id);
}
