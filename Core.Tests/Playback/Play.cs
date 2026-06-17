namespace Core.Tests.Playback;

using Backend.Features.Playback;

public class Play
{
	[Test]
	public async Task SetsTheCurrentSongAndStartsPlaying()
	{
		var result = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);

		await Assert.That(result.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(result.IsPlaying).IsTrue();
	}

	[Test]
	public async Task AnchorsTheNewSongAtOffsetZero()
	{
		var result = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);

		await Assert.That(result.Position.Offset).IsEqualTo(0);
		await Assert.That(result.Position.AnchorTimestamp).IsEqualTo(1000);
	}

	[Test]
	public async Task ReplacesThePreviousCurrentSong()
	{
		var playing = PlaybackTimeline.Play(PlaybackState.Idle, "song-1", now: 1000);

		var result = PlaybackTimeline.Play(playing, "song-2", now: 5000);

		await Assert.That(result.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(result.Position.Offset).IsEqualTo(0);
		await Assert.That(result.IsPlaying).IsTrue();
	}
}
