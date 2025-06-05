namespace Core.Playlists;

public class Effects
{
	[EffectMethod(typeof(LoadPlaylistsAction))]
	public async Task LoadPlaylists(IDispatcher dispatcher)
	{
		var (dayPlaylist, nightPlaylist) = await Loader.LoadPlaylists();
		dispatcher.Dispatch(new SetPlaylistsAction(dayPlaylist, nightPlaylist));
	}
}
