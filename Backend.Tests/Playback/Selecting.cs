namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Selecting
{
	private static TimelineState WithPlaylists()
	{
		var day = new TimelinePlaylist(
			[TimelineFixtures.Song("day-1", 10), TimelineFixtures.Song("day-2", 10)],
			CurrentIndex: 0);
		var night = new TimelinePlaylist(
			[TimelineFixtures.Song("night-1", 10), TimelineFixtures.Song("night-2", 10)],
			CurrentIndex: 0);
		return TimelineState.Idle with { Day = day, Night = night };
	}

	[Test]
	public async Task SelectingInTheActivePhaseStartsPlayingThatSong()
	{
		var state = WithPlaylists();

		var result = PlaybackTimeline.SelectSong(state, GamePhase.Day, index: 1, now: 1000);

		await Assert.That(result.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(result.Day.CurrentIndex).IsEqualTo(1);
		await Assert.That(result.IsPlaying).IsTrue();
		await Assert.That(result.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task SelectingInTheInactivePhaseUpdatesItsCurrentSongWithoutSounding()
	{
		var state = WithPlaylists();

		var result = PlaybackTimeline.SelectSong(state, GamePhase.Night, index: 1, now: 1000);

		await Assert.That(result.Night.CurrentIndex).IsEqualTo(1);
		await Assert.That(result.CurrentSongId).IsNull();
		await Assert.That(result.IsPlaying).IsFalse();
	}
}
