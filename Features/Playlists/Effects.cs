namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(IPlaylistLoader playlistLoader)
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public async Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (daySongs, nightSongs) = await playlistLoader.LoadPlaylists();
		var dayPlaylist = new Playlist(daySongs);
		var nightPlaylist = new Playlist(nightSongs);
		dispatcher.Dispatch(new SetPlaylistsAction(dayPlaylist, nightPlaylist));
		dispatcher.Dispatch(new PlaylistsLoadedAction());
	}
}
