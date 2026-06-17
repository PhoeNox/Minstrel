namespace Backend.Tests.Timer;

using Features.Timer;

public class Countdown
{
	[Test]
	public async Task CountsDownWithElapsedTimeWhileRunning()
	{
		var anchor = CountdownTimer.Start(duration: TimeSpan.FromSeconds(60), now: 1000);

		var left = CountdownTimer.DeriveTimeLeft(anchor, now: 11_000);

		await Assert.That(left).IsEqualTo(50);
	}

	[Test]
	public async Task IsExactlyTheDurationAtTheAnchorInstant()
	{
		var anchor = CountdownTimer.Start(duration: TimeSpan.FromSeconds(60), now: 1000);

		var left = CountdownTimer.DeriveTimeLeft(anchor, now: 1000);

		await Assert.That(left).IsEqualTo(60);
	}

	[Test]
	public async Task NeverGoesBelowZero()
	{
		var anchor = CountdownTimer.Start(duration: TimeSpan.FromSeconds(60), now: 1000);

		var left = CountdownTimer.DeriveTimeLeft(anchor, now: 100_000);

		await Assert.That(left).IsEqualTo(0);
	}

	[Test]
	public async Task StaysAtTheDurationWhenIdle()
	{
		var left = CountdownTimer.DeriveTimeLeft(TimerAnchor.Idle, now: 100_000);

		await Assert.That(left).IsEqualTo(0);
	}

	[Test]
	public async Task HasNotExpiredBeforeTheDurationElapses()
	{
		var anchor = CountdownTimer.Start(duration: TimeSpan.FromSeconds(60), now: 1000);

		await Assert.That(CountdownTimer.HasExpired(anchor, now: 60_000)).IsFalse();
	}

	[Test]
	public async Task ExpiresOnceTheDurationElapses()
	{
		var anchor = CountdownTimer.Start(duration: TimeSpan.FromSeconds(60), now: 1000);

		await Assert.That(CountdownTimer.HasExpired(anchor, now: 61_000)).IsTrue();
	}

	[Test]
	public async Task AnIdleTimerNeverExpires()
	{
		await Assert.That(CountdownTimer.HasExpired(TimerAnchor.Idle, now: 100_000)).IsFalse();
	}
}
