namespace Features.Tests.Timer;

using Features.Playback;
using Features.Timer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Time.Testing;

public class StoppingTimer
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	[Test]
	public async Task WhenStoppedManually_DoesNotPlayGong()
	{
		var gongPlayed = false;
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<PlaySoundSignal>(this, _ => gongPlayed = true);
		var fakeTimeProvider = (FakeTimeProvider)AppContext.Services.GetRequiredService<TimeProvider>();

		AppContext.Dispatcher.Dispatch(new StartTimerAction(TimeSpan.FromSeconds(10)));
		await Task.Yield();

		AppContext.Dispatcher.Dispatch(new SetIsTimeRunningAction(false));
		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(100));
		await Task.Yield();

		await Assert.That(gongPlayed).IsFalse();
	}

	[Test]
	public async Task WhenExpiredNaturally_PlaysGong()
	{
		var gongPlayed = false;
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<PlaySoundSignal>(this, _ => gongPlayed = true);
		var fakeTimeProvider = (FakeTimeProvider)AppContext.Services.GetRequiredService<TimeProvider>();

		AppContext.Dispatcher.Dispatch(new StartTimerAction(TimeSpan.FromMilliseconds(100)));
		await Task.Yield();

		fakeTimeProvider.Advance(TimeSpan.FromMilliseconds(200));
		await Task.Yield();

		await Assert.That(gongPlayed).IsTrue();
	}
}
