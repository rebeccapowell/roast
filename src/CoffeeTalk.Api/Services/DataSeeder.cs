using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTalk.Api.Services;

public sealed class DataSeeder
{
    private readonly CoffeeTalkDbContext _context;
    private readonly ICoffeeBarRepository _repository;
    private readonly bool _resetOnStartup;

    public DataSeeder(CoffeeTalkDbContext context, ICoffeeBarRepository repository, bool resetOnStartup)
    {
        _context = context;
        _repository = repository;
        _resetOnStartup = resetOnStartup;
    }

    public async Task SeedAsync()
    {
        if (_resetOnStartup)
        {
            // Purge existing data
            await _context.Database.EnsureDeletedAsync();
            await _context.Database.EnsureCreatedAsync();
        }
        else
        {
            await _context.Database.EnsureCreatedAsync();

            // Check if we already have data
            if (await _context.CoffeeBars.AnyAsync())
            {
                return;
            }
        }

        var now = DateTimeOffset.UtcNow;

        // Create a coffee bar with a unique code (6 chars, no vowels)
        var coffeeBar = CoffeeBar.Create(
            id: Guid.NewGuid(),
            code: "RST42Y",
            theme: "Tech Talk Roast",
            defaultMaxIngredientsPerHipster: 3,
            submissionPolicy: SubmissionPolicy.AlwaysOpen);

        // Add 3 hipsters
        var hipster1 = coffeeBar.AddHipster(Guid.NewGuid(), "CoolCoder");
        var hipster2 = coffeeBar.AddHipster(Guid.NewGuid(), "DevGuru");
        var hipster3 = coffeeBar.AddHipster(Guid.NewGuid(), "TechNinja");

        // Add 3 random YouTube videos as ingredients
        coffeeBar.SubmitIngredient(
            submissionId: Guid.NewGuid(),
            hipsterId: hipster1.Id,
            videoId: "dQw4w9WgXcQ",
            submittedAt: now,
            title: "Never Gonna Give You Up",
            thumbnailUrl: "https://i.ytimg.com/vi/dQw4w9WgXcQ/default.jpg");

        coffeeBar.SubmitIngredient(
            submissionId: Guid.NewGuid(),
            hipsterId: hipster2.Id,
            videoId: "jNQXAC9IVRw",
            submittedAt: now,
            title: "Me at the zoo",
            thumbnailUrl: "https://i.ytimg.com/vi/jNQXAC9IVRw/default.jpg");

        coffeeBar.SubmitIngredient(
            submissionId: Guid.NewGuid(),
            hipsterId: hipster3.Id,
            videoId: "9bZkp7q19f0",
            submittedAt: now,
            title: "PSY - GANGNAM STYLE",
            thumbnailUrl: "https://i.ytimg.com/vi/9bZkp7q19f0/default.jpg");

        await _repository.AddAsync(coffeeBar);
    }
}
