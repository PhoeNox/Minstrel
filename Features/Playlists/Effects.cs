namespace Features.Playlists;

using Infrastructure.FileSystem;

public class Effects(IPlaylistLoader playlistLoader)
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public async Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (daySongs, nightSongs) = await playlistLoader.LoadPlaylists();
		dispatcher.Dispatch(new SetPlaylistsAction(daySongs, nightSongs));
		dispatcher.Dispatch(new PlaylistsLoadedAction());
	}
}
