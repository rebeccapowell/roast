using System.Net;
using System.Net.Http.Json;
using System.Threading;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using CoffeeTalk.Api.Contracts;
using CoffeeTalk.TestUtilities;
using Shouldly;
using Xunit;

namespace CoffeeTalk.Api.Tests;

public class BrewSessionEndpointsTests
{
    [RequiresDockerFact]
    public async Task GetBrewSessions_ReturnsSeededSessions()
    {
        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var response = await client.GetFromJsonAsync<IReadOnlyList<BrewSessionSummary>>("/brew-sessions");

        response.ShouldNotBeNull();
        response!.ShouldNotBeEmpty();
        response.First().Name.ShouldBe("Latte Art Throwdown");
    }

    [RequiresDockerFact]
    public async Task GetBrewSessionById_ReturnsNotFound_ForUnknownId()
    {
        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var response = await client.GetAsync($"/brew-sessions/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    private static async Task<DistributedApplication> BuildAndStartAppAsync()
    {
        var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CoffeeTalk_AppHost>();
        var app = await builder.BuildAsync();
        await app.StartAsync();
        return app;
    }

    private static async Task WaitForApiAsync(DistributedApplication app)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await app.ResourceNotifications.WaitForResourceHealthyAsync("coffeetalk-api", cts.Token);
    }

}
