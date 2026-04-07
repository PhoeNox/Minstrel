namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

public class MoveSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task MoveSong_MovesForward(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 0, 2));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Songs).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task MoveSong_MovesBackward(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 2, 0));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Songs).IsEquivalentTo([song3, song1, song2]);
	}
	
	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public Task MoveSong_SavesPlaylist(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 0, 2));
		
		AppContext.FileSystemProvider.SavePlaylist(gamePhase, Any()).WasCalled();
		return Task.CompletedTask;
	}
}
