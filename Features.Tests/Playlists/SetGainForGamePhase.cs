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
	public async Task SetDayGain()
	{
		AppContext.Dispatcher.Dispatch(new SetDayGainAction(0.5f));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
	}
	
	[Test]
	public async Task SetNightGain()
	{
		AppContext.Dispatcher.Dispatch(new SetNightGainAction(0.5f));
		
		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Gain).IsEqualTo(0.5f);
	}

	[Test]
	public async Task WhenChangingGainForCurrentGamePhase_UpdatesCurrentSongGain()
	{
		(Song song, float gain) requestedSongGain = new(Dummies.Song, 1);
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<SetGainSignal>(this, action => requestedSongGain = (action.Song, action.Gain));
		
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([Dummies.Song], []));
		AppContext.Dispatcher.Dispatch(new PlayAction());
		
		AppContext.Dispatcher.Dispatch(new SetDayGainAction(0.5f));
		
		await Assert.That(requestedSongGain).IsEqualTo((Dummies.Song, 0.5f));
	}
}
