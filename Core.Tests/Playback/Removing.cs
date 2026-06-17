namespace Core.Tests.Playback;

using Core.Playback;
using Core;
using Core.Playlist;

public class Removing
{
	private static (PlaylistEntry[] Entries, PlaybackState State) ActivePlaying(int cursor, params string[] ids)
	{
		var entries = ids.Select(id => Fixtures.Entry(id, 100)).ToArray();
		var state = PlaybackTimeline.PlayAt(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: cursor) },
			ids[cursor],
			offset: 30,
			now: 1000);
		return (entries, state);
	}

	private static (PlaylistEntry[] Remaining, PlaybackState State) Remove(
		PlaylistEntry[] entries,
		PlaybackState state,
		GamePhase phase,
		int index,
		long now)
	{
		var remaining = Playlists.RemoveAt(entries, index);
		var reindexed = PlaybackTimeline.ReindexAfterRemove(state, phase, index, entries.Length, ToTracks(remaining), now);
		return (remaining, reindexed);
	}

	private static Track[] ToTracks(PlaylistEntry[] entries) =>
		entries.Select(entry => new Track(entry.Id, entry.Length)).ToArray();

	[Test]
	public async Task RemovingBeforeCurrentShiftsCurrentDownAndKeepsPlaying()
	{
		var (entries, state) = ActivePlaying(2, "a", "b", "c");

		var (remaining, removed) = Remove(entries, state, GamePhase.Day, index: 0, now: 2000);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "b", "c" });
		await Assert.That(removed.Day.Cursor).IsEqualTo(1);
		await Assert.That(removed.CurrentSongId).IsEqualTo("c");
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task RemovingAfterCurrentLeavesPlaybackUnchanged()
	{
		var (entries, state) = ActivePlaying(0, "a", "b", "c");

		var (remaining, removed) = Remove(entries, state, GamePhase.Day, index: 2, now: 2000);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b" });
		await Assert.That(removed.Day.Cursor).IsEqualTo(0);
		await Assert.That(removed.CurrentSongId).IsEqualTo("a");
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task RemovingCurrentStartsTheSlidInSongFromZero()
	{
		var (entries, state) = ActivePlaying(1, "a", "b", "c");

		var (remaining, removed) = Remove(entries, state, GamePhase.Day, index: 1, now: 2000);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "c" });
		await Assert.That(removed.Day.Cursor).IsEqualTo(1);
		await Assert.That(removed.CurrentSongId).IsEqualTo("c");
		await Assert.That(removed.Position.Offset).IsEqualTo(0);
		await Assert.That(removed.IsPlaying).IsTrue();
	}

	[Test]
	public async Task RemovingTheCurrentLastEntryWrapsToTheStart()
	{
		var (entries, state) = ActivePlaying(2, "a", "b", "c");

		var (remaining, removed) = Remove(entries, state, GamePhase.Day, index: 2, now: 2000);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b" });
		await Assert.That(removed.Day.Cursor).IsEqualTo(0);
		await Assert.That(removed.CurrentSongId).IsEqualTo("a");
		await Assert.That(removed.Position.Offset).IsEqualTo(0);
		await Assert.That(removed.IsPlaying).IsTrue();
	}

	[Test]
	public async Task RemovingTheLastRemainingSongGoesIdle()
	{
		var (entries, state) = ActivePlaying(0, "a");

		var (remaining, removed) = Remove(entries, state, GamePhase.Day, index: 0, now: 2000);

		await Assert.That(remaining).IsEmpty();
		await Assert.That(removed.Day.Cursor).IsNull();
		await Assert.That(removed.CurrentSongId).IsNull();
		await Assert.That(removed.IsPlaying).IsFalse();
	}

	[Test]
	public async Task RemovingFromTheInactivePhaseRepairsItsCurrentIndexWithoutTouchingPlayback()
	{
		var night = new[] { "x", "y", "z" }.Select(id => Fixtures.Entry(id, 100)).ToArray();
		var state = PlaybackTimeline.PlayAt(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0), Night = new PhasePlayback(Cursor: 2) },
			"a",
			offset: 30,
			now: 1000);

		var (remaining, removed) = Remove(night, state, GamePhase.Night, index: 0, now: 2000);

		await Assert.That(remaining.Select(entry => entry.Id)).IsEquivalentTo(new[] { "y", "z" });
		await Assert.That(removed.Night.Cursor).IsEqualTo(1);
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task IgnoresAnOutOfRangeIndex()
	{
		var (entries, state) = ActivePlaying(0, "a", "b");

		var (_, removed) = Remove(entries, state, GamePhase.Day, index: 5, now: 2000);

		await Assert.That(removed).IsEqualTo(state);
	}
}
