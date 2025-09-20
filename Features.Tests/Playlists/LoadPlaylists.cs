namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Testing.Platform.Services;

[UseAppContext]
public class LoadPlaylists
{
	private readonly AppContext appContext;
	private readonly IDispatcher dispatcher;

	public LoadPlaylists(AppContext appContext, IDispatcher dispatcher)
	{
		this.appContext = appContext;
		this.dispatcher = dispatcher;
		
		appContext.PlaylistLoader
			.Setup(x => x.LoadPlaylists())
			.ReturnsAsync(([Dummies.Song], [Dummies.Song]));
	}

	[Test]
	public async Task AcceptanceTest()
	{
		dispatcher.Dispatch(new LoadPlaylistsAction());
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist).IsEquivalentTo(Dummies.Playlist);
		await Assert.That(state.Value.NightPlaylist).IsEquivalentTo(Dummies.Playlist);
	}

	[Test]
	public async Task DoesNotAffectPlaylistGains()
	{
		dispatcher.Dispatch(new SetDayGainAction(0.5f));
		
		dispatcher.Dispatch(new LoadPlaylistsAction());
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
	}
	
	[Test]
	public async Task WhenCurrentSongIsNotInNewPlaylist_SetsCurrentSongToFirstSong()
	{
		var oldSong = Dummies.Song with {Title = "Old Song"};
		dispatcher.Dispatch(new SetPlaylistsAction([oldSong], [Dummies.Song]));
		
		dispatcher.Dispatch(new LoadPlaylistsAction());
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.CurrentSong).IsEqualTo(Dummies.Song);
	}
}
