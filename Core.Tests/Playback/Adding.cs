namespace Core.Tests.Playback;

using Core.Playback;
using Core;

public class Adding
{
	[Test]
	public async Task LeavesTheCurrentEntryAndPositionUntouched()
	{
		var state = PlaybackTimeline.Play(PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0) }, "a", now: 1000);

		var reindexed = PlaybackTimeline.ReindexAfterAdd(state, GamePhase.Day);

		await Assert.That(reindexed.Day.Cursor).IsEqualTo(0);
		await Assert.That(reindexed.CurrentSongId).IsEqualTo("a");
		await Assert.That(reindexed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task SelectsTheFirstEntryWhenAddingToAnEmptyPlaylist()
	{
		var reindexed = PlaybackTimeline.ReindexAfterAdd(PlaybackState.Idle, GamePhase.Day);

		await Assert.That(reindexed.Day.Cursor).IsEqualTo(0);
	}
}
