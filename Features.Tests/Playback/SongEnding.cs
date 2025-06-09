namespace Features.Tests.Playback;

using Features.Playback;
using Features.Playlists;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;

[UseAppContext]
public class SongEnding(AppContext appContext, IDispatcher dispatcher)
{
	private readonly Song song1 = Dummies.Song with {Title = "Song1"};
	private readonly Song song2 = Dummies.Song with {Title = "Song2"};

	[Test]
	public async Task ResetsCurrentSong()
	{
		var requestedSongToBeReset = Dummies.Song; 
		var actionSubscriber = appContext.Services.GetRequiredService<IActionSubscriber>();
		actionSubscriber.SubscribeToAction<PauseSongSignal>(this, action =>
		{
			if (action.Reset)
				requestedSongToBeReset = action.Song;
		});

		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2], []));
		dispatcher.Dispatch(new PlayAction());
		
		dispatcher.Dispatch(new SongEndedSignal(song1));

		await Assert.That(requestedSongToBeReset).IsEqualTo(song1);
	}

	[Test]
	public async Task PlaysNextSong()
	{
		dispatcher.Dispatch(new SetPlaylistsAction([song1, song2], []));
		dispatcher.Dispatch(new PlayAction());
		
		dispatcher.Dispatch(new SongEndedSignal(song1));

		var playbackState = appContext.Services.GetRequiredService<IState<Features.Playback.State>>();
		await Assert.That(playbackState.Value.PlayingSongs).HasCount(1)
			.And.Contains(new PlayingSong(song2, 1));
		var playlistState = appContext.Services.GetRequiredService<IState<Features.Playlists.State>>();
		await Assert.That(playlistState.Value.DayPlaylist.CurrentSong).IsEqualTo(song2);
	}
}
