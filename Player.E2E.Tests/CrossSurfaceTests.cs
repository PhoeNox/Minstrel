namespace Player.E2E.Tests;

using System.Text.Json;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;
using static VerifyTUnit.Verifier;

// Scenarios that drive the shipped Remote and observe both the Remote and the
// Player reacting over the shared Backend session. The Backend is one global
// session with no reset, so these run serially (NotInParallel) in a fixed Order:
// the pristine "nothing playing" startup shot must be captured before any test
// selects or plays a song, since no command clears the current selection.
public class CrossSurfaceTests
{
	// Fresh process: the loaded Day playlist is shown with its first song cued but
	// nothing playing ("Nothing cued"). Must run first — Order 1 — while pristine.
	[Test, NotInParallel(Order = 1)]
	public async Task Startup_RemoteShowsIdlePlaylist()
	{
		await using var context = await RemoteContextAsync();
		var remote = await OpenRemoteAsync(context);

		await remote.Locator(".np-empty").WaitForAsync();
		await remote.Locator(".deck li").First.WaitForAsync();

		await StabilizeRemoteAsync(remote);
		await Verify(await remote.ScreenshotAsync(StillFrame), "png");
	}

	// Starting the timer from the Remote: the Player raises its countdown medallion
	// and the Remote's transport shows the running clock.
	[Test, NotInParallel(Order = 2)]
	public async Task Timer_StartedFromRemote()
	{
		await using var playerContext = await PlayerContextAsync();
		await using var remoteContext = await RemoteContextAsync();
		var player = await playerContext.NewPageAsync();
		var remote = await remoteContext.NewPageAsync();

		await EnterTownAsync(player);
		await EnsurePhaseAsync(player, night: false);
		await OpenRemoteAsync(remoteContext, remote);

		// Default stepper is 5 minutes → a 300s timer.
		await remote.Locator(".timerbtn").ClickAsync();
		await remote.Locator(".timer-start").ClickAsync();

		await player.Locator(".clock").WaitForAsync();
		await StabilizePlayerAsync(player);
		await Verify(await player.ScreenshotAsync(StillFrame), "png").UseTextForParameters("player");

		// The Remote clock ticks every second; freeze its time at the server's timer
		// anchor so the displayed value is exactly the full duration ("5:00").
		await remote.Clock.SetFixedTimeAsync(await TimerAnchorAsync(remote));
		await Expect(remote.Locator(".timerbtn-clock")).ToHaveTextAsync("5:00");
		await StabilizeRemoteAsync(remote);
		await Verify(await remote.ScreenshotAsync(StillFrame), "png").UseTextForParameters("remote");

		await PostCommandAsync(remote, "/commands/timer-stop");
	}

	// Switching to Night from the Remote and opening the Night playlist: the Remote
	// adopts its moon-blue night skin and the Player's backdrop desaturates.
	[Test, NotInParallel(Order = 3)]
	public async Task Night_SwitchedFromRemote()
	{
		await using var playerContext = await PlayerContextAsync();
		await using var remoteContext = await RemoteContextAsync();
		var player = await playerContext.NewPageAsync();
		var remote = await remoteContext.NewPageAsync();

		await EnterTownAsync(player);
		await EnsurePhaseAsync(player, night: false);
		await EnsureNoTimerAsync(player);
		await OpenRemoteAsync(remoteContext, remote);

		await remote.Locator(".switch").ClickAsync();
		await remote.Locator(".app.night").WaitForAsync();
		await remote.Locator(".tab", new PageLocatorOptions { HasTextString = "Night" }).ClickAsync();
		await remote.Locator(".tab.active", new PageLocatorOptions { HasTextString = "Night" }).WaitForAsync();

		await player.Locator("main.night").WaitForAsync();
		await StabilizePlayerAsync(player);
		await Verify(await player.ScreenshotAsync(StillFrame), "png").UseTextForParameters("player");

		await StabilizeRemoteAsync(remote);
		await Verify(await remote.ScreenshotAsync(StillFrame), "png").UseTextForParameters("remote");
	}

	private static Task<IBrowserContext> PlayerContextAsync() =>
		BackendServer.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
			DeviceScaleFactor = 1,
			ReducedMotion = ReducedMotion.Reduce,
			ColorScheme = ColorScheme.Dark,
		});

	private static Task<IBrowserContext> RemoteContextAsync() =>
		BackendServer.Browser.NewContextAsync(new BrowserNewContextOptions
		{
			ViewportSize = new ViewportSize { Width = 390, Height = 844 },
			DeviceScaleFactor = 1,
			ReducedMotion = ReducedMotion.Reduce,
			ColorScheme = ColorScheme.Dark,
		});

	private static async Task<IPage> OpenRemoteAsync(IBrowserContext context, IPage? existing = null)
	{
		var remote = existing ?? await context.NewPageAsync();
		await remote.GotoAsync($"{BackendServer.BaseUrl}/remote/", new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
		await remote.Locator(".deck li").First.WaitForAsync();
		return remote;
	}

	private static async Task EnterTownAsync(IPage player)
	{
		// The Player holds a long-lived SSE stream open, so the network never goes
		// idle; gate on DOM load plus the button instead.
		await player.GotoAsync(BackendServer.BaseUrl, new PageGotoOptions { WaitUntil = WaitUntilState.DOMContentLoaded });
		await player.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Enter the town" }).ClickAsync();
		await player.Locator(".gate").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
	}

	// Phase is toggle-only with no read endpoint, so read the true phase from one
	// SSE snapshot, toggle if it differs, then wait for the Player DOM to reflect it.
	private static async Task EnsurePhaseAsync(IPage player, bool night)
	{
		var snapshot = await ReadSnapshotAsync(player);
		var isNight = snapshot.GetProperty("phase").GetString() == "Night";
		if (isNight != night)
			await PostCommandAsync(player, "/commands/switch-phase");
		await player.Locator(night ? "main.night" : "main:not(.night)").WaitForAsync();
	}

	private static async Task EnsureNoTimerAsync(IPage player)
	{
		await PostCommandAsync(player, "/commands/timer-stop");
		await player.Locator(".clock").WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
	}

	private static async Task<DateTime> TimerAnchorAsync(IPage page)
	{
		var anchor = (await ReadSnapshotAsync(page)).GetProperty("timer").GetProperty("anchorTimestamp").GetInt64();
		return DateTimeOffset.FromUnixTimeMilliseconds(anchor).UtcDateTime;
	}

	private static async Task<JsonElement> ReadSnapshotAsync(IPage page)
	{
		var json = await page.EvaluateAsync<string>(ReadSnapshotJson);
		return JsonDocument.Parse(json).RootElement;
	}

	private static Task PostCommandAsync(IPage page, string path) =>
		page.EvaluateAsync($"() => fetch('{path}', {{ method: 'POST' }})");

	// Strip motion and non-deterministic chrome (release marker, QR with LAN IP),
	// settle web fonts and the backdrop image, so the Player snapshot is stable.
	private static async Task StabilizePlayerAsync(IPage player)
	{
		await player.AddStyleTagAsync(new PageAddStyleTagOptions
		{
			Content = """
				*, *::before, *::after { animation: none !important; transition: none !important; }
				.version, .pairing, .reveal-btn { visibility: hidden !important; }
				""",
		});
		await player.EvaluateAsync("async () => { await document.fonts.ready; }");
		await player.WaitForFunctionAsync(
			"() => { const i = document.querySelector('.backdrop'); return !!i && i.complete && i.naturalHeight > 0; }");
	}

	// Strip motion and the live playback-progress fill (grows with the clock), then
	// settle web fonts, so the Remote snapshot is stable.
	private static async Task StabilizeRemoteAsync(IPage remote)
	{
		await remote.AddStyleTagAsync(new PageAddStyleTagOptions
		{
			Content = """
				*, *::before, *::after { animation: none !important; transition: none !important; }
				.fill { display: none !important; }
				""",
		});
		await remote.EvaluateAsync("async () => { await document.fonts.ready; }");
	}

	// Reads the first full state snapshot off the SSE stream and returns its JSON.
	private const string ReadSnapshotJson = """
		async () => {
			const reader = (await fetch('/sse')).body.getReader();
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
						if (line) return line.slice(5).trim();
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
