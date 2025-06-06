namespace Features.Tests.Playlists;

using Features.Playlists;
using Fluxor;
using Microsoft.Testing.Platform.Services;

[UseAppContext]
public class LoadPlaylists(AppContext appContext, IDispatcher dispatcher)
{
	[Test]
	public async Task AcceptanceTest()
	{
		appContext.PlaylistLoader
			.Setup(x => x.LoadPlaylists())
			.ReturnsAsync(([Dummies.Song], [Dummies.Song]));
		
		dispatcher.Dispatch(new LoadPlaylistsAction());
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist).IsEquivalentTo(Dummies.Playlist);
		await Assert.That(state.Value.NightPlaylist).IsEquivalentTo(Dummies.Playlist);
	}
}
