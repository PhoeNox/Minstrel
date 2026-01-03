namespace Features.Tests.Playback;

using Features.GamePhases;
using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

public class SettingSongs
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};
	private readonly Song song3 = Dummies.Song with {Title = "Song3"};

	[Test]
	[MatrixDataSource]
	public async Task SettingSong(
			[Matrix(GamePhase.Day, GamePhase.Night)] GamePhase currentGamePhase,
			[Matrix(GamePhase.Day, GamePhase.Night)] GamePhase playlistToSetSong,
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

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(playlistToSetSong, [song1, song2, song3]));
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(currentGamePhase));
		if (isPlaying)
			AppContext.Dispatcher.Dispatch(new PlayAction());

		AppContext.Dispatcher.Dispatch(new SetCurrentSongAction(playlistToSetSong, song2, song1));

		await Assert.That(loadedSongs).Contains(song2);
		await Assert.That(pausedSong).IsEqualTo(song1);
		if (isPlaying && currentGamePhase == playlistToSetSong)
		{
			await Assert.That(playedSong).IsEqualTo(song2);
			await Assert.That(loadedSongs).Contains(song3);
		}
	}
}
