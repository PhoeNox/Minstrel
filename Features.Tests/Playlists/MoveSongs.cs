namespace Features.Tests.Playlists;

using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.Playlists.State;

[UseAppContext]
public class MoveSongs(AppContext appContext, IDispatcher dispatcher)
{
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};

	[Test]
	public async Task MoveDaySong_MovesForward()
	{
		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3], []));
		dispatcher.Dispatch(new MoveDaySongAction(0, 2));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Songs).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	public async Task MoveDaySong_MovesBackward()
	{
		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3], []));
		dispatcher.Dispatch(new MoveDaySongAction(2, 0));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Songs).IsEquivalentTo([song3, song1, song2]);
	}

	[Test]
	public async Task MoveNightSong_MovesForward()
	{
		dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3]));
		dispatcher.Dispatch(new MoveNightSongAction(0, 2));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Songs).IsEquivalentTo([song2, song3, song1]);
	}

	[Test]
	public async Task MoveNightSong_MovesBackward()
	{
		dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3]));
		dispatcher.Dispatch(new MoveNightSongAction(2, 0));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Songs).IsEquivalentTo([song3, song1, song2]);
	}
}
