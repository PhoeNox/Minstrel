namespace Backend.Tests.Playback;

using Backend.Playback;

public class Play
{
	[Test]
	public async Task SetsTheCurrentSongAndStartsPlaying()
	{
		var result = PlaybackTimeline.Play(TimelineState.Idle, "song-1");

		await Assert.That(result.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(result.IsPlaying).IsTrue();
	}

	[Test]
	public async Task ReplacesThePreviousCurrentSong()
	{
		var playing = PlaybackTimeline.Play(TimelineState.Idle, "song-1");

		var result = PlaybackTimeline.Play(playing, "song-2");

		await Assert.That(result.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(result.IsPlaying).IsTrue();
	}
}
