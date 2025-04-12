namespace Game;

using Microsoft.AspNetCore.SignalR;

public class PlaylistHub : Hub
{
	public async Task UpdatePlaylist()
	{
		var playlist = new Playlist([
			new Song("Song 1", "Artist 1", "Album 1", TimeSpan.FromMinutes(3)),
			new Song("Song 2", "Artist 2", "Album 2", TimeSpan.FromMinutes(4)),
			new Song("Song 3", "Artist 3", "Album 3", TimeSpan.FromMinutes(5)),
		]);
		
		await Clients.All.SendAsync("PlaylistUpdated", playlist);
	}
}
