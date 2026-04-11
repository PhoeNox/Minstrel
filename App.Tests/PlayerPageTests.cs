namespace App.Tests;

using Microsoft.Playwright;
using TUnit.Playwright;

public class PlayerPageTests : PageTest
{
    [Test]
    public async Task CapturePlayerPage()
    {
        await Page.GotoAsync(AppFixture.BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(Page.Locator(".full-page-container")).ToBeVisibleAsync();

        var screenshot = await Page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        await Verify(screenshot, "png");
    }
}
