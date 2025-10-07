using CoffeeTalk.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<CoffeeTalkDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("coffeetalkdb")
        ?? throw new InvalidOperationException("Connection string 'coffeetalkdb' was not found.");

    options.UseNpgsql(connectionString);
});

using var host = builder.Build();

await host.RunMigrationAsync<CoffeeTalkDbContext>();

internal static class MigrationHostExtensions
{
    public static async Task RunMigrationAsync<TContext>(this IHost host)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(host);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.MigrateAsync();
    }
}
