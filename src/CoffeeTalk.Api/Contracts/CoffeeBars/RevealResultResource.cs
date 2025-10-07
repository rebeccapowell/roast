using System;
using System.Collections.Generic;

namespace CoffeeTalk.Api.Contracts.CoffeeBars;

public sealed record RevealResultResource(
    Guid CycleId,
    IReadOnlyDictionary<Guid, int> Tally,
    IReadOnlyCollection<Guid> CorrectSubmitterIds,
    IReadOnlyCollection<Guid> CorrectGuessers);
