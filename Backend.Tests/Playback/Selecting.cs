namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Selecting
{
	private static TimelineState WithPlaylists()
	{
		var day = new TimelinePlaylist(
			[new TimelineSong("day-1", 10), new TimelineSong("day-2", 10)],
			CurrentSongId: "day-1");
		var night = new TimelinePlaylist(
			[new TimelineSong("night-1", 10), new TimelineSong("night-2", 10)],
			CurrentSongId: "night-1");
		return TimelineState.Idle with { Day = day, Night = night };
	}

	[Test]
	public async Task SelectingInTheActivePhaseStartsPlayingThatSong()
	{
		var state = WithPlaylists();

		var result = PlaybackTimeline.SelectSong(state, GamePhase.Day, "day-2", now: 1000);

		await Assert.That(result.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(result.Day.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(result.IsPlaying).IsTrue();
		await Assert.That(result.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task SelectingInTheInactivePhaseUpdatesItsCurrentSongWithoutSounding()
	{
		var state = WithPlaylists();

		var result = PlaybackTimeline.SelectSong(state, GamePhase.Night, "night-2", now: 1000);

		await Assert.That(result.Night.CurrentSongId).IsEqualTo("night-2");
		await Assert.That(result.CurrentSongId).IsNull();
		await Assert.That(result.IsPlaying).IsFalse();
	}
}
