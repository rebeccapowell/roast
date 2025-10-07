using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using CoffeeTalk.Api.Contracts.CoffeeBars;
using CoffeeTalk.Domain.CoffeeBars;
using CoffeeTalk.TestUtilities;
using Shouldly;
using Xunit;

namespace CoffeeTalk.Api.Tests;

public sealed class CoffeeBarEndpointsTests
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();

    [RequiresDockerFact]
    public async Task CreateCoffeeBar_ReturnsCreatedResource()
    {
        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var createResponse = await client.PostAsJsonAsync(
            "/coffee-bars",
            new
            {
                theme = "Synthwave Showdown",
                defaultMaxIngredientsPerHipster = 4,
                submissionPolicy = SubmissionPolicy.AlwaysOpen.ToString()
            });

        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<CoffeeBarResource>(SerializerOptions);
        created.ShouldNotBeNull();
        created!.Theme.ShouldBe("Synthwave Showdown");
        created.Code.Length.ShouldBe(6);
        created.Code.All(char.IsLetterOrDigit).ShouldBeTrue();
        created.Code.ToUpperInvariant().IndexOfAny("AEIOU".ToCharArray()).ShouldBe(-1);

        var fetched = await client.GetFromJsonAsync<CoffeeBarResource>($"/coffee-bars/{created.Code}", SerializerOptions);
        fetched.ShouldNotBeNull();
        fetched!.Code.ShouldBe(created.Code);
        fetched.Theme.ShouldBe("Synthwave Showdown");
    }

    [RequiresDockerFact]
    public async Task JoinCoffeeBar_PreventsDuplicateUsernames()
    {
        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var coffeeBar = await CreateCoffeeBarAsync(client);

        var joinPayload = new { username = "LatteQueen" };
        var firstJoin = await client.PostAsJsonAsync($"/coffee-bars/{coffeeBar.Code}/hipsters", joinPayload);
        firstJoin.EnsureSuccessStatusCode();

        var duplicateJoin = await client.PostAsJsonAsync($"/coffee-bars/{coffeeBar.Code}/hipsters", joinPayload);
        duplicateJoin.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [RequiresDockerFact]
    public async Task SubmitIngredient_NormalizesYouTubeUrl()
    {
        await using var app = await BuildAndStartAppAsync();
        await WaitForApiAsync(app);

        var client = app.CreateHttpClient("coffeetalk-api");
        var coffeeBar = await CreateCoffeeBarAsync(client);

        var joinResponse = await client.PostAsJsonAsync(
            $"/coffee-bars/{coffeeBar.Code}/hipsters",
            new { username = "BeanCollector" });
        joinResponse.EnsureSuccessStatusCode();

        var joined = await joinResponse.Content.ReadFromJsonAsync<JoinCoffeeBarResponse>(SerializerOptions);
        joined.ShouldNotBeNull();

        var submitResponse = await client.PostAsJsonAsync(
            $"/coffee-bars/{coffeeBar.Code}/ingredients",
            new
            {
                hipsterId = joined!.Hipster.Id,
                url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ"
            });

        submitResponse.EnsureSuccessStatusCode();

        var submission = await submitResponse.Content.ReadFromJsonAsync<SubmitIngredientResponse>(SerializerOptions);
        submission.ShouldNotBeNull();
        submission!.Ingredient.VideoId.ShouldBe("dQw4w9WgXcQ");
        submission.Ingredient.SubmitterIds.ShouldContain(joined.Hipster.Id);
    }

    private static async Task<CoffeeBarResource> CreateCoffeeBarAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync(
            "/coffee-bars",
            new
            {
                theme = "Late Night Jazz",
                defaultMaxIngredientsPerHipster = 5,
                submissionPolicy = SubmissionPolicy.LockOnFirstBrew.ToString()
            });

        response.EnsureSuccessStatusCode();
        var resource = await response.Content.ReadFromJsonAsync<CoffeeBarResource>(SerializerOptions);
        return resource ?? throw new InvalidOperationException("Coffee bar creation returned no content.");
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

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
