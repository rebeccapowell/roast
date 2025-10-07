using CoffeeTalk.Domain;
using CoffeeTalk.Domain.BrewSessions;
using CoffeeTalk.Domain.CoffeeBars;
using Shouldly;

namespace CoffeeTalk.Domain.Tests.CoffeeBars;

public class CoffeeBarTests
{
    private static CoffeeBar CreateBar(SubmissionPolicy policy = SubmissionPolicy.LockOnFirstBrew)
    {
        return CoffeeBar.Create(Guid.NewGuid(), "BCDF12", "Indie Vibes", 5, policy);
    }

    [Fact]
    public void AddHipster_Throws_WhenUsernameAlreadyExistsRegardlessOfCase()
    {
        var bar = CreateBar();
        bar.AddHipster(Guid.NewGuid(), "MochaMike");

        var act = () => bar.AddHipster(Guid.NewGuid(), "mochaMike");

        var exception = Should.Throw<DomainException>(act);
        exception.Message.ShouldContain("already exists");
    }

    [Fact]
    public void SubmitIngredient_Throws_WhenHipsterExceedsQuota()
    {
        var bar = CreateBar();
        var hipster = bar.AddHipster(Guid.NewGuid(), "LatteLiz");

        foreach (var index in Enumerable.Range(0, bar.DefaultMaxIngredientsPerHipster))
        {
            bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, $"video-{index}", DateTimeOffset.UtcNow);
        }

        var act = () => bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "another-video", DateTimeOffset.UtcNow);

        var exception = Should.Throw<DomainException>(act);
        exception.Message.ShouldContain("quota");
    }

    [Fact]
    public void SubmitIngredient_ReusesIngredient_WhenVideoIdAlreadySubmitted()
    {
        var bar = CreateBar(SubmissionPolicy.AlwaysOpen);
        var liz = bar.AddHipster(Guid.NewGuid(), "LatteLiz");
        var joe = bar.AddHipster(Guid.NewGuid(), "EspressoJoe");

        var firstSubmission = bar.SubmitIngredient(Guid.NewGuid(), liz.Id, "yt-123", DateTimeOffset.UtcNow);
        var secondSubmission = bar.SubmitIngredient(Guid.NewGuid(), joe.Id, "yt-123", DateTimeOffset.UtcNow.AddMinutes(1));

        firstSubmission.IngredientId.ShouldBe(secondSubmission.IngredientId);

        var ingredient = bar.Ingredients.ShouldHaveSingleItem();
        ingredient.SubmitterIds.ShouldContain(liz.Id);
        ingredient.SubmitterIds.ShouldContain(joe.Id);
        ingredient.SubmitterIds.Count.ShouldBe(2);
    }

    [Fact]
    public void StartSession_LocksSubmissions_WhenPolicyIsLockOnFirstBrew()
    {
        var bar = CreateBar(SubmissionPolicy.LockOnFirstBrew);
        var hipster = bar.AddHipster(Guid.NewGuid(), "FilterFran");
        bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "yt-123", DateTimeOffset.UtcNow);

        bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);

        bar.SubmissionsLocked.ShouldBeTrue();
        var act = () => bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "yt-456", DateTimeOffset.UtcNow);
        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void StartNextCycle_ConsumesSelectedIngredient()
    {
        var bar = CreateBar(SubmissionPolicy.AlwaysOpen);
        var hipster = bar.AddHipster(Guid.NewGuid(), "ChemexCharlie");
        var ingredient = bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "yt-001", DateTimeOffset.UtcNow);
        bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var selector = new StubIngredientSelector();
        var cycle = bar.StartNextCycle(bar.Sessions.Single().Id, Guid.NewGuid(), DateTimeOffset.UtcNow, selector);

        cycle.IngredientId.ShouldBe(ingredient.IngredientId);
        bar.Ingredients.Single().IsConsumed.ShouldBeTrue();
    }

    [Fact]
    public void StartSession_Throws_WhenAnotherSessionIsActive()
    {
        var bar = CreateBar();
        var hipster = bar.AddHipster(Guid.NewGuid(), "PressPaul");
        bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "yt-777", DateTimeOffset.UtcNow);

        bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var act = () => bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(1));

        var exception = Should.Throw<DomainException>(act);
        exception.Message.ShouldContain("already active", Case.Insensitive);
    }

    [Fact]
    public void EndSession_Throws_WhenCycleStillActive()
    {
        var bar = CreateBar();
        var alice = bar.AddHipster(Guid.NewGuid(), "Alice");
        bar.SubmitIngredient(Guid.NewGuid(), alice.Id, "yt-abc", DateTimeOffset.UtcNow);
        var session = bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        bar.StartNextCycle(session.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, new StubIngredientSelector());

        var act = () => bar.EndSession(session.Id, DateTimeOffset.UtcNow.AddMinutes(5));

        var exception = Should.Throw<DomainException>(act);
        exception.Message.ShouldContain("Reveal", Case.Insensitive);
    }

    [Fact]
    public void EndSession_UnlocksSubmissions_WhenPolicyLocksOnBrew()
    {
        var bar = CreateBar(SubmissionPolicy.LockOnFirstBrew);
        var alice = bar.AddHipster(Guid.NewGuid(), "Alice");
        var bob = bar.AddHipster(Guid.NewGuid(), "Bob");
        bar.SubmitIngredient(Guid.NewGuid(), alice.Id, "yt-abc", DateTimeOffset.UtcNow);
        bar.SubmitIngredient(Guid.NewGuid(), bob.Id, "yt-def", DateTimeOffset.UtcNow);
        var session = bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cycle = bar.StartNextCycle(session.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, new StubIngredientSelector());
        bar.Reveal(cycle.Id, DateTimeOffset.UtcNow.AddMinutes(2));

        bar.EndSession(session.Id, DateTimeOffset.UtcNow.AddMinutes(3));

        bar.SubmissionsLocked.ShouldBeFalse();
        var newSubmission = bar.SubmitIngredient(Guid.NewGuid(), alice.Id, "yt-ghi", DateTimeOffset.UtcNow.AddMinutes(4));
        newSubmission.ShouldNotBeNull();
    }

    [Fact]
    public void EndSession_AllowsStartingNewSession()
    {
        var bar = CreateBar(SubmissionPolicy.AlwaysOpen);
        var alice = bar.AddHipster(Guid.NewGuid(), "Alice");
        bar.SubmitIngredient(Guid.NewGuid(), alice.Id, "yt-abc", DateTimeOffset.UtcNow);
        bar.SubmitIngredient(Guid.NewGuid(), alice.Id, "yt-def", DateTimeOffset.UtcNow.AddMinutes(1));

        var firstSession = bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var firstCycle = bar.StartNextCycle(firstSession.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, new StubIngredientSelector());
        bar.Reveal(firstCycle.Id, DateTimeOffset.UtcNow.AddMinutes(2));
        bar.EndSession(firstSession.Id, DateTimeOffset.UtcNow.AddMinutes(3));

        Should.NotThrow(() => bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow.AddMinutes(4)));
    }

    [Fact]
    public void CastVote_Throws_WhenHipsterVotesForSelf()
    {
        var bar = CreateBar();
        var hipster = bar.AddHipster(Guid.NewGuid(), "RoastRita");
        var other = bar.AddHipster(Guid.NewGuid(), "PourOverPete");
        bar.SubmitIngredient(Guid.NewGuid(), hipster.Id, "yt-123", DateTimeOffset.UtcNow);
        var session = bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cycle = bar.StartNextCycle(session.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, new StubIngredientSelector());

        var act = () => bar.CastVote(cycle.Id, Guid.NewGuid(), hipster.Id, hipster.Id, DateTimeOffset.UtcNow);

        Should.Throw<DomainException>(act);
    }

    [Fact]
    public void Reveal_MarksCorrectVotes_WhenTargetMatchesAnySubmitter()
    {
        var bar = CreateBar();
        var alex = bar.AddHipster(Guid.NewGuid(), "AeroPressAlex");
        var taylor = bar.AddHipster(Guid.NewGuid(), "Taylor");
        var jordan = bar.AddHipster(Guid.NewGuid(), "Jordan");

        bar.SubmitIngredient(Guid.NewGuid(), alex.Id, "yt-999", DateTimeOffset.UtcNow);
        bar.SubmitIngredient(Guid.NewGuid(), taylor.Id, "yt-999", DateTimeOffset.UtcNow.AddMinutes(1));

        var session = bar.StartSession(Guid.NewGuid(), DateTimeOffset.UtcNow);
        var cycle = bar.StartNextCycle(session.Id, Guid.NewGuid(), DateTimeOffset.UtcNow, new StubIngredientSelector());

        bar.CastVote(cycle.Id, Guid.NewGuid(), jordan.Id, alex.Id, DateTimeOffset.UtcNow);
        bar.CastVote(cycle.Id, Guid.NewGuid(), alex.Id, taylor.Id, DateTimeOffset.UtcNow);

        var result = bar.Reveal(cycle.Id, DateTimeOffset.UtcNow);

        result.CorrectGuessers.ShouldContain(jordan.Id);
        result.CorrectGuessers.ShouldContain(alex.Id);
        result.CorrectGuessers.Count.ShouldBe(2);
        result.Tally.ContainsKey(alex.Id).ShouldBeTrue();
        result.Tally[alex.Id].ShouldBe(1);
        result.Tally.ContainsKey(taylor.Id).ShouldBeTrue();
        result.Tally[taylor.Id].ShouldBe(1);
    }

    private sealed class StubIngredientSelector : INextIngredientSelector
    {
        public Ingredient? PickNext(IReadOnlyCollection<Ingredient> candidates)
        {
            return candidates.FirstOrDefault();
        }
    }
}
