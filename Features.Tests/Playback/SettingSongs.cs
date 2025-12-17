using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

namespace Features.Tests.Playback;

public class SettingSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};

	[Test]
	[MatrixDataSource]
	public async Task SettingSongForDay(
			[Matrix(GamePhase.Day, GamePhase.Night)] GamePhase gamePhase,
			[Matrix(true, false)] bool isPlaying)
	{
		var playedSong = Dummies.Song;
		var pausedSong = Dummies.Song;
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));
		actionSubscriber.SubscribeToAction<PlaySongSignal>(this,
				action => playedSong = action.Song);
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this,
				action => pausedSong = action.Song);

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([song1, song2, song3], [Dummies.Song]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		if (isPlaying)
			AppContext.Dispatcher.Dispatch(new PlayAction());

		AppContext.Dispatcher.Dispatch(new SetCurrentSongForDayAction(song2, song1));

		await Assert.That(loadedSongs).Contains(song2);
		await Assert.That(pausedSong).IsEqualTo(song1);
		if (isPlaying && gamePhase == GamePhase.Day)
		{
			await Assert.That(playedSong).IsEqualTo(song2);
			await Assert.That(loadedSongs).Contains(song3);
		}
	}

	[Test]
	[MatrixDataSource]
	public async Task SettingSongForNight(
			[Matrix(GamePhase.Day, GamePhase.Night)] GamePhase gamePhase,
			[Matrix(true, false)] bool isPlaying)
	{
		var playedSong = Dummies.Song;
		var pausedSong = Dummies.Song;
		var loadedSongs = new List<Song>();
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<LoadSongSignal>(this,
				action => loadedSongs.Add(action.Song));
		actionSubscriber.SubscribeToAction<PlaySongSignal>(this,
				action => playedSong = action.Song);
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this,
				action => pausedSong = action.Song);

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([Dummies.Song], [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		if (isPlaying)
			AppContext.Dispatcher.Dispatch(new PlayAction());

		AppContext.Dispatcher.Dispatch(new SetCurrentSongForNightAction(song2, song1));

		await Assert.That(loadedSongs).Contains(song2);
		await Assert.That(pausedSong).IsEqualTo(song1);
		if (isPlaying && gamePhase == GamePhase.Night)
		{
			await Assert.That(playedSong).IsEqualTo(song2);
			await Assert.That(loadedSongs).Contains(song3);
		}
	}
}
