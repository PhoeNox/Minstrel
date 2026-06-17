namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Adding
{
	private static TimelineState WithDaySongs(params string[] ids)
	{
		var playlist = new TimelinePlaylist(
			ids.Select(id => TimelineFixtures.Song(id, 10)).ToArray(),
			CurrentIndex: 0);
		return TimelineState.Idle with { Day = playlist };
	}

	private static string[] DayOrder(TimelineState state) =>
		state.Day.Songs.Select(song => song.Id).ToArray();

	[Test]
	public async Task AppendsTheSongToTheEndOfThePlaylist()
	{
		var state = WithDaySongs("a", "b");

		var added = PlaybackTimeline.AddSong(state, GamePhase.Day, TimelineFixtures.Song("c", 20));

		await Assert.That(DayOrder(added)).IsEquivalentTo(new[] { "a", "b", "c" });
		await Assert.That(added.Day.Songs[^1].Length).IsEqualTo(20);
	}

	[Test]
	public async Task AllowsAddingTheSameSongTwice()
	{
		var state = WithDaySongs("a");

		var added = PlaybackTimeline.AddSong(state, GamePhase.Day, TimelineFixtures.Song("a", 10));

		await Assert.That(DayOrder(added)).IsEquivalentTo(new[] { "a", "a" });
	}

	[Test]
	public async Task LeavesTheCurrentSongAndPositionUntouched()
	{
		var state = PlaybackTimeline.Play(WithDaySongs("a", "b"), "a", now: 1000);

		var added = PlaybackTimeline.AddSong(state, GamePhase.Day, TimelineFixtures.Song("c", 30));

		await Assert.That(added.Day.CurrentIndex).IsEqualTo(0);
		await Assert.That(added.CurrentSongId).IsEqualTo("a");
		await Assert.That(added.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task SelectsTheFirstSongWhenAddingToAnEmptyPlaylist()
	{
		var added = PlaybackTimeline.AddSong(TimelineState.Idle, GamePhase.Day, TimelineFixtures.Song("a", 10));

		await Assert.That(added.Day.CurrentIndex).IsEqualTo(0);
	}
}
