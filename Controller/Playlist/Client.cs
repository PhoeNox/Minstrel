namespace Controller.Playlist;

using Microsoft.AspNetCore.SignalR.Client;
using Playlist = Shared.Playlist;

public class Client
{
	private readonly HubConnection hubConnection;

	public event Action<Playlist> PlaylistUpdated = _ => { };

	public Client()
	{
		hubConnection = new HubConnectionBuilder()
			.WithUrl("https://localhost:7194/playlistHub")
			.Build();
		hubConnection.On<Playlist>("PlaylistUpdated", playlist => PlaylistUpdated.Invoke(playlist));
		hubConnection.StartAsync().Wait();
	}
	
	public async Task UpdatePlaylist()
	{
		await hubConnection.SendAsync("UpdatePlaylist");
	}
}
