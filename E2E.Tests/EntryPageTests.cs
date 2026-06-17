namespace E2E.Tests;

using Microsoft.Playwright;
using static VerifyTUnit.Verifier;

public class EntryPageTests
{
	// The "Enter the town" gate the Player shows on first open, before sound starts.
	[Test]
	public async Task EntryGate_OnFirstOpen()
	{
		await using var context = await BackendServer.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
			DeviceScaleFactor = 1,
			ReducedMotion = ReducedMotion.Reduce,
			ColorScheme = ColorScheme.Dark,
		});
		var page = await context.NewPageAsync();

		// The Player holds a long-lived SSE stream open, so the network never goes
		// idle; gate on DOM load plus the button instead.
		await page.GotoAsync(BackendServer.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Enter the town" }).WaitForAsync();

		// Strip motion and non-deterministic chrome (release marker, QR with LAN IP,
		// animated backdrop) so the snapshot is stable across runs.
		await page.AddStyleTagAsync(new PageAddStyleTagOptions
		{
			Content = """
				*, *::before, *::after { animation: none !important; transition: none !important; }
				.version, .stage, .pairing, .reveal-btn { visibility: hidden !important; }
				""",
		});
		await page.EvaluateAsync("async () => { await document.fonts.ready; }");

		var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
		{
			Animations = ScreenshotAnimations.Disabled,
		});
		await Verify(screenshot, "png");
	}
}
