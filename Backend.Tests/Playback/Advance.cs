namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Advance
{
	private static TimelineState Playing(params (string Id, double Length)[] songs)
	{
		var playlist = new TimelinePlaylist(
			songs.Select(song => new TimelineSong(song.Id, song.Length)).ToArray(),
			CurrentIndex: 0);
		var state = TimelineState.Idle with { Day = playlist };
		return PlaybackTimeline.SelectSong(state, GamePhase.Day, index: 0, now: 1000);
	}

	[Test]
	public async Task AdvancesToTheNextSongWhenTheCurrentOneEnds()
	{
		var state = Playing(("song-1", 10), ("song-2", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 12000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(ticked.Day.CurrentIndex).IsEqualTo(1);
		await Assert.That(ticked.Position.Offset).IsEqualTo(0);
		await Assert.That(ticked.IsPlaying).IsTrue();
	}

	[Test]
	public async Task AdvancesByOccurrenceWhenTheSameSongAppearsTwice()
	{
		var state = Playing(("song-1", 10), ("dup", 10), ("dup", 10));
		var onFirstDup = PlaybackTimeline.SelectSong(state, GamePhase.Day, index: 1, now: 2000);

		var ticked = PlaybackTimeline.Tick(onFirstDup, now: 13000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("dup");
		await Assert.That(ticked.Day.CurrentIndex).IsEqualTo(2);

		var wrapped = PlaybackTimeline.Tick(ticked, now: 24000);

		await Assert.That(wrapped.Day.CurrentIndex).IsEqualTo(0);
		await Assert.That(wrapped.CurrentSongId).IsEqualTo("song-1");
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
		var onLast = PlaybackTimeline.SelectSong(state, GamePhase.Day, index: 1, now: 2000);

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

	[Test]
	public async Task ReportsAJumpWhenASingleSongLoops()
	{
		var state = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 12000);

		await Assert.That(PlaybackTimeline.PlaybackJumped(state, ticked, now: 12000)).IsTrue();
	}

	[Test]
	public async Task ReportsNoJumpWhileTheSongIsStillPlaying()
	{
		var state = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, now: 6000);

		await Assert.That(PlaybackTimeline.PlaybackJumped(state, ticked, now: 6000)).IsFalse();
	}
}
