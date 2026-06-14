namespace Backend.Tests.Playback;

using Backend.Playback;
using Core;

public class Switching
{
	private static TimelineState WithPlaylists()
	{
		var day = new TimelinePlaylist(
			[new TimelineSong("day-1", 10), new TimelineSong("day-2", 10)],
			CurrentSongId: "day-1");
		var night = new TimelinePlaylist(
			[new TimelineSong("night-1", 10), new TimelineSong("night-2", 10)],
			CurrentSongId: "night-1");
		return TimelineState.Idle with { Day = day, Night = night };
	}

	[Test]
	public async Task SwitchingFlipsThePhaseAndPlaysTheOtherPlaylistsCurrentSong()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);

		var result = PlaybackTimeline.SwitchPhase(state, now: 2000);

		await Assert.That(result.ActivePhase).IsEqualTo(GamePhase.Night);
		await Assert.That(result.CurrentSongId).IsEqualTo("night-1");
		await Assert.That(result.IsPlaying).IsTrue();
		await Assert.That(result.Position.Offset).IsEqualTo(0);
		await Assert.That(result.Position.AnchorTimestamp).IsEqualTo(2000);
	}

	[Test]
	public async Task SwitchingBackReturnsToTheStartingPhasesCurrentSong()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);

		var switched = PlaybackTimeline.SwitchPhase(state, now: 2000);
		var back = PlaybackTimeline.SwitchPhase(switched, now: 3000);

		await Assert.That(back.ActivePhase).IsEqualTo(GamePhase.Day);
		await Assert.That(back.CurrentSongId).IsEqualTo("day-1");
	}

	[Test]
	public async Task SwitchingBackResumesTheSongAtThePositionLeftPlusTheFadeout()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);

		var switched = PlaybackTimeline.SwitchPhase(state, now: 4000);
		var back = PlaybackTimeline.SwitchPhase(switched, now: 9000);

		await Assert.That(back.CurrentSongId).IsEqualTo("day-1");
		await Assert.That(back.Position.Offset).IsEqualTo(3 + PlaybackTimeline.FadeSeconds);
		await Assert.That(back.Position.AnchorTimestamp).IsEqualTo(9000);
		await Assert.That(back.Position.IsPlaying).IsTrue();
	}

	[Test]
	public async Task ResumeOffsetIsCappedAtTheSongLength()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);

		var switched = PlaybackTimeline.SwitchPhase(state, now: 9000);
		var back = PlaybackTimeline.SwitchPhase(switched, now: 12000);

		await Assert.That(back.Position.Offset).IsEqualTo(10);
	}

	[Test]
	public async Task SelectingAnotherSongInTheIdlePhaseResetsItsResumeOffset()
	{
		var state = PlaybackTimeline.Play(WithPlaylists(), "day-1", now: 1000);

		var switched = PlaybackTimeline.SwitchPhase(state, now: 4000);
		var reselected = PlaybackTimeline.SelectSong(switched, GamePhase.Day, "day-2", now: 6000);
		var back = PlaybackTimeline.SwitchPhase(reselected, now: 9000);

		await Assert.That(back.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(back.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task SwitchingIsANoOpWhenTheOtherPlaylistHasNoCurrentSong()
	{
		var state = PlaybackTimeline.Play(
			WithPlaylists() with { Night = TimelinePlaylist.Empty },
			"day-1",
			now: 1000);

		var result = PlaybackTimeline.SwitchPhase(state, now: 2000);

		await Assert.That(result).IsEqualTo(state);
	}
}
