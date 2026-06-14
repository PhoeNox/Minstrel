namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Moving
{
	private static TimelineState WithDaySongs(params string[] ids)
	{
		var playlist = new TimelinePlaylist(
			ids.Select(id => new TimelineSong(id, 10)).ToArray(),
			CurrentSongId: ids[0]);
		return TimelineState.Idle with { Day = playlist };
	}

	private static string[] DayOrder(TimelineState state) =>
		state.Day.Songs.Select(song => song.Id).ToArray();

	[Test]
	public async Task ReordersSongsWithinThePlaylist()
	{
		var state = WithDaySongs("a", "b", "c");

		var moved = PlaybackTimeline.MoveSong(state, GamePhase.Day, oldIndex: 0, newIndex: 2);

		await Assert.That(DayOrder(moved)).IsEquivalentTo(new[] { "b", "c", "a" });
	}

	[Test]
	public async Task KeepsTheCurrentSongSelectionWhileReordering()
	{
		var state = WithDaySongs("a", "b", "c");

		var moved = PlaybackTimeline.MoveSong(state, GamePhase.Day, oldIndex: 1, newIndex: 0);

		await Assert.That(moved.Day.CurrentSongId).IsEqualTo("a");
	}

	[Test]
	public async Task IgnoresAnOutOfRangeSourceIndex()
	{
		var state = WithDaySongs("a", "b");

		var moved = PlaybackTimeline.MoveSong(state, GamePhase.Day, oldIndex: 5, newIndex: 0);

		await Assert.That(DayOrder(moved)).IsEquivalentTo(new[] { "a", "b" });
	}
}
