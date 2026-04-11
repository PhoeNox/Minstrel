namespace App.Tests;

using Microsoft.Playwright;
using TUnit.Playwright;

[DependsOn<PlayerPageTests>]
public class RemotePageTests : PageTest
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
    public async Task CaptureRemotePage()
    {
        await Page.GotoAsync($"{AppFixture.BaseUrl}/Remote", new PageGotoOptions
        {
            WaitUntil = WaitUntilState.NetworkIdle,
        });
        await Expect(Page.Locator("#remote-container")).ToBeVisibleAsync();

        var screenshot = await Page.ScreenshotAsync();
        await Verify(screenshot, "png");
    }
}