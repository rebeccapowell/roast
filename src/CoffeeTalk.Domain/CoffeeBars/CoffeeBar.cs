using CoffeeTalk.Domain.BrewSessions;

namespace CoffeeTalk.Domain.CoffeeBars;

public sealed class CoffeeBar
{
    private readonly List<Hipster> _hipsters = new();
    private readonly List<Ingredient> _ingredients = new();
    private readonly List<Submission> _submissions = new();
    private readonly List<BrewSession> _sessions = new();
    private bool _submissionsLocked;

    private CoffeeBar(
        Guid id,
        CoffeeBarCode code,
        string theme,
        int defaultMaxIngredientsPerHipster,
        SubmissionPolicy submissionPolicy)
    {
        Id = id;
        Code = code;
        Theme = theme;
        DefaultMaxIngredientsPerHipster = defaultMaxIngredientsPerHipster;
        SubmissionPolicy = submissionPolicy;
    }

    public Guid Id { get; }

    public CoffeeBarCode Code { get; }

    public string Theme { get; private set; }

    public int DefaultMaxIngredientsPerHipster { get; private set; }

    public SubmissionPolicy SubmissionPolicy { get; private set; }

    public bool SubmissionsLocked => _submissionsLocked;

    public bool IsClosed => _ingredients.All(ingredient => ingredient.IsConsumed);

    public IReadOnlyCollection<Hipster> Hipsters => _hipsters.AsReadOnly();

    public IReadOnlyCollection<Ingredient> Ingredients => _ingredients.AsReadOnly();

    public IReadOnlyCollection<Submission> Submissions => _submissions.AsReadOnly();

    public IReadOnlyCollection<BrewSession> Sessions => _sessions.AsReadOnly();

    public static CoffeeBar Create(
        Guid id,
        string code,
        string theme,
        int defaultMaxIngredientsPerHipster,
        SubmissionPolicy submissionPolicy)
    {
        if (id == Guid.Empty)
        {
            throw new DomainException("Coffee bar identifier cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(theme))
        {
            throw new DomainException("Coffee bar theme is required.");
        }

        if (defaultMaxIngredientsPerHipster < 1)
        {
            throw new DomainException("Default ingredient quota must be at least one.");
        }

        var barCode = CoffeeBarCode.From(code);
        return new CoffeeBar(id, barCode, theme.Trim(), defaultMaxIngredientsPerHipster, submissionPolicy);
    }

    public Hipster AddHipster(Guid hipsterId, string username)
    {
        if (hipsterId == Guid.Empty)
        {
            throw new DomainException("Hipster identifier cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new DomainException("Hipster username is required.");
        }

        var trimmed = username.Trim();

        if (trimmed.Length is < 3 or > 20)
        {
            throw new DomainException("Hipster username must be between 3 and 20 characters long.");
        }

        var normalized = trimmed.ToUpperInvariant();

        if (_hipsters.Any(hipster => hipster.NormalizedUsername == normalized))
        {
            throw new DomainException("A hipster with the same username already exists in this coffee bar.");
        }

        var hipster = new Hipster(hipsterId, trimmed, normalized, DefaultMaxIngredientsPerHipster);
        _hipsters.Add(hipster);
        return hipster;
    }

    public Submission SubmitIngredient(
        Guid submissionId,
        Guid hipsterId,
        string videoId,
        DateTimeOffset submittedAt,
        string? title = null,
        string? thumbnailUrl = null)
    {
        if (_submissionsLocked)
        {
            throw new DomainException("Submissions are currently locked for this coffee bar.");
        }

        if (submissionId == Guid.Empty)
        {
            throw new DomainException("Submission identifier cannot be empty.");
        }

        var hipster = _hipsters.FirstOrDefault(h => h.Id == hipsterId)
            ?? throw new DomainException("Hipster must join the coffee bar before submitting ingredients.");

        if (string.IsNullOrWhiteSpace(videoId))
        {
            throw new DomainException("Video identifier is required.");
        }

        var normalizedVideoId = videoId.Trim();

        var hipsterSubmissionCount = _submissions.Count(submission => submission.HipsterId == hipsterId);
        if (hipsterSubmissionCount >= hipster.MaxIngredientQuota)
        {
            throw new DomainException("Hipster has reached the submission quota for this coffee bar.");
        }

        var ingredient = _ingredients.FirstOrDefault(i => string.Equals(i.VideoId, normalizedVideoId, StringComparison.Ordinal))
                        ?? CreateIngredient(normalizedVideoId, submittedAt, title, thumbnailUrl);

        ingredient.ApplyMetadata(title, thumbnailUrl);

        ingredient.RegisterSubmission(hipsterId);

        var submission = new Submission(submissionId, ingredient.Id, hipsterId, submittedAt);
        _submissions.Add(submission);
        return submission;
    }

    public BrewSession StartSession(Guid sessionId, DateTimeOffset startedAt)
    {
        if (sessionId == Guid.Empty)
        {
            throw new DomainException("Session identifier cannot be empty.");
        }

        if (!_ingredients.Any())
        {
            throw new DomainException("Cannot start a session without any ingredients.");
        }

        if (IsClosed)
        {
            throw new DomainException("Coffee bar is closed because all ingredients have been consumed.");
        }

        if (_sessions.Any(session => session.IsActive))
        {
            throw new DomainException("A session is already active for this coffee bar.");
        }

        if (_sessions.Any(session => session.Id == sessionId))
        {
            throw new DomainException("A session with the same identifier already exists.");
        }

        var session = new BrewSession(sessionId, Id, startedAt);
        _sessions.Add(session);

        if (SubmissionPolicy == SubmissionPolicy.LockOnFirstBrew)
        {
            _submissionsLocked = true;
        }

        return session;
    }

    public BrewCycle StartNextCycle(
        Guid sessionId,
        Guid cycleId,
        DateTimeOffset startedAt,
        INextIngredientSelector ingredientSelector)
    {
        ArgumentNullException.ThrowIfNull(ingredientSelector);

        var session = GetSession(sessionId);

        var availableIngredients = _ingredients
            .Where(ingredient => !ingredient.IsConsumed)
            .ToList();

        if (!availableIngredients.Any())
        {
            throw new DomainException("No remaining ingredients are available for the next cycle.");
        }

        var selected = ingredientSelector.PickNext(availableIngredients)
            ?? throw new DomainException("Ingredient selector did not return a candidate.");

        if (selected.IsConsumed)
        {
            throw new DomainException("Selected ingredient has already been consumed.");
        }

        selected.MarkConsumed();

        var cycle = session.StartCycle(cycleId, selected.Id, startedAt);
        return cycle;
    }

    public BrewSession EndSession(Guid sessionId, DateTimeOffset endedAt)
    {
        if (sessionId == Guid.Empty)
        {
            throw new DomainException("Session identifier cannot be empty.");
        }

        var session = GetSession(sessionId);
        session.End(endedAt);

        if (SubmissionPolicy == SubmissionPolicy.LockOnFirstBrew)
        {
            _submissionsLocked = false;
        }

        return session;
    }

    public void RemoveSubmission(Guid hipsterId, Guid submissionId)
    {
        if (hipsterId == Guid.Empty)
        {
            throw new DomainException("Hipster identifier cannot be empty.");
        }

        if (submissionId == Guid.Empty)
        {
            throw new DomainException("Submission identifier cannot be empty.");
        }

        if (_hipsters.All(hipster => hipster.Id != hipsterId))
        {
            throw new DomainException("Hipster must be part of the coffee bar.");
        }

        var submission = _submissions.FirstOrDefault(submission => submission.Id == submissionId)
            ?? throw new DomainException("Submission was not found.");

        if (submission.HipsterId != hipsterId)
        {
            throw new DomainException("Hipster can only remove their own submissions.");
        }

        var ingredient = _ingredients.FirstOrDefault(ingredient => ingredient.Id == submission.IngredientId)
            ?? throw new DomainException("Ingredient was not found.");

        if (ingredient.IsConsumed)
        {
            throw new DomainException("Cannot remove an ingredient that has already been consumed.");
        }

        ingredient.RemoveSubmission(hipsterId);
        _submissions.Remove(submission);

        if (!ingredient.SubmitterIds.Any())
        {
            _ingredients.Remove(ingredient);
        }
    }

    public Vote CastVote(Guid cycleId, Guid voteId, Guid voterHipsterId, Guid targetHipsterId, DateTimeOffset castAt)
    {
        if (!_hipsters.Any(h => h.Id == voterHipsterId))
        {
            throw new DomainException("Voter must be part of the coffee bar.");
        }

        if (!_hipsters.Any(h => h.Id == targetHipsterId))
        {
            throw new DomainException("Target hipster must be part of the coffee bar.");
        }

        var cycle = GetCycle(cycleId);
        return cycle.CastVote(voteId, voterHipsterId, targetHipsterId, castAt);
    }

    public RevealResult Reveal(Guid cycleId, DateTimeOffset revealedAt)
    {
        var cycle = GetCycle(cycleId);
        var ingredient = _ingredients.First(i => i.Id == cycle.IngredientId);
        var submitterIds = ingredient.SubmitterIds;
        return cycle.Reveal(submitterIds, revealedAt);
    }

    private Ingredient CreateIngredient(string videoId, DateTimeOffset createdAt, string? title, string? thumbnailUrl)
    {
        var ingredient = new Ingredient(Guid.NewGuid(), videoId, createdAt, title, thumbnailUrl);
        _ingredients.Add(ingredient);
        return ingredient;
    }

    private BrewSession GetSession(Guid sessionId)
    {
        var session = _sessions.FirstOrDefault(session => session.Id == sessionId);
        return session ?? throw new DomainException("Session was not found.");
    }

    private BrewCycle GetCycle(Guid cycleId)
    {
        foreach (var session in _sessions)
        {
            if (session.Cycles.FirstOrDefault(cycle => cycle.Id == cycleId) is { } cycle)
            {
                return cycle;
            }
        }

        throw new DomainException("Brew cycle was not found.");
    }

    internal static CoffeeBar FromState(
        Guid id,
        string code,
        string theme,
        int defaultMaxIngredientsPerHipster,
        SubmissionPolicy submissionPolicy,
        bool submissionsLocked,
        IEnumerable<Hipster> hipsters,
        IEnumerable<Ingredient> ingredients,
        IEnumerable<Submission> submissions,
        IEnumerable<BrewSession> sessions)
    {
        ArgumentNullException.ThrowIfNull(hipsters);
        ArgumentNullException.ThrowIfNull(ingredients);
        ArgumentNullException.ThrowIfNull(submissions);
        ArgumentNullException.ThrowIfNull(sessions);

        var coffeeBar = new CoffeeBar(
            id,
            CoffeeBarCode.From(code),
            theme,
            defaultMaxIngredientsPerHipster,
            submissionPolicy);

        coffeeBar._submissionsLocked = submissionsLocked;
        coffeeBar._hipsters.AddRange(hipsters);
        coffeeBar._ingredients.AddRange(ingredients);
        coffeeBar._submissions.AddRange(submissions);
        coffeeBar._sessions.AddRange(sessions);

        return coffeeBar;
    }
}
