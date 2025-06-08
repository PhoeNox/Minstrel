namespace Features.Tests.Playback;

using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playback.State;

[UseAppContext]
public class Play
{
	private readonly AppContext appContext;
	private readonly IDispatcher dispatcher;
	
	private readonly Song daySong = Dummies.Song with {Title = "Song Of The Day"};
	private readonly Song nightSong = Dummies.Song with {Title = "Song Of The Night"};
	private readonly Playlist dayPlaylist;
	private readonly Playlist nightPlaylist;
	
	public Play(AppContext appContext, IDispatcher dispatcher)
	{
		this.appContext = appContext;
		this.dispatcher = dispatcher;
		
		dayPlaylist = new Playlist([daySong]);
		nightPlaylist = new Playlist([nightSong]);
	}
	
	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenNothingIsPlaying_PlaysTheFirstSongOfTheCurrentPlaylist(GamePhase gamePhase)
	{
		var requestedSong = Dummies.Song; 
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<PlaySignal>(this, action => requestedSong = action.Song);

		dispatcher.Dispatch(new SetPlaylistsAction(dayPlaylist, nightPlaylist));
		dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		
		dispatcher.Dispatch(new PlayAction());

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.IsPlaying).IsTrue();
		if (gamePhase == GamePhase.Day)
		{
			await Assert.That(state.Value.PlayingSongs.Single().Song.Title).IsEqualTo("Song Of The Day");
			await Assert.That(requestedSong).IsEqualTo(daySong);
		}
		else
		{
			await Assert.That(state.Value.PlayingSongs.Single().Song.Title).IsEqualTo("Song Of The Night");
			await Assert.That(requestedSong).IsEqualTo(nightSong);
		}
	}
}
