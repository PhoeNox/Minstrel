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
				.Setup(x => x.LoadPlaylists())
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
		AppContext.Dispatcher.Dispatch(new SetDayGainAction(0.5f));

		AppContext.Dispatcher.Dispatch(new LoadPlaylistsAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
	}

	[Test]
	public async Task WhenCurrentSongIsNotInNewPlaylist_SetsCurrentSongToFirstSong()
	{
		var oldSong = Dummies.Song with {Title = "Old Song"};
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Day, [oldSong]));

		AppContext.Dispatcher.Dispatch(new LoadPlaylistsAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.CurrentSong).IsEqualTo(Dummies.Song);
	}
}
