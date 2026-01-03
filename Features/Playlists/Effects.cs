namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(IFileSystemProvider fileSystemProvider)
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (daySongs, nightSongs) = fileSystemProvider.LoadPlaylists();
		dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Day, daySongs));
		dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Night, nightSongs));
		dispatcher.Dispatch(new PlaylistsLoadedAction());
		return Task.CompletedTask;
	}

	[EffectMethod(typeof(StoreInitializedAction))]
	public Task LoadSongs(IDispatcher dispatcher)
	{
		var songs = fileSystemProvider.LoadSongs();
		dispatcher.Dispatch(new SetDatabaseAction(songs));
		return Task.CompletedTask;
	}
}
