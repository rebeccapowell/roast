using CoffeeTalk.Infrastructure.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeTalk.Infrastructure.Data;

public sealed class CoffeeTalkDbContext(DbContextOptions<CoffeeTalkDbContext> options) : DbContext(options)
{
    public DbSet<CoffeeBarEntity> CoffeeBars => Set<CoffeeBarEntity>();

    public DbSet<HipsterEntity> Hipsters => Set<HipsterEntity>();

    public DbSet<IngredientEntity> Ingredients => Set<IngredientEntity>();

    public DbSet<SubmissionEntity> Submissions => Set<SubmissionEntity>();

    public DbSet<BrewSessionEntity> BrewSessions => Set<BrewSessionEntity>();

    public DbSet<BrewCycleEntity> BrewCycles => Set<BrewCycleEntity>();

    public DbSet<VoteEntity> Votes => Set<VoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoffeeTalkDbContext).Assembly);
    }
}
