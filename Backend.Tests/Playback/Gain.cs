namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Gain
{
	private static PlaybackState WithCursors() =>
		PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0), Night = new PhasePlayback(Cursor: 0) };

	[Test]
	public async Task SetGainUpdatesTheTargetPhasesGain()
	{
		var result = PlaybackTimeline.SetGain(WithCursors(), GamePhase.Night, gain: 0.4);

		await Assert.That(result.Night.Gain).IsEqualTo(0.4);
	}

	[Test]
	public async Task SetGainLeavesTheOtherPhasesGainUnchanged()
	{
		var result = PlaybackTimeline.SetGain(WithCursors(), GamePhase.Night, gain: 0.4);

		await Assert.That(result.Day.Gain).IsEqualTo(1.0);
	}

	[Test]
	public async Task GainPersistsWhenTheSongAdvancesWithinThePhase()
	{
		var tracks = Fixtures.Tracks(("day-1", 10), ("day-2", 10));
		var state = PlaybackTimeline.Play(PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0) }, "day-1", now: 1000);
		var quieter = PlaybackTimeline.SetGain(state, GamePhase.Day, gain: 0.3);

		var advanced = PlaybackTimeline.Tick(quieter, tracks, now: 1000 + 11_000);

		await Assert.That(advanced.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(advanced.Day.Gain).IsEqualTo(0.3);
	}
}
