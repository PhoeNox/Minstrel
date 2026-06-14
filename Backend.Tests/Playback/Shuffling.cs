namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Shuffling
{
	private static TimelineState ActivePlaying(int current, params string[] ids)
	{
		var playlist = new TimelinePlaylist(
			ids.Select(id => new TimelineSong(id, 100)).ToArray(),
			CurrentIndex: current);
		var state = TimelineState.Idle with { Day = playlist };
		return PlaybackTimeline.PlayAt(state, ids[current], offset: 30, now: 1000);
	}

	[Test]
	public async Task TracksTheCurrentEntryToItsNewIndexWithoutDisturbingPlayback()
	{
		var state = ActivePlaying(1, "a", "b", "c", "d", "e");

		var shuffled = PlaybackTimeline.Shuffle(state, GamePhase.Day, new Random(12345));

		await Assert.That(shuffled.Day.CurrentSong?.Id).IsEqualTo("b");
		await Assert.That(shuffled.CurrentSongId).IsEqualTo("b");
		await Assert.That(shuffled.Position).IsEqualTo(state.Position);
	}

	[Test]
	public async Task KeepsEveryEntry()
	{
		var state = ActivePlaying(0, "a", "b", "c", "d", "e");

		var shuffled = PlaybackTimeline.Shuffle(state, GamePhase.Day, new Random(1));

		await Assert.That(shuffled.Day.Songs.Select(song => song.Id))
			.IsEquivalentTo(new[] { "a", "b", "c", "d", "e" });
	}

	[Test]
	public async Task IsDeterministicForASeededRandom()
	{
		var state = ActivePlaying(0, "a", "b", "c", "d", "e");

		var first = PlaybackTimeline.Shuffle(state, GamePhase.Day, new Random(42));
		var second = PlaybackTimeline.Shuffle(state, GamePhase.Day, new Random(42));

		var firstOrder = string.Join(",", first.Day.Songs.Select(song => song.Id));
		var secondOrder = string.Join(",", second.Day.Songs.Select(song => song.Id));
		await Assert.That(firstOrder).IsEqualTo(secondOrder);
	}

	[Test]
	public async Task PreservesTheInactivePlaylistsCurrentEntry()
	{
		var night = new TimelinePlaylist(
			new[] { "x", "y", "z", "w" }.Select(id => new TimelineSong(id, 100)).ToArray(),
			CurrentIndex: 2);
		var day = new TimelinePlaylist([new TimelineSong("a", 100)], CurrentIndex: 0);
		var state = PlaybackTimeline.PlayAt(
			TimelineState.Idle with { Day = day, Night = night }, "a", offset: 30, now: 1000);

		var shuffled = PlaybackTimeline.Shuffle(state, GamePhase.Night, new Random(7));

		await Assert.That(shuffled.Night.CurrentSong?.Id).IsEqualTo("z");
		await Assert.That(shuffled.Position).IsEqualTo(state.Position);
	}
}
