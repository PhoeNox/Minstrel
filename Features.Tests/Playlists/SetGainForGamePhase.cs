namespace Features.Tests.Playlists;

using Features.Playback;
using Features.Playlists;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

[UseAppContext]
public class SetGainForGamePhase(AppContext appContext, IDispatcher dispatcher)
{
	[Test]
	public async Task SetDayGain()
	{
		dispatcher.Dispatch(new SetDayGainAction(0.5f));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
	}
	
	[Test]
	public async Task SetNightGain()
	{
		dispatcher.Dispatch(new SetNightGainAction(0.5f));
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Gain).IsEqualTo(0.5f);
	}

	[Test]
	public async Task WhenChangingGainForCurrentGamePhase_UpdatesCurrentSongGain()
	{
		(Song song, float gain) requestedSongGain = new(Dummies.Song, 1);
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<SetGainSignal>(this, action => requestedSongGain = (action.Song, action.Gain));
		
		var dayPlaylist = new Playlist([Dummies.Song]);
		dispatcher.Dispatch(new SetPlaylistsAction(dayPlaylist, Dummies.Playlist));
		dispatcher.Dispatch(new PlayAction());
		
		dispatcher.Dispatch(new SetDayGainAction(0.5f));
		
		await Assert.That(requestedSongGain)!.IsEqualTo((dayPlaylist.CurrentSong, 0.5f));
	}
}
