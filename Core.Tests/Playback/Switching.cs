namespace Core.Tests.Playback;

using Core.Playback;
using Core;
using Core.Playlist;

public class Switching
{
	private static PlaylistBook WithPlaylists() =>
		Fixtures.Book(
			[Fixtures.Entry("day-1", 10), Fixtures.Entry("day-2", 10)],
			[Fixtures.Entry("night-1", 10), Fixtures.Entry("night-2", 10)]);

	private static PlaybackState Started() =>
		PlaybackTimeline.Play(
			PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0), Night = new PhasePlayback(Cursor: 0) },
			"day-1",
			now: 1000);

	[Test]
	public async Task SwitchingFlipsThePhaseAndPlaysTheOtherPlaylistsCurrentSong()
	{
		var result = Fixtures.Switch(WithPlaylists(), Started(), now: 2000);

		await Assert.That(result.ActivePhase).IsEqualTo(GamePhase.Night);
		await Assert.That(result.CurrentSongId).IsEqualTo("night-1");
		await Assert.That(result.IsPlaying).IsTrue();
		await Assert.That(result.Position.Offset).IsEqualTo(0);
		await Assert.That(result.Position.AnchorTimestamp).IsEqualTo(2000);
	}

	[Test]
	public async Task SwitchingBackReturnsToTheStartingPhasesCurrentSong()
	{
		var book = WithPlaylists();

		var switched = Fixtures.Switch(book, Started(), now: 2000);
		var back = Fixtures.Switch(book, switched, now: 3000);

		await Assert.That(back.ActivePhase).IsEqualTo(GamePhase.Day);
		await Assert.That(back.CurrentSongId).IsEqualTo("day-1");
	}

	[Test]
	public async Task SwitchingBackResumesTheSongAtThePositionLeftPlusTheFadeout()
	{
		var book = WithPlaylists();

		var switched = Fixtures.Switch(book, Started(), now: 4000);
		var back = Fixtures.Switch(book, switched, now: 9000);

		await Assert.That(back.CurrentSongId).IsEqualTo("day-1");
		await Assert.That(back.Position.Offset).IsEqualTo(3 + PlaybackTimeline.FadeSeconds);
		await Assert.That(back.Position.AnchorTimestamp).IsEqualTo(9000);
		await Assert.That(back.Position.IsPlaying).IsTrue();
	}

	[Test]
	public async Task ResumeOffsetIsCappedAtTheSongLength()
	{
		var book = WithPlaylists();

		var switched = Fixtures.Switch(book, Started(), now: 9000);
		var back = Fixtures.Switch(book, switched, now: 12000);

		await Assert.That(back.Position.Offset).IsEqualTo(10);
	}

	[Test]
	public async Task SelectingAnotherSongInTheIdlePhaseResetsItsResumeOffset()
	{
		var book = WithPlaylists();

		var switched = Fixtures.Switch(book, Started(), now: 4000);
		var reselected = Fixtures.SelectIn(book, switched, GamePhase.Day, index: 1, now: 6000);
		var back = Fixtures.Switch(book, reselected, now: 9000);

		await Assert.That(back.CurrentSongId).IsEqualTo("day-2");
		await Assert.That(back.Position.Offset).IsEqualTo(0);
	}

	[Test]
	public async Task SwitchingIsANoOpWhenTheOtherPlaylistHasNoCurrentSong()
	{
		var book = Fixtures.Book([Fixtures.Entry("day-1", 10), Fixtures.Entry("day-2", 10)], []);
		var state = PlaybackTimeline.Play(PlaybackState.Idle with { Day = new PhasePlayback(Cursor: 0) }, "day-1", now: 1000);

		var result = Fixtures.Switch(book, state, now: 2000);

		await Assert.That(result).IsEqualTo(state);
	}
}
