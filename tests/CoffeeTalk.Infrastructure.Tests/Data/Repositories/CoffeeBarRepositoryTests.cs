using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data;
using CoffeeTalk.Infrastructure.Data.Entities;
using CoffeeTalk.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace CoffeeTalk.Infrastructure.Tests.Data.Repositories;

public sealed class CoffeeBarRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_PersistsNewHipstersWithoutConcurrencyConflicts()
    {
        var options = new DbContextOptionsBuilder<CoffeeTalkDbContext>()
            .UseInMemoryDatabase($"coffee-bar-{Guid.NewGuid()}")
            .Options;

        await using var context = new CoffeeTalkDbContext(options);

        var coffeeBarId = Guid.NewGuid();
        const string coffeeBarCode = "BRW789";

        context.CoffeeBars.Add(new CoffeeBarEntity
        {
            Id = coffeeBarId,
            Code = coffeeBarCode,
            Theme = "Initial Theme",
            DefaultMaxIngredientsPerHipster = 3,
            SubmissionPolicy = SubmissionPolicy.AlwaysOpen,
            SubmissionsLocked = false
        });

        await context.SaveChangesAsync();

        var repository = new CoffeeBarRepository(context);
        var coffeeBar = await repository.GetByCodeAsync(coffeeBarCode);
        coffeeBar.ShouldNotBeNull();

        var hipsterId = Guid.NewGuid();
        coffeeBar!.AddHipster(hipsterId, "NewHipster");
        coffeeBar.Hipsters.Count.ShouldBe(1);

        await repository.UpdateAsync(coffeeBar);

        var reloaded = await context.CoffeeBars
            .Include(bar => bar.Hipsters)
            .SingleAsync(bar => bar.Id == coffeeBarId);

        reloaded.Hipsters.ShouldContain(hipster => hipster.Id == hipsterId);
    }
}
