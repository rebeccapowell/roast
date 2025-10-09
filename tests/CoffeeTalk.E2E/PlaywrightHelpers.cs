using Microsoft.Playwright;
using Shouldly;
using Xunit;

public sealed class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = default!;
    public IBrowser Browser { get; private set; } = default!;

    public async Task InitializeAsync()
    {
        var exitCode = await Task.Run(() => Microsoft.Playwright.Program.Main(new[] { "install", "--with-deps" }));
        exitCode.ShouldBe(0);

        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.CloseAsync();
        }

        Playwright?.Dispose();
    }
}

[CollectionDefinition("playwright")]
public sealed class PlaywrightCollection : ICollectionFixture<PlaywrightFixture>
{
}

public static class Target
{
    public static string BaseUrl =>
        Environment.GetEnvironmentVariable("WEB_BASE_URL")
        ?? throw new InvalidOperationException("WEB_BASE_URL not set");
}
