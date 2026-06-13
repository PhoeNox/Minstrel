namespace Backend.Tests.Playback;

using Backend.Playback;

public class Tick
{
	[Test]
	public async Task AdvancesTheAnchorWhilePlaying()
	{
		var playing = PlaybackTimeline.Play(TimelineState.Idle, "song-1", now: 1000);

		var ticked = PlaybackTimeline.Tick(playing, now: 4000);

		await Assert.That(ticked.Position.Offset).IsEqualTo(3);
		await Assert.That(ticked.Position.AnchorTimestamp).IsEqualTo(4000);
		await Assert.That(PlaybackTimeline.DerivePosition(ticked.Position, now: 4000)).IsEqualTo(3);
	}

	[Test]
	public async Task DoesNotAdvanceWhilePaused()
	{
		var playing = PlaybackTimeline.Play(TimelineState.Idle, "song-1", now: 1000);
		var paused = PlaybackTimeline.Pause(playing, now: 4000);

		var ticked = PlaybackTimeline.Tick(paused, now: 9000);

		await Assert.That(ticked).IsEqualTo(paused);
	}
}
