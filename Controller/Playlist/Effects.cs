namespace Controller.Playlist;

using Fluxor;

public class Effects(Client client)
{
	[EffectMethod(typeof(UpdatePlaylistAction))]
	public async Task UpdatePlaylist(IDispatcher dispatcher)
	{
		client.PlaylistUpdated += playlist =>
		{
			dispatcher.Dispatch(new SetPlaylistAction(playlist));
		};
		await client.UpdatePlaylist();
	}
}
