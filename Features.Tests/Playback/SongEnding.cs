namespace Features.Tests.Playback;

using Features.Playback;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;

public class SongEnding
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};

	[Test]
	public async Task ResetsCurrentSong()
	{
		var requestedSongToBeReset = Dummies.Song; 
		var actionSubscriber = AppContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this, action =>
		{
			if (action.Reset)
				requestedSongToBeReset = action.Song;
		});

		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([song1, song2], []));
		AppContext.Dispatcher.Dispatch(new PlayAction());
		
		AppContext.Dispatcher.Dispatch(new SongEndedSignal(song1));

		await Assert.That(requestedSongToBeReset).IsEqualTo(song1);
	}

	[Test]
	public async Task PlaysNextSong()
	{
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction([song1, song2], []));
		AppContext.Dispatcher.Dispatch(new PlayAction());
		
		AppContext.Dispatcher.Dispatch(new SongEndedSignal(song1));

		var playbackState = AppContext.Services.GetRequiredService<IState<Features.Playback.State>>();
		await Assert.That(playbackState.Value.PlayingSongs).Count().IsEqualTo(1)
			.And.Contains(new PlayingSong(song2, 1));
		var playlistState = AppContext.Services.GetRequiredService<IState<Features.Playlists.State>>();
		await Assert.That(playlistState.Value.DayPlaylist.CurrentSong).IsEqualTo(song2);
	}
}
