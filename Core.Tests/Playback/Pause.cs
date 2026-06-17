namespace Core.Tests.Playback;

using Core.Playback;

public class Pause
{
	[Test]
	public async Task FreezesTheAnchorAtTheElapsedOffset()
	{
		var playing = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);

		var paused = PlaybackTimeline.Pause(playing, now: 4000);

		await Assert.That(paused.IsPlaying).IsFalse();
		await Assert.That(paused.Position.Offset).IsEqualTo(3);
	}

	[Test]
	public async Task FrozenPositionDoesNotAdvanceAsTimePasses()
	{
		var playing = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);
		var paused = PlaybackTimeline.Pause(playing, now: 4000);

		var laterPosition = PlaybackTimeline.DerivePosition(paused.Position, now: 9000);

		await Assert.That(laterPosition).IsEqualTo(3);
	}
}
