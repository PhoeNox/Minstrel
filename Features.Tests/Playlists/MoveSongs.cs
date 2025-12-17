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
	public async Task MoveDaySong_MovesForward()
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3], []));
		AppContext.Dispatcher.Dispatch(new MoveDaySongAction(0, 2));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Songs).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	public async Task MoveDaySong_MovesBackward()
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3], []));
		AppContext.Dispatcher.Dispatch(new MoveDaySongAction(2, 0));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Songs).IsEquivalentTo([song3, song1, song2]);
	}

	[Test]
	public async Task MoveNightSong_MovesForward()
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new MoveNightSongAction(0, 2));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Songs).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	public async Task MoveNightSong_MovesBackward()
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new MoveNightSongAction(2, 0));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Songs).IsEquivalentTo([song3, song1, song2]);
	}
}
