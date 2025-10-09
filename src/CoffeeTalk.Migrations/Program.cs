using CoffeeTalk.Infrastructure.Data;
using CoffeeTalk.Migrations;
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