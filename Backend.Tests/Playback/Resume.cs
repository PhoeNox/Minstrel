namespace Backend.Tests.Playback;

using Features.Playback;

public class Resume
{
	[Test]
	public async Task ContinuesFromTheFrozenOffset()
	{
		var playing = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);
		var paused = PlaybackTimeline.Pause(playing, now: 4000);

		var resumed = PlaybackTimeline.Resume(paused, now: 10000);

		await Assert.That(resumed.IsPlaying).IsTrue();
		await Assert.That(PlaybackTimeline.DerivePosition(resumed.Position, now: 12000)).IsEqualTo(5);
	}

	[Test]
	public async Task DoesNothingWhenNoSongIsCurrent()
	{
		var resumed = PlaybackTimeline.Resume(PlaybackState.Idle, now: 1000);

		await Assert.That(resumed.IsPlaying).IsFalse();
		await Assert.That(resumed.CurrentSongId).IsNull();
	}
}
