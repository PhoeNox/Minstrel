namespace Core.Tests.Playback;

using Core.Playback;
using Core;
using Playlist;

public class Shuffling
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

	[Test]
	public async Task TracksTheCurrentEntryToItsNewIndexWithoutDisturbingPlayback()
	{
		var (entries, state) = ActivePlaying(1, "a", "b", "c", "d", "e");

		var (shuffled, permutation) = Playlists.Shuffle(entries, new Random(12345));
		var reindexed = PlaybackTimeline.ReindexAfterShuffle(state, GamePhase.Day, permutation);

		await Assert.That(shuffled[reindexed.Day.Cursor!.Value].Id).IsEqualTo("b");
		await Assert.That(reindexed.CurrentSongId).IsEqualTo("b");
		await Assert.That(reindexed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task KeepsEveryEntry()
	{
		var (entries, _) = ActivePlaying(0, "a", "b", "c", "d", "e");

		var (shuffled, _) = Playlists.Shuffle(entries, new Random(1));

		var entryIds = shuffled.Select(entry => entry.Id).ToArray();
		await Assert.That(entryIds).HasCount(5)
				.And.Contains("a")
				.And.Contains("b")
				.And.Contains("c")
				.And.Contains("d")
				.And.Contains("e");
	}

	[Test]
	public async Task IsDeterministicForASeededRandom()
	{
		var (entries, _) = ActivePlaying(0, "a", "b", "c", "d", "e");

		var (first, _) = Playlists.Shuffle(entries, new Random(42));
		var (second, _) = Playlists.Shuffle(entries, new Random(42));

		var firstOrder = string.Join(",", first.Select(entry => entry.Id));
		var secondOrder = string.Join(",", second.Select(entry => entry.Id));
		await Assert.That(firstOrder).IsEqualTo(secondOrder);
	}

	[Test]
	public async Task PreservesTheInactivePlaylistsCurrentEntry()
	{
		var night = new[] { "x", "y", "z", "w" }.Select(id => Fixtures.Entry(id, 100)).ToArray();
		var state = PlaybackTimeline.PlayAt(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0), Night = new PhasePlayback(Cursor: 2) },
			"a",
			offset: 30,
			now: 1000);

		var (shuffled, permutation) = Playlists.Shuffle(night, new Random(7));
		var reindexed = PlaybackTimeline.ReindexAfterShuffle(state, GamePhase.Night, permutation);

		await Assert.That(shuffled[reindexed.Night.Cursor!.Value].Id).IsEqualTo("z");
		await Assert.That(reindexed.Position).IsEqualTo(state.Position);
	}
}
