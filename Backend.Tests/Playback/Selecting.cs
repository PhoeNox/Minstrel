namespace Backend.Tests.Playback;

using Core;
using Features.Playback;

public class Selecting
{
	private static PlaybackState WithCursors() =>
		PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0), Night = new PhasePlayback(Cursor: 0) };

	[Test]
	public async Task SelectingInTheActivePhaseStartsPlayingThatSong()
	{
		var result = PlaybackTimeline.Select(WithCursors(), GamePhase.Day, index: 1, Fixtures.Track("day-2", 10), now: 1000);

		await Assert.That(result.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(result.Day.Cursor).IsEqualTo(1);
		await Assert.That(result.IsPlaying).IsTrue();
		await Assert.That(result.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task SelectingInTheInactivePhaseUpdatesItsCurrentSongWithoutSounding()
	{
		var result = PlaybackTimeline.Select(WithCursors(), GamePhase.Night, index: 1, Fixtures.Track("night-2", 10), now: 1000);

		await Assert.That(result.Night.Cursor).IsEqualTo(1);
		await Assert.That(result.CurrentSongId).IsNull();
		await Assert.That(result.IsPlaying).IsFalse();
	}
}
