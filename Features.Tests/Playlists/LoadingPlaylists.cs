namespace Features.Tests.Playlists;

using Features.Playlists;
using Fluxor;
using Microsoft.Testing.Platform.Services;

public class LoadingPlaylists
{
	[Test]
	public async Task AcceptanceTest()
	{
		var testContext = new TestContext();
		var dispatcher = testContext.Services.GetRequiredService<IDispatcher>();
		testContext.PlaylistLoader
			.Setup(x => x.LoadPlaylists())
			.ReturnsAsync(([Dummies.Song], [Dummies.Song]));
		
		dispatcher.Dispatch(new LoadPlaylistsAction());
		
		var state = testContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist).IsEquivalentTo(Dummies.Playlist);
		await Assert.That(state.Value.NightPlaylist).IsEquivalentTo(Dummies.Playlist);
	}
}
