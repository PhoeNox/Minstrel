using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Tests.Playback;

[UseAppContext]
public class MovingSongs(AppContext appContext, IDispatcher dispatcher)
{
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};
	private readonly Song song4 = Dummies.Song with {Title = "Song4"};

	[Test]
	public async Task WhenMovingDaySong_AndNextSongHasMovedAway_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
			action => loadedSongs.Add(action.Song));

		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3, song4], []));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Day));
		dispatcher.Dispatch(new SetCurrentSongForDayAction(song1));
		dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveDaySongAction(1, 3));

		await Assert.That(loadedSongs).Contains(song3);
	}
	
	[Test]
	public async Task WhenMovingDaySong_AndNextSongHasBeenInserted_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));

		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3, song4], []));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Day));
		dispatcher.Dispatch(new SetCurrentSongForDayAction(song1));
		dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveDaySongAction(3, 1));

		await Assert.That(loadedSongs).Contains(song4);
	}
	
	[Test]
	public async Task WhenMovingDaySong_AndCurrentSongHasBeenMoved_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));
		
		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3, song4], []));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Day));
		dispatcher.Dispatch(new SetCurrentSongForDayAction(song1));
		dispatcher.Dispatch(new PlayAction());
		
		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveDaySongAction(0, 2));
		
		await Assert.That(loadedSongs).Contains(song4);
	}

	[Test]
	public async Task WhenMovingNightSong_AndNextSongHasMovedAway_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
			action => loadedSongs.Add(action.Song));

		dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3, song4]));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Night));
		dispatcher.Dispatch(new SetCurrentSongForNightAction(song1));
		dispatcher.Dispatch(new PlayAction());

		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveNightSongAction(1, 3));

		await Assert.That(loadedSongs).Contains(song3);
	}
	
	[Test]
	public async Task WhenMovingNightSong_AndNextSongHasBeenInserted_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));
		
		dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3, song4]));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Night));
		dispatcher.Dispatch(new SetCurrentSongForNightAction(song1));
		dispatcher.Dispatch(new PlayAction());
		
		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveNightSongAction(3, 1));
		
		await Assert.That(loadedSongs).Contains(song4);
	}
	
	[Test]
	public async Task WhenMovingNightSong_AndCurrentSongHasBeenMoved_LoadsNewNextSong()
	{
		var loadedSongs = new List<Song>();
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));
		
		dispatcher.Dispatch(new SetPlaylistsAction([], [song1, song2, song3, song4]));
		dispatcher.Dispatch(new SetGamePhaseAction(GamePhase.Night));
		dispatcher.Dispatch(new SetCurrentSongForNightAction(song1));
		dispatcher.Dispatch(new PlayAction());
		
		loadedSongs.Clear();
		dispatcher.Dispatch(new MoveNightSongAction(0, 2));
		
		await Assert.That(loadedSongs).Contains(song4);
	}
}
