namespace Features.Tests.Playback;

using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playback.State;

[UseAppContext]
public class SwitchGamePhase(AppContext appContext, IDispatcher dispatcher)
{
	private readonly Song daySong = Dummies.Song with {Title = "Song Of The Day"};
	private readonly Song nightSong = Dummies.Song with {Title = "Song Of The Night"};

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task PlaysSongFromOtherPlaylist(GamePhase gamePhase)
	{
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		var requestedSongToBePlayed = Dummies.Song; 
		actionSubscriber.SubscribeToAction<PlaySongSignal>(this, action => requestedSongToBePlayed = action.Song);
		var requestedSongToBePaused = Dummies.Song;
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this, action => requestedSongToBePaused = action.Song);
		
		dispatcher.Dispatch(new SetPlaylistsAction([daySong], [nightSong]));
		dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		dispatcher.Dispatch(new PlayAction());
		
		dispatcher.Dispatch(new SwitchGamePhaseAction());

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.PlayingSongs)
			.HasCount(2)
			.And.Contains(new PlayingSong(daySong, 1))
			.And.Contains(new PlayingSong(nightSong, 1));
		if (gamePhase == GamePhase.Day)
		{
			await Assert.That(requestedSongToBePlayed).IsEqualTo(nightSong);
			await Assert.That(requestedSongToBePaused).IsEqualTo(daySong);
		}
		else
		{
			await Assert.That(requestedSongToBePlayed).IsEqualTo(daySong);
			await Assert.That(requestedSongToBePaused).IsEqualTo(nightSong);
		}
	}
}
