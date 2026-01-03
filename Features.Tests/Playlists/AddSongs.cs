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
		AppContext.Dispatcher.Dispatch(new AddSongToPlaylistAction(gamePhase, song1));

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		if (gamePhase == GamePhase.Day)
			await Assert.That(state.Value.DayPlaylist.Songs).IsEquivalentTo([song1]);
		else
			await Assert.That(state.Value.NightPlaylist.Songs).IsEquivalentTo([song1]);
	}
}
