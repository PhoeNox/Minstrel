using Microsoft.Playwright;
using TUnit.Core;
using TUnit.Playwright;

namespace App.Tests;

public class RemotePageTests : PageTest
{
    public override BrowserNewContextOptions ContextOptions(TestContext testContext)
    {
        return new BrowserNewContextOptions(Playwright.Devices["iPhone 13"])
        {
            Locale = "en-US",
            ColorScheme = ColorScheme.Light
        };
    }

    [Test]
    public async Task CaptureRemotePage()
    {
        await Page.GotoAsync($"{AppFixture.BaseUrl}/Remote");
        await Page.WaitForSelectorAsync("#remote-container");

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = Path.Combine(AppFixture.ScreenshotsDir, "remote-mobile.png"),
            FullPage = true
        });
    }
}
