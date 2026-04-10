namespace App.Tests;

using Microsoft.Playwright;
using TUnit.Playwright;

public class PlaylistsPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        return new BrowserNewContextOptions(Playwright.Devices["iPhone 13"])
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light,
        };
    }

    [Test]
    public async Task CapturePlaylistsPage()
    {
        await Page.GotoAsync($"{AppFixture.BaseUrl}/playlists");
        await Page.WaitForSelectorAsync("#day-playlist");

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(AppFixture.ScreenshotsDir, "playlists-mobile.png"),
            FullPage = true,
        });
    }
}
