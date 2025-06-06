namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(IPlaylistLoader playlistLoader)
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public async Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (dayPlaylist, nightPlaylist) = await playlistLoader.LoadPlaylists();
		dispatcher.Dispatch(new SetPlaylistsAction(dayPlaylist, nightPlaylist));
	}
}
