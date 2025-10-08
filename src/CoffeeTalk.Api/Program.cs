using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeTalk.Api.Endpoints;
using CoffeeTalk.Api.Hubs;
using CoffeeTalk.Api.Services;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data;
using CoffeeTalk.Infrastructure.Data.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

const string FrontendCorsPolicyName = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicyName, policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "https://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

builder.Services.AddDbContext<CoffeeTalkDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("coffeetalkdb")
        ?? throw new InvalidOperationException("Connection string 'coffeetalkdb' was not found.");

    options.UseNpgsql(connectionString);
});

builder.Services.AddScoped<ICoffeeBarRepository, CoffeeBarRepository>();
builder.Services.AddScoped<ICoffeeBarCodeGenerator, CoffeeBarCodeGenerator>();
builder.Services.AddSingleton<INextIngredientSelector, RandomNextIngredientSelector>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IBrewSessionSummaryProvider, InMemoryBrewSessionSummaryProvider>();
builder.Services.Configure<YouTubeOptions>(builder.Configuration.GetSection("YouTube"));
builder.Services.PostConfigure<YouTubeOptions>(options =>
{
    options.ApiKey ??= builder.Configuration["YouTubeApiKey"];
});
builder.Services.AddHttpClient<IYouTubeMetadataProvider, YouTubeMetadataProvider>(client =>
{
    client.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var seeder = new DataSeeder(
            scope.ServiceProvider.GetRequiredService<CoffeeTalkDbContext>(),
            scope.ServiceProvider.GetRequiredService<ICoffeeBarRepository>(),
            true);

        await seeder.SeedAsync();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicyName);

app.MapDefaultEndpoints();
app.MapCoffeeBarEndpoints();
app.MapBrewSessionEndpoints();
app.MapHub<CoffeeBarHub>("/hubs/coffee-bar");

await app.RunAsync();
