namespace Core.Tests.Playback;

using Core.Playback;
using Core;
using Playlist;

public class Moving
{
	private static PlaylistEntry[] DaySongs(params string[] ids) =>
		ids.Select(id => Fixtures.Entry(id, 10)).ToArray();

	private static PlaybackState PlayingAt(int cursor) =>
		PlaybackState.Idle with { Day = new PhasePlayback(Cursor: cursor) };

	[Test]
	public async Task ReordersSongsWithinThePlaylist()
	{
		var entries = DaySongs("a", "b", "c");

		var moved = Playlists.Move(entries, oldIndex: 0, newIndex: 2);

		await Assert.That(moved.Select(entry => entry.Id)).IsEquivalentTo(new[] { "b", "c", "a" });
	}

	[Test]
	public async Task KeepsTheCurrentEntrySelectionWhileReordering()
	{
		var entries = DaySongs("a", "b", "c");

		var moved = Playlists.Move(entries, oldIndex: 1, newIndex: 0);
		var reindexed = PlaybackTimeline.ReindexAfterMove(PlayingAt(0), GamePhase.Day, oldIndex: 1, newIndex: 0, length: entries.Length);

		await Assert.That(reindexed.Day.Cursor).IsEqualTo(1);
		await Assert.That(moved[reindexed.Day.Cursor!.Value].Id).IsEqualTo("a");
	}

	[Test]
	public async Task TracksTheCurrentEntryWhenItIsTheOneMoved()
	{
		var entries = DaySongs("a", "b", "c");

		var moved = Playlists.Move(entries, oldIndex: 0, newIndex: 2);
		var reindexed = PlaybackTimeline.ReindexAfterMove(PlayingAt(0), GamePhase.Day, oldIndex: 0, newIndex: 2, length: entries.Length);

		await Assert.That(reindexed.Day.Cursor).IsEqualTo(2);
		await Assert.That(moved[reindexed.Day.Cursor!.Value].Id).IsEqualTo("a");
	}

	[Test]
	public async Task IgnoresAnOutOfRangeSourceIndex()
	{
		var entries = DaySongs("a", "b");

		var moved = Playlists.Move(entries, oldIndex: 5, newIndex: 0);
		var reindexed = PlaybackTimeline.ReindexAfterMove(PlayingAt(0), GamePhase.Day, oldIndex: 5, newIndex: 0, length: entries.Length);

		await Assert.That(moved.Select(entry => entry.Id)).IsEquivalentTo(new[] { "a", "b" });
		await Assert.That(reindexed.Day.Cursor).IsEqualTo(0);
	}
}
