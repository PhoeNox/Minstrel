namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(
		IFileSystemProvider fileSystemProvider,
		IState<State> state)
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

	[EffectMethod]
	public Task MoveSong(MoveSongAction action, IDispatcher dispatcher)
	{
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.WithSongMoved(action.OldIndex, action.NewIndex);
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task AddSong(AddSongAction action, IDispatcher dispatcher)
	{
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.WithSongAdded(action.Song);
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task RemoveSong(RemoveSongAction action, IDispatcher dispatcher)
	{
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.WithSongRemoved(action.Song);
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task SavePlaylist(SetPlaylistsAction action, IDispatcher dispatcher)
	{
		fileSystemProvider.SavePlaylist(action.GamePhase, action.Songs);
		return Task.CompletedTask;
	}
}
