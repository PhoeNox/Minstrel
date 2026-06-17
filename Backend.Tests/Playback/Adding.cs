namespace Backend.Tests.Playback;

using Core;
using Features.Playback;
using Features.Playlist;

public class Adding
{
	[Test]
	public async Task AppendsTheEntryToTheEndOfThePlaylist()
	{
		var entries = new[] { Fixtures.Entry("a", 10), Fixtures.Entry("b", 10) };

		var added = Playlists.Add(entries, Fixtures.Entry("c", 20));

		await Assert.That(added.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b", "c" });
		await Assert.That(added[^1].Length).IsEqualTo(20);
	}

	[Test]
	public async Task AllowsAddingTheSameEntryTwice()
	{
		var entries = new[] { Fixtures.Entry("a", 10) };

		var added = Playlists.Add(entries, Fixtures.Entry("a", 10));

		await Assert.That(added.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "a" });
	}

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
