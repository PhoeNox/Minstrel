using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Playwright;

namespace App.Tests;

public class PlaylistsPageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        return Playwright.Devices["iPhone 13"];
    }

    [Test]
    public async Task CapturePlaylistsPage()
    {
        await Page.GotoAsync($"{AppFixture.BaseUrl}/playlists");
        await Page.WaitForSelectorAsync("#day-playlist");

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(AppFixture.ScreenshotsDir, "playlists-mobile.png"),
            FullPage = true
        });
    }
}
