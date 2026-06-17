namespace Player.E2E.Tests;

using Microsoft.Playwright;
using static VerifyTUnit.Verifier;

// Snapshots of the running Player (past the entry gate) in its main states. The
// Backend is a single shared session, so these tests run sequentially (NotInParallel)
// and each drives the session to an absolute state — phase and timer — before
// capturing, rather than relying on whatever a previous test left behind. The Order
// keeps them after CrossSurfaceTests' Order 1 startup shot, which needs pristine state.
public class PlayerStateTests
{
	// Day phase, no timer: the Player at rest, just its atmospheric backdrop.
	[Test, NotInParallel(Order = 4)]
	public async Task Resting_AfterEnteringTown()
	{
		await using var context = await NewStageContextAsync();
		var page = await context.NewPageAsync();

		await EnterTownAsync(page);
		await EnsurePhaseAsync(page, night: false);
		await StopTimerAsync(page);

		await StabilizeAsync(page);
		await Verify(await page.ScreenshotAsync(StillFrame), "png");
	}

	// Night phase: the backdrop drops to its desaturated night look.
	[Test, NotInParallel(Order = 5)]
	public async Task Resting_AtNight()
	{
		await using var context = await NewStageContextAsync();
		var page = await context.NewPageAsync();

		await EnterTownAsync(page);
		await StopTimerAsync(page);
		await EnsurePhaseAsync(page, night: true);

		await StabilizeAsync(page);
		await Verify(await page.ScreenshotAsync(StillFrame), "png");
	}

	// Day phase with a countdown running: the clock medallion is on screen. A
	// 600s timer renders a stable "10 min" for the seconds the capture takes.
	[Test, NotInParallel(Order = 6)]
	public async Task Daytime_WithTimerRunning()
	{
		await using var context = await NewStageContextAsync();
		var page = await context.NewPageAsync();

		await EnterTownAsync(page);
		await EnsurePhaseAsync(page, night: false);
		await StartTimerAsync(page, 600);

		await StabilizeAsync(page);
		await Verify(await page.ScreenshotAsync(StillFrame), "png");
	}

	private static Task<IBrowserContext> NewStageContextAsync() =>
		BackendServer.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
			DeviceScaleFactor = 1,
			ReducedMotion = ReducedMotion.Reduce,
			ColorScheme = ColorScheme.Dark,
		});

	private static async Task EnterTownAsync(IPage page)
	{
		// The Player holds a long-lived SSE stream open, so the network never goes
		// idle; gate on DOM load plus the button instead.
		await page.GotoAsync(BackendServer.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
		await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Enter the town" }).ClickAsync();
		await page.Locator(".gate").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
	}

	// Phase is toggle-only with no read endpoint, so read the true phase from one
	// SSE snapshot, toggle if it differs, then wait for the DOM to reflect the target.
	private static async Task EnsurePhaseAsync(IPage page, bool night)
	{
		var isNight = await CurrentPhaseAsync(page) == "Night";
		if (isNight != night)
			await PostCommandAsync(page, "/playback/switch-phase");
		await page.Locator(night ? "main.night" : "main:not(.night)").WaitForAsync();
	}

	private static Task<string> CurrentPhaseAsync(IPage page) => page.EvaluateAsync<string>(ReadSnapshotPhase);

	private static async Task StopTimerAsync(IPage page)
	{
		await PostCommandAsync(page, "/timer/stop");
		await page.Locator(".clock").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
	}

	private static async Task StartTimerAsync(IPage page, int seconds)
	{
		await page.EvaluateAsync(
			"s => fetch('/timer/start', { method: 'POST', headers: { 'content-type': 'application/json' }, body: JSON.stringify({ duration: s }) })",
			seconds);
		await page.Locator(".clock").WaitForAsync();
	}

	private static Task PostCommandAsync(IPage page, string path) =>
		page.EvaluateAsync($"() => fetch('{path}', {{ method: 'POST' }})");

	// Strip motion and non-deterministic chrome (release marker, QR with LAN IP),
	// settle web fonts and the backdrop image, so the snapshot is stable across runs.
	private static async Task StabilizeAsync(IPage page)
	{
		await page.AddStyleTagAsync(new PageAddStyleTagOptions
		{
			Content = """
				*, *::before, *::after { animation: none !important; transition: none !important; }
				.version, .pairing, .reveal-btn { visibility: hidden !important; }
				""",
		});
		await page.EvaluateAsync("async () => { await document.fonts.ready; }");
		await page.WaitForFunctionAsync(
			"() => { const i = document.querySelector('.backdrop'); return !!i && i.complete && i.naturalHeight > 0; }");
	}

	// Reads the first full state snapshot off the SSE stream and returns its phase.
	private const string ReadSnapshotPhase = """
		async () => {
			const reader = (await fetch('/playback/sse')).body.getReader();
			const decoder = new TextDecoder();
			let buffer = '';
			try {
				while (true) {
					const { value, done } = await reader.read();
					if (done) return null;
					buffer += decoder.decode(value, { stream: true });
					let i;
					while ((i = buffer.indexOf('\n\n')) >= 0) {
						const line = buffer.slice(0, i).split('\n').find(l => l.startsWith('data:'));
						if (line) return JSON.parse(line.slice(5).trim()).phase;
						buffer = buffer.slice(i + 2);
					}
				}
			} finally {
				await reader.cancel();
			}
		}
		""";

	private static readonly PageScreenshotOptions StillFrame = new() { Animations = ScreenshotAnimations.Disabled };
}
