namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

public class ShuffleSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	private readonly Song song1 = new("Path1", "Song1", "Artist", "Album", TimeSpan.FromMinutes(1));
	private readonly Song song2 = new("Path2", "Song2", "Artist", "Album", TimeSpan.FromMinutes(1));
	private readonly Song song3 = new("Path3", "Song3", "Artist", "Album", TimeSpan.FromMinutes(1));

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task ShufflePlaylist_KeepsAllSongs(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song1));
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song2));
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song3));

		AppContext.Dispatcher.Dispatch(new ShufflePlaylistAction(gamePhase));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Songs).IsEquivalentTo([song1, song2, song3]);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public Task ShufflePlaylist_SavesPlaylist(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song1));
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song2));
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song3));

		AppContext.Dispatcher.Dispatch(new ShufflePlaylistAction(gamePhase));

		AppContext.FileSystemProvider.Verify(x => x.SavePlaylist(
			gamePhase,
			It.Is<Song[]>(s => s.Length == 3)));
		return Task.CompletedTask;
	}
}
