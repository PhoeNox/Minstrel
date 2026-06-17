namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Advance
{
	private static (Track[] Tracks, PlaybackState State) Playing(params (string Id, double Length)[] songs)
	{
		var tracks = Fixtures.Tracks(songs);
		var state = PlaybackTimeline.Select(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0) },
			GamePhase.Day,
			index: 0,
			tracks[0],
			now: 1000);
		return (tracks, state);
	}

	[Test]
	public async Task AdvancesToTheNextSongWhenTheCurrentOneEnds()
	{
		var (tracks, state) = Playing(("song-1", 10), ("song-2", 10));

		var ticked = PlaybackTimeline.Tick(state, tracks, now: 12000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-2");
		await Assert.That(ticked.Day.Cursor).IsEqualTo(1);
		await Assert.That(ticked.Position.Offset).IsEqualTo(0);
		await Assert.That(ticked.IsPlaying).IsTrue();
	}

	[Test]
	public async Task AdvancesByOccurrenceWhenTheSameSongAppearsTwice()
	{
		var (tracks, state) = Playing(("song-1", 10), ("dup", 10), ("dup", 10));
		var onFirstDup = PlaybackTimeline.Select(state, GamePhase.Day, index: 1, tracks[1], now: 2000);

		var ticked = PlaybackTimeline.Tick(onFirstDup, tracks, now: 13000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("dup");
		await Assert.That(ticked.Day.Cursor).IsEqualTo(2);

		var wrapped = PlaybackTimeline.Tick(ticked, tracks, now: 24000);

		await Assert.That(wrapped.Day.Cursor).IsEqualTo(0);
		await Assert.That(wrapped.CurrentSongId).IsEqualTo("song-1");
	}

	[Test]
	public async Task DoesNotAdvanceBeforeTheCurrentSongEnds()
	{
		var (tracks, state) = Playing(("song-1", 10), ("song-2", 10));

		var ticked = PlaybackTimeline.Tick(state, tracks, now: 6000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(ticked.Position.Offset).IsEqualTo(5);
	}

	[Test]
	public async Task WrapsToTheFirstSongAtTheEndOfThePlaylist()
	{
		var (tracks, state) = Playing(("song-1", 10), ("song-2", 10));
		var onLast = PlaybackTimeline.Select(state, GamePhase.Day, index: 1, tracks[1], now: 2000);

		var ticked = PlaybackTimeline.Tick(onLast, tracks, now: 13000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
	}

	[Test]
	public async Task WrapsToTheSameSongWhenThePlaylistHasOneSong()
	{
		var (tracks, state) = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, tracks, now: 12000);

		await Assert.That(ticked.CurrentSongId).IsEqualTo("song-1");
		await Assert.That(ticked.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task ReportsAJumpWhenASingleSongLoops()
	{
		var (tracks, state) = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, tracks, now: 12000);

		await Assert.That(PlaybackTimeline.PlaybackJumped(state, ticked, now: 12000)).IsTrue();
	}

	[Test]
	public async Task ReportsNoJumpWhileTheSongIsStillPlaying()
	{
		var (tracks, state) = Playing(("song-1", 10));

		var ticked = PlaybackTimeline.Tick(state, tracks, now: 6000);

		await Assert.That(PlaybackTimeline.PlaybackJumped(state, ticked, now: 6000)).IsFalse();
	}
}
