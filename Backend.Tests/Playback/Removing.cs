namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Removing
{
	private static TimelineState ActivePlaying(int current, params string[] ids)
	{
		var playlist = new TimelinePlaylist(
			ids.Select(id => TimelineFixtures.Song(id, 100)).ToArray(),
			CurrentIndex: current);
		var state = TimelineState.Idle with { Day = playlist };
		return PlaybackTimeline.PlayAt(state, ids[current], offset: 30, now: 1000);
	}

	private static string[] DayOrder(TimelineState state) =>
		state.Day.Songs.Select(song => song.Id).ToArray();

	[Test]
	public async Task RemovingBeforeCurrentShiftsCurrentDownAndKeepsPlaying()
	{
		var state = ActivePlaying(2, "a", "b", "c");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 0, now: 2000);

		await Assert.That(DayOrder(removed)).IsEquivalentTo(new[] { "b", "c" });
		await Assert.That(removed.Day.CurrentIndex).IsEqualTo(1);
		await Assert.That(removed.CurrentSongId).IsEqualTo("c");
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task RemovingAfterCurrentLeavesPlaybackUnchanged()
	{
		var state = ActivePlaying(0, "a", "b", "c");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 2, now: 2000);

		await Assert.That(DayOrder(removed)).IsEquivalentTo(new[] { "a", "b" });
		await Assert.That(removed.Day.CurrentIndex).IsEqualTo(0);
		await Assert.That(removed.CurrentSongId).IsEqualTo("a");
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task RemovingCurrentStartsTheSlidInSongFromZero()
	{
		var state = ActivePlaying(1, "a", "b", "c");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 1, now: 2000);

		await Assert.That(DayOrder(removed)).IsEquivalentTo(new[] { "a", "c" });
		await Assert.That(removed.Day.CurrentIndex).IsEqualTo(1);
		await Assert.That(removed.CurrentSongId).IsEqualTo("c");
		await Assert.That(removed.Position.Offset).IsEqualTo(0);
		await Assert.That(removed.IsPlaying).IsTrue();
	}

	[Test]
	public async Task RemovingTheCurrentLastEntryWrapsToTheStart()
	{
		var state = ActivePlaying(2, "a", "b", "c");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 2, now: 2000);

		await Assert.That(DayOrder(removed)).IsEquivalentTo(new[] { "a", "b" });
		await Assert.That(removed.Day.CurrentIndex).IsEqualTo(0);
		await Assert.That(removed.CurrentSongId).IsEqualTo("a");
		await Assert.That(removed.Position.Offset).IsEqualTo(0);
		await Assert.That(removed.IsPlaying).IsTrue();
	}

	[Test]
	public async Task RemovingTheLastRemainingSongGoesIdle()
	{
		var state = ActivePlaying(0, "a");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 0, now: 2000);

		await Assert.That(removed.Day.Songs).IsEmpty();
		await Assert.That(removed.Day.CurrentIndex).IsNull();
		await Assert.That(removed.CurrentSongId).IsNull();
		await Assert.That(removed.IsPlaying).IsFalse();
	}

	[Test]
	public async Task RemovingFromTheInactivePhaseRepairsItsCurrentIndexWithoutTouchingPlayback()
	{
		var night = new TimelinePlaylist(
			new[] { "x", "y", "z" }.Select(id => TimelineFixtures.Song(id, 100)).ToArray(),
			CurrentIndex: 2);
		var day = new TimelinePlaylist([TimelineFixtures.Song("a", 100)], CurrentIndex: 0);
		var state = PlaybackTimeline.PlayAt(
			TimelineState.Idle with { Day = day, Night = night }, "a", offset: 30, now: 1000);

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Night, index: 0, now: 2000);

		await Assert.That(removed.Night.Songs.Select(song => song.Id)).IsEquivalentTo(new[] { "y", "z" });
		await Assert.That(removed.Night.CurrentIndex).IsEqualTo(1);
		await Assert.That(removed.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task IgnoresAnOutOfRangeIndex()
	{
		var state = ActivePlaying(0, "a", "b");

		var removed = PlaybackTimeline.RemoveSong(state, GamePhase.Day, index: 5, now: 2000);

		await Assert.That(removed).IsEqualTo(state);
	}
}
