namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Gain
{
	private static TimelineState WithPlaylists()
	{
		var day = new TimelinePlaylist([new TimelineSong("day-1", 10), new TimelineSong("day-2", 10)], CurrentIndex: 0);
		var night = new TimelinePlaylist([new TimelineSong("night-1", 10)], CurrentIndex: 0);
		return TimelineState.Idle with { Day = day, Night = night };
	}

	[Test]
	public async Task SetGainUpdatesTheTargetPhasesGain()
	{
		var result = PlaybackTimeline.SetGain(WithPlaylists(), GamePhase.Night, gain: 0.4);

		await Assert.That(result.Night.Gain).IsEqualTo(0.4);
	}

	[Test]
	public async Task SetGainLeavesTheOtherPhasesGainUnchanged()
	{
		var result = PlaybackTimeline.SetGain(WithPlaylists(), GamePhase.Night, gain: 0.4);

		await Assert.That(result.Day.Gain).IsEqualTo(1.0);
	}

	[Test]
	public async Task GainPersistsWhenTheSongAdvancesWithinThePhase()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);
		var quieter = PlaybackTimeline.SetGain(state, GamePhase.Day, gain: 0.3);

		var advanced = PlaybackTimeline.Tick(quieter, now: 1000 + 11_000);

		await Assert.That(advanced.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(advanced.Day.Gain).IsEqualTo(0.3);
	}
}
