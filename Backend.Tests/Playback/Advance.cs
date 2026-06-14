namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Advance
{
	private static TimelineState Playing(params (string Id, double Length)[] songs)
	{
		var playlist = new TimelinePlaylist(
			songs.Select(song => new TimelineSong(song.Id, song.Length)).ToArray(),
			CurrentSongId: songs[0].Id);
		var state = TimelineState.Idle with { Day = playlist };
		return PlaybackTimeline.SelectSong(state, GamePhase.Day, songs[0].Id, now: 1000);
	}

	[Test]
	public async Task AdvancesToTheNextSongWhenTheCurrentOneEnds()
	{
		var state = Playing(("song-1", 10), ("song-2", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 12000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(ticked.Day.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(ticked.Position.Offset).IsEqualTo(0);
		await Assert.That(ticked.IsPlaying).IsTrue();
	}

	[Test]
	public async Task DoesNotAdvanceBeforeTheCurrentSongEnds()
	{
		var state = Playing(("song-1", 10), ("song-2", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 6000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(ticked.Position.Offset).IsEqualTo(5);
	}

	[Test]
	public async Task WrapsToTheFirstSongAtTheEndOfThePlaylist()
	{
		var state = Playing(("song-1", 10), ("song-2", 10));
		var onLast = PlaybackTimeline.SelectSong(state, GamePhase.Day, "song-2", now: 2000);

		var ticked = PlaybackTimeline.Tick(onLast, now: 13000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
	}

	[Test]
	public async Task WrapsToTheSameSongWhenThePlaylistHasOneSong()
	{
		var state = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 12000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(ticked.Position.Offset).IsEqualTo(0);
	}
}
