namespace App;

using Playlists;

public class Interactions(AppState state)
{
	public event Action<Core.Playlists> OnPlaylistsLoaded = _ => { };
	
	public event Action OnPlaybackChanged = () => { };

	public event Action OnFirstSongPreloaded = () => { };
	
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

	public void Switch()
	{
		if (state.CurrentSong is null || state.CurrentSongStarted is null)
			return;
		
		state.GamePhase = state.GamePhase == GamePhase.Day
			? GamePhase.Night
			: GamePhase.Day;
		
		var currentSong = state.SongOnOtherPlaylist;
		var currentSongStarted = DateTime.Now - state.TimeOnOtherPlaylist;
		
		var currentPlaylist = state.GamePhase == GamePhase.Day
			? state.Playlists.DayPlaylist
			: state.Playlists.NightPlaylist;
		
		var indexOfCurrentSong = Array.IndexOf(currentPlaylist.Songs, currentSong);
		
		state.SongOnOtherPlaylist = state.CurrentSong;
		state.TimeOnOtherPlaylist = DateTime.Now - (DateTime)state.CurrentSongStarted;
		
		state.NextSong = currentPlaylist.Songs[indexOfCurrentSong + 1 % currentPlaylist.Songs.Length];
		
		state.CurrentSong = currentSong;
		state.CurrentSongStarted = currentSongStarted;
		
		OnPlaybackChanged();
	}

	public void NotifyPreloaded()
		=> OnFirstSongPreloaded();
}
