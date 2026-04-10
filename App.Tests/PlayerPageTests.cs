namespace App.Tests;

using Microsoft.Playwright;
using TUnit.Playwright;


public class PlayerPageTests : PageTest
{
    [Test]
    public async Task CapturePlayerPage()
    {
        await Page.GotoAsync(AppFixture.BaseUrl);
        await Page.WaitForSelectorAsync(".full-page-container");

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(AppFixture.ScreenshotsDir, "player-desktop.png"),
            FullPage = true,
        });
    }
}
