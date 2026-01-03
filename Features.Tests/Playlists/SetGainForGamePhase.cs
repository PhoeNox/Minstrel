namespace Features.Tests.Playlists;

using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

public class SetGainForGamePhase
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task SetGain(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetGainAction(gamePhase, 0.5f));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Gain).IsEqualTo(0.5f);
	}
	
	[Test]
	public async Task WhenChangingGainForCurrentGamePhase_UpdatesCurrentSongGain()
	{
		(Song song, float gain) requestedSongGain = new(Dummies.Song, 1);
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<SetGainSignal>(this, action => requestedSongGain = (action.Song, action.Gain));
		
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Day, [Dummies.Song]));
		AppContext.Dispatcher.Dispatch(new PlayAction());
		
		AppContext.Dispatcher.Dispatch(new SetGainAction(GamePhase.Day, 0.5f));
		
		await Assert.That(requestedSongGain).IsEqualTo((Dummies.Song, 0.5f));
	}
}
