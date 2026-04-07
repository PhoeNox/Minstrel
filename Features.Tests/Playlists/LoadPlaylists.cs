namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Testing.Platform.Services;

public class LoadPlaylists
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	[Before(Test)]
	public void MockFileSystem()
	{
		AppContext.FileSystemProvider
				.LoadPlaylists()
				.Returns(([Dummies.Song], [Dummies.Song]));
	}

	[Test]
	public async Task AcceptanceTest()
	{
		AppContext.Dispatcher.Dispatch(new LoadPlaylistsAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist).IsEquivalentTo(Dummies.Playlist);
		await Assert.That(state.Value.NightPlaylist).IsEquivalentTo(Dummies.Playlist);
	}

	[Test]
	public async Task DoesNotAffectPlaylistGains()
	{
		AppContext.Dispatcher.Dispatch(new SetGainAction(GamePhase.Day, 0.5f));
		AppContext.Dispatcher.Dispatch(new SetGainAction(GamePhase.Night, 0.6f));

		AppContext.Dispatcher.Dispatch(new LoadPlaylistsAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
		await Assert.That(state.Value.NightPlaylist.Gain).IsEqualTo(0.6f);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenCurrentSongIsNotInNewPlaylist_SetsCurrentSongToFirstSong(GamePhase gamePhase)
	{
		var oldSong = Dummies.Song with {Title = "Old Song"};
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [oldSong]));

		AppContext.Dispatcher.Dispatch(new LoadPlaylistsAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).CurrentSong).IsEqualTo(Dummies.Song);
	}
}
