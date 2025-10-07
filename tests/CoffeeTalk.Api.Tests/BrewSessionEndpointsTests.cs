using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using CoffeeTalk.Api.Contracts;
using Shouldly;
using Xunit;

namespace CoffeeTalk.Api.Tests;

public class BrewSessionEndpointsTests
{
    [Fact]
    public async Task GetBrewSessions_ReturnsSeededSessions()
    {
        if (!IsContainerRuntimeAvailable())
        {
            return;
        }

        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var response = await client.GetFromJsonAsync<IReadOnlyList<BrewSessionSummary>>("/brew-sessions");

        response.ShouldNotBeNull();
        response!.ShouldNotBeEmpty();
        response.First().Name.ShouldBe("Latte Art Throwdown");
    }

    [Fact]
    public async Task GetBrewSessionById_ReturnsNotFound_ForUnknownId()
    {
        if (!IsContainerRuntimeAvailable())
        {
            return;
        }

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

    private static bool IsContainerRuntimeAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "--version",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            });

            if (process is null)
            {
                return false;
            }

            if (!process.WaitForExit(2000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // Swallow exceptions thrown when attempting to kill a hung process.
                }

                return false;
            }

            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
