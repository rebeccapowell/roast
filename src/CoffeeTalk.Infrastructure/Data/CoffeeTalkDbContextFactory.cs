using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CoffeeTalk.Infrastructure.Data;

public sealed class CoffeeTalkDbContextFactory : IDesignTimeDbContextFactory<CoffeeTalkDbContext>
{
    public CoffeeTalkDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CoffeeTalkDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=coffeetalk;Username=postgres;Password=postgres");

        return new CoffeeTalkDbContext(optionsBuilder.Options);
    }
}
