using CoffeeTalk.Api.Endpoints;
using CoffeeTalk.Api.Services;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IBrewSessionSummaryProvider, InMemoryBrewSessionSummaryProvider>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.MapBrewSessionEndpoints();

app.Run();
