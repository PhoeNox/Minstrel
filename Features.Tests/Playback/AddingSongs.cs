namespace Features.Tests.Playback;

using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

public class AddingSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenCurrentSongIsLastSong_AndAddingSong_LoadsAddedSong(GamePhase gamePhase)
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1]));
		AppContext.Dispatcher.Dispatch(new SetCurrentSongAction(gamePhase, song1));

		loadedSongs.Clear();
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song2));

		await Assert.That(loadedSongs).Contains(song2);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenPlaylistIsEmpty_AndAddingSong_LoadsAddedSong(GamePhase gamePhase)
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, []));

		loadedSongs.Clear();
		AppContext.Dispatcher.Dispatch(new AddSongAction(gamePhase, song1));

		await Assert.That(loadedSongs).Contains(song1);
	}
}
