namespace App.Tests;

using Microsoft.Playwright;
using TUnit.Playwright;

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

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(AppFixture.ScreenshotsDir, "remote-mobile.png"),
            FullPage = true,
        });
    }
}
