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
		var songToMove = state.Value.GetPlaylist(action.GamePhase).Songs[action.OldIndex];
		
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.ToList();
		songs.RemoveAt(action.OldIndex);
		if (action.NewIndex < songs.Count)
			songs.Insert(action.NewIndex, songToMove);
		else
			songs.Add(songToMove);
		
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs.ToArray()));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task AddSong(AddSongAction action, IDispatcher dispatcher)
	{
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.ToList();
		songs.Add(action.Song);
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs.ToArray()));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task RemoveSong(RemoveSongAction action, IDispatcher dispatcher)
	{
		var songs = state.Value.GetPlaylist(action.GamePhase).Songs.ToList();
		songs.Remove(action.Song);
		dispatcher.Dispatch(new SetPlaylistsAction(action.GamePhase, songs.ToArray()));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task SavePlaylist(SetPlaylistsAction action, IDispatcher dispatcher)
	{
		fileSystemProvider.SavePlaylist(action.GamePhase, action.Songs);
		return Task.CompletedTask;
	}
}
