namespace App;

using Playlists;

public class Interactions(AppState state)
{
	public event Action<Core.Playlists> OnPlaylistsLoaded = _ => { };
	
	public event Action OnPlaybackChanged = () => { };
	
	public void LoadPlaylists()
	{
		var playlists = Loader.LoadPlaylists();
		state.Playlists = playlists;
		OnPlaylistsLoaded(playlists);
	}

	public void Play()
	{
		state.CurrentSong = state.Playlists.DayPlaylist.Songs[0];
		state.CurrentSongStarted = DateTime.Now;
		state.NextSong = state.Playlists.DayPlaylist.Songs.Length > 1
			? state.Playlists.DayPlaylist.Songs[1]
			: state.Playlists.DayPlaylist.Songs[0];
		state.SongOnOtherPlaylist = state.Playlists.NightPlaylist.Songs[0];
		OnPlaybackChanged();
	}
}
