using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Tests.Playback;

public class MovingSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }

	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};
	private readonly Song song4 = Dummies.Song with {Title = "Song4"};

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenMovingSong_AndNextSongHasMovedAway_LoadsNewNextSong(GamePhase gamePhase)
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3, song4]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		AppContext.Dispatcher.Dispatch(new SetCurrentSongAction(gamePhase, song1));
		AppContext.Dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 1, 3));

		await Assert.That(loadedSongs).Contains(song3);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenMovingSong_AndNextSongHasBeenInserted_LoadsNewNextSong(GamePhase gamePhase)
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3, song4]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		AppContext.Dispatcher.Dispatch(new SetCurrentSongAction(gamePhase, song1));
		AppContext.Dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 3, 1));

		await Assert.That(loadedSongs).Contains(song4);
	}

	[Test]
	[Arguments(GamePhase.Day)]
	[Arguments(GamePhase.Night)]
	public async Task WhenMovingSong_AndCurrentSongHasBeenMoved_LoadsNewNextSong(GamePhase gamePhase)
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(gamePhase, [song1, song2, song3, song4]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		AppContext.Dispatcher.Dispatch(new SetCurrentSongAction(gamePhase, song1));
		AppContext.Dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		AppContext.Dispatcher.Dispatch(new MoveSongAction(gamePhase, 0, 2));

		await Assert.That(loadedSongs).Contains(song4);
	}
}
