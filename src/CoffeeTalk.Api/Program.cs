using CoffeeTalk.Api.Endpoints;
using CoffeeTalk.Api.Services;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<IBrewSessionSummaryProvider, InMemoryBrewSessionSummaryProvider>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapDefaultEndpoints();
app.MapBrewSessionEndpoints();

app.Run();
