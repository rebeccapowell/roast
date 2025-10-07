using System.Text.Json;
using System.Text.Json.Serialization;
using CoffeeTalk.Api.Endpoints;
using CoffeeTalk.Api.Services;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.Infrastructure.Data;
using CoffeeTalk.Infrastructure.Data.Repositories;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

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
            .AllowAnyMethod();
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(FrontendCorsPolicyName);

app.MapDefaultEndpoints();
app.MapCoffeeBarEndpoints();
app.MapBrewSessionEndpoints();

app.Run();
