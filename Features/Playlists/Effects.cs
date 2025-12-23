namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(IPlaylistLoader playlistLoader, ISongLoader songLoader)
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public async Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (daySongs, nightSongs) = await playlistLoader.LoadPlaylists();
		dispatcher.Dispatch(new SetPlaylistsAction(daySongs, nightSongs));
		dispatcher.Dispatch(new PlaylistsLoadedAction());
	}

	[EffectMethod(typeof(StoreInitializedAction))]
	public Task LoadSongs(IDispatcher dispatcher)
	{
		var songs = songLoader.LoadSongs();
		dispatcher.Dispatch(new SetDatabaseAction(songs));
		return Task.CompletedTask;
	}
}
