namespace Game;

using Microsoft.AspNetCore.SignalR;

public class PlaylistHub : Hub
{
	public async Task UpdatePlaylists()
	{
		var playlists = Playlist.Loader.LoadPlaylists();
		await Clients.All.SendAsync("PlaylistsUpdated", playlists);
	}
}
