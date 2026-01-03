namespace Features.Tests.Playback;

using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playback.State;

public class SwitchGamePhase
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	private readonly Song daySong = Dummies.Song with {Title = "Song Of The Day"};
	private readonly Song nightSong = Dummies.Song with {Title = "Song Of The Night"};

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task PlaysSongFromOtherPlaylist(GamePhase gamePhase)
	{
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		var requestedSongToBePlayed = Dummies.Song; 
		actionSubscriber.SubscribeToAction<PlaySongSignal>(this, action => requestedSongToBePlayed = action.Song);
		var requestedSongToBePaused = Dummies.Song;
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this, action => requestedSongToBePaused = action.Song);
		
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Day, [daySong]));
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Night, [nightSong]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		AppContext.Dispatcher.Dispatch(new PlayAction());
		
		AppContext.Dispatcher.Dispatch(new SwitchGamePhaseAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.PlayingSongs)
			.Count().IsEqualTo(2)
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
