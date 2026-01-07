namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

public class RemoveSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	private readonly Song song1 = Dummies.Song;
	private readonly Song song2 = Dummies.Song;

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task RemoveSong(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2]));
		
		AppContext.Dispatcher.Dispatch(new RemoveSongAction(gamePhase, song1));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.GetPlaylist(gamePhase).Songs).IsEquivalentTo([song2]);
	}
	
	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public Task RemoveSong_SavesPlaylist(GamePhase gamePhase)
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2]));
		
		AppContext.Dispatcher.Dispatch(new RemoveSongAction(gamePhase, song1));
		
		AppContext.FileSystemProvider.Verify(x => x.SavePlaylist(gamePhase, new[] {song2}));
		return Task.CompletedTask;
	}
}
