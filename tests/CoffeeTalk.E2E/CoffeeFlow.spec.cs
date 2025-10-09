using Microsoft.Playwright;
using Shouldly;
using System.Text.RegularExpressions;
using Xunit;

[Collection("playwright")]
public class CoffeeFlowSpec
{
    private readonly PlaywrightFixture fixture;

    public CoffeeFlowSpec(PlaywrightFixture fixture)
    {
        this.fixture = fixture;
    }

    [Fact]
    public async Task CreateSubmitVoteReveal_HappyPath()
    {
        Target.BaseUrl.ShouldNotBeNullOrWhiteSpace();

        await using var context = await fixture.Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        try
        {
            await page.GotoAsync($"{Target.BaseUrl}/coffee");

            var firstBarLink = page.GetByRole(AriaRole.Link).First;
            await firstBarLink.ClickAsync();

            var submissionsHeading = page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Submissions", RegexOptions.IgnoreCase) });
            await submissionsHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await submissionsHeading.IsVisibleAsync()).ShouldBeTrue();

            await page.GetByPlaceholder("https://youtu.be/...").FillAsync("https://youtu.be/dQw4w9WgXcQ");
            var submitButton = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("submit", RegexOptions.IgnoreCase) });
            await submitButton.ClickAsync();

            var firstVoteButton = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("^Vote for", RegexOptions.IgnoreCase) }).First;
            await firstVoteButton.ClickAsync();

            var reveal = page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex("reveal results", RegexOptions.IgnoreCase) });
            if (await reveal.IsVisibleAsync())
            {
                await reveal.ClickAsync();
            }

            var resultsHeading = page.GetByRole(AriaRole.Heading, new() { NameRegex = new Regex("Results", RegexOptions.IgnoreCase) });
            await resultsHeading.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
            (await resultsHeading.IsVisibleAsync()).ShouldBeTrue();
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
