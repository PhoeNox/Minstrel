namespace Core.Tests.Playback;

using Core.Playback;
using Core;

public class Shuffling
{
	private static PlaybackState ActivePlaying(int cursor, string songId) =>
		PlaybackTimeline.PlayAt(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: cursor) },
			songId,
			offset: 30,
			now: 1000);

	[Test]
	public async Task PinsTheActivePhasesCursor_WhenItsSongIsPlaying()
	{
		var state = ActivePlaying(1, "b");

		var pinned = PlaybackTimeline.CursorOfStartedSong(state, GamePhase.Day);

		await Assert.That(pinned).IsEqualTo(1);
	}

	[Test]
	public async Task PinsTheActivePhasesCursor_WhenItsSongIsPausedMidway()
	{
		var state = PlaybackTimeline.Pause(ActivePlaying(2, "c"), now: 2000);

		var pinned = PlaybackTimeline.CursorOfStartedSong(state, GamePhase.Day);

		await Assert.That(pinned).IsEqualTo(2);
	}

	[Test]
	public async Task PinsNothing_WhenTheActivePhaseNeverStartedASong()
	{
		var state = PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 1) };

		var pinned = PlaybackTimeline.CursorOfStartedSong(state, GamePhase.Day);

		await Assert.That(pinned).IsNull();
	}

	[Test]
	public async Task PinsTheInactivePhasesCursor_WhenItsSongWasStartedBeforeSwitchingAway()
	{
		var state = PlaybackState.Idle with
		{
			Night = new PhasePlayback(Cursor: 2, ResumeOffset: 35),
		};

		var pinned = PlaybackTimeline.CursorOfStartedSong(state, GamePhase.Night);

		await Assert.That(pinned).IsEqualTo(2);
	}

	[Test]
	public async Task PinsNothing_WhenTheInactivePhasesSongNeverStarted()
	{
		var state = PlaybackState.Idle with { Night = new PhasePlayback(Cursor: 2) };

		var pinned = PlaybackTimeline.CursorOfStartedSong(state, GamePhase.Night);

		await Assert.That(pinned).IsNull();
	}

	[Test]
	public async Task MovesTheCursorToTheFrontWithoutDisturbingPlayback()
	{
		var state = ActivePlaying(3, "d");

		var reindexed = PlaybackTimeline.ReindexAfterShuffle(state, GamePhase.Day);

		await Assert.That(reindexed.Day.Cursor).IsEqualTo(0);
		await Assert.That(reindexed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task MovesTheInactivePhasesCursorToTheFront()
	{
		var state = ActivePlaying(0, "a") with { Night = new PhasePlayback(Cursor: 2) };

		var reindexed = PlaybackTimeline.ReindexAfterShuffle(state, GamePhase.Night);

		await Assert.That(reindexed.Night.Cursor).IsEqualTo(0);
		await Assert.That(reindexed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task KeepsAMissingCursorMissing()
	{
		var state = PlaybackState.Idle;

		var reindexed = PlaybackTimeline.ReindexAfterShuffle(state, GamePhase.Day);

		await Assert.That(reindexed.Day.Cursor).IsNull();
	}
}
