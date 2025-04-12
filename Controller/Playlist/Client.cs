namespace Controller.Playlist;

using Microsoft.AspNetCore.SignalR.Client;
using Playlist = Shared.Playlist;

public class Client
{
	private readonly HubConnection hubConnection;

	public event Action<Playlists> PlaylistsUpdated = _ => { };

	public Client()
	{
		hubConnection = new HubConnectionBuilder()
			.WithUrl("https://localhost:7194/playlistHub")
			.Build();
		hubConnection.On<Playlists>(
			"PlaylistsUpdated",
			playlists => PlaylistsUpdated.Invoke(playlists));
		hubConnection.StartAsync().Wait();
	}
	
	public async Task UpdatePlaylists()
	{
		await hubConnection.SendAsync("UpdatePlaylists");
	}
}
