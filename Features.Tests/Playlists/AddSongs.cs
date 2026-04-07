namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

public class AddSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	private readonly Song song1 = Dummies.Song;

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task AddSong(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song1));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Songs).IsEquivalentTo([song1]);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public Task AddSong_SavesPlaylist(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song1));
		AppContext.FileSystemProvider.SavePlaylist(gamePhase, Any()).WasCalled();
		return Task.CompletedTask;
	}
}
