namespace Controller.Playlist;

using Fluxor;

public class Effects(Client client)
{
	[EffectMethod(typeof(UpdatePlaylistAction))]
	public async Task UpdatePlaylists(IDispatcher dispatcher)
	{
		client.PlaylistsUpdated += playlists => dispatcher.Dispatch(new SetPlaylistsAction(playlists));
		await client.UpdatePlaylists();
	}
}
