namespace App;

using Playlists;

public class Interactions(AppState state)
{
	public event Action<Core.Playlists> PlaylistsLoaded = _ => { };
	public event Action PlaybackChanged = () => { };
	public event Action SongOnOtherPlaylistChanged = () => { };
	public event Action FirstSongPreloaded = () => { };
	public event Action<GamePhase> GamePhaseChanged = _ => { };
	public event Action<float> GainChanged = _ => { };

	public void LoadPlaylists()
	{
		var playlists = Loader.LoadPlaylists();
		state.Playlists = playlists;
		PlaylistsLoaded(playlists);
	}

	public void StartPlaying()
	{
		state.CurrentSong = state.Playlists.DayPlaylist.Songs[0];
		state.NextSong = state.Playlists.DayPlaylist.Songs.Length > 1
			? state.Playlists.DayPlaylist.Songs[1]
			: state.Playlists.DayPlaylist.Songs[0];
		state.SongOnOtherPlaylist = state.Playlists.NightPlaylist.Songs[0];
		PlaybackChanged();
	}

	public void SwitchGamePhase()
	{
		if (state.CurrentSong is null)
			return;

		state.GamePhase = state.GamePhase == GamePhase.Day
			? GamePhase.Night
			: GamePhase.Day;
		GamePhaseChanged(state.GamePhase);

		(state.SongOnOtherPlaylist, state.CurrentSong) = (state.CurrentSong, state.SongOnOtherPlaylist);
		state.NextSong = GetNextSong();
		PlaybackChanged();
	}

	public void NotifyPreloaded()
		=> FirstSongPreloaded();

	public void PlayNextSong()
	{
		if (state.CurrentSong is null)
			return;
		state.CurrentSong = state.NextSong;
		state.NextSong = GetNextSong();
		PlaybackChanged();
	}

	public void SetDayGain(double value)
	{
		state.DayGain = (float) value;
		if (state.GamePhase == GamePhase.Day)
			GainChanged(state.DayGain);
	}

	public void SetNightGain(double value)
	{
		state.NightGain = (float) value;
		if (state.GamePhase == GamePhase.Night)
			GainChanged(state.NightGain);
	}
	
	public void SetDaySong(Song song)
	{
		if (state.GamePhase == GamePhase.Night)
		{
			state.SongOnOtherPlaylist = song;
			SongOnOtherPlaylistChanged();
		}
		else
		{
			state.CurrentSong = song;
			state.NextSong = GetNextSong();
			PlaybackChanged();
		}
	}

	public void SetNightSong(Song song)
	{
		if (state.GamePhase == GamePhase.Day)
		{
			state.SongOnOtherPlaylist = song;
			SongOnOtherPlaylistChanged();
		}
		else
		{
			state.CurrentSong = song;
			state.NextSong = GetNextSong();
			PlaybackChanged();
		}
	}

	private Song GetNextSong()
	{
		var currentPlaylist = state.GamePhase == GamePhase.Day
			? state.Playlists.DayPlaylist
			: state.Playlists.NightPlaylist;
		var indexOfCurrentSong = Array.IndexOf(currentPlaylist.Songs, state.CurrentSong);
		return currentPlaylist.Songs[(indexOfCurrentSong + 1) % currentPlaylist.Songs.Length];
	}
}
