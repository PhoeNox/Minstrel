namespace Features.Playback;

using Playlists;

public class Effects(
	IState<Playlists.State> playlistState,
	IState<GamePhases.State> gamePhaseState)
{
	[EffectMethod]
	public Task OnPlaylistsLoaded(PlaylistsLoadedAction action, IDispatcher dispatcher)
	{
		var firstDaySong = playlistState.Value.DayPlaylist.Songs.FirstOrDefault();
		if (firstDaySong is not null)
			dispatcher.Dispatch(new LoadSongSignal(firstDaySong));
		
		var firstNightSong = playlistState.Value.NightPlaylist.Songs.FirstOrDefault();
		if (firstNightSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(firstNightSong));
		
		return Task.CompletedTask;
	}
	
	[EffectMethod]
	public Task Play(PlayAction action, IDispatcher dispatcher)
	{
		var (_, currentSong, gain, nextSong) = GetSongsAndGainForCurrentGamePhase();
		dispatcher.Dispatch(new PlaySongSignal(currentSong, gain));
		dispatcher.Dispatch(new LoadSongSignal(nextSong));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task Pause(PauseAction action, IDispatcher dispatcher)
	{
		var song = gamePhaseState.Value.Phase == GamePhase.Day
			? playlistState.Value.DayPlaylist.CurrentSong
			: playlistState.Value.NightPlaylist.CurrentSong;
		dispatcher.Dispatch(new PauseSongSignal(song!));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnPlaylistSwitched(SwitchPlaylist action, IDispatcher dispatcher)
	{
		var (oldSong, currentSong, gain, nextSong) = GetSongsAndGainForCurrentGamePhase();
		dispatcher.Dispatch(new PauseSongSignal(oldSong));
		dispatcher.Dispatch(new PlaySongSignal(currentSong, gain));
		dispatcher.Dispatch(new LoadSongSignal(nextSong));
		return Task.CompletedTask;
	}

	private (Song oldSong, Song currentSong, float currentGain, Song nextSong) GetSongsAndGainForCurrentGamePhase()
	{
		if (gamePhaseState.Value.Phase == GamePhase.Day)
		{
			var oldSong = playlistState.Value.NightPlaylist.CurrentSong!;
			var song = playlistState.Value.DayPlaylist.CurrentSong!;
			var gain = playlistState.Value.DayPlaylist.Gain;
			var nextSong = GetNextSong(song, playlistState.Value.DayPlaylist);
			return (oldSong, song, gain, nextSong);
		}
		else
		{
			var oldSong = playlistState.Value.DayPlaylist.CurrentSong!;
			var song = playlistState.Value.NightPlaylist.CurrentSong!;
			var gain = playlistState.Value.NightPlaylist.Gain;
			var nextSong = GetNextSong(song, playlistState.Value.NightPlaylist);
			return (oldSong, song, gain, nextSong);
		}
	}

	private static Song GetNextSong(Song song, Playlist playlist)
	{
		var indexOfSong = Array.IndexOf(playlist.Songs, song);
		return playlist.Songs[(indexOfSong + 1) % playlist.Songs.Length];
	}

	[EffectMethod]
	public Task OnSongEnded(SongEndedSignal signal, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase == GamePhase.Day)
		{
			var nextSong = GetNextSong(signal.Song, playlistState.Value.DayPlaylist);
			dispatcher.Dispatch(new SetCurrentSongForDayAction(nextSong, signal.Song));
		}
		else
		{
			var nextSong = GetNextSong(signal.Song, playlistState.Value.NightPlaylist);
			dispatcher.Dispatch(new SetCurrentSongForNightAction(nextSong, signal.Song));
		}
		return Task.CompletedTask;	
	}

	[EffectMethod]
	public Task OnSetCurrentSongForDay(SetCurrentSongForDayAction action, IDispatcher dispatcher)
	{
		dispatcher.Dispatch(new LoadSongSignal(action.Song));

		if (gamePhaseState.Value.Phase == GamePhase.Day)
		{
			if (action.PreviousSong is not null)
				dispatcher.Dispatch(new PauseSongSignal(action.PreviousSong, Reset: true));
			
			dispatcher.Dispatch(new PlaySongSignal(action.Song, playlistState.Value.DayPlaylist.Gain));
			var songAfterNextSong = GetNextSong(action.Song, playlistState.Value.DayPlaylist);
			dispatcher.Dispatch(new LoadSongSignal(songAfterNextSong));
		}

		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnSetCurrentSongForNight(SetCurrentSongForNightAction action, IDispatcher dispatcher)
	{
		dispatcher.Dispatch(new LoadSongSignal(action.Song));

		if (gamePhaseState.Value.Phase == GamePhase.Night)
		{
			if (action.PreviousSong is not null)
				dispatcher.Dispatch(new PauseSongSignal(action.PreviousSong, Reset: true));
			
			dispatcher.Dispatch(new PlaySongSignal(action.Song, playlistState.Value.NightPlaylist.Gain));
			var songAfterNextSong = GetNextSong(action.Song, playlistState.Value.NightPlaylist);
			dispatcher.Dispatch(new LoadSongSignal(songAfterNextSong));
		}
		
		return Task.CompletedTask;
	}
	
	[EffectMethod]
	public Task OnDayGainChanged(SetDayGainAction action, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase != GamePhase.Day)
			return Task.CompletedTask;
		
		var song = playlistState.Value.DayPlaylist.CurrentSong!;
		var gain = playlistState.Value.DayPlaylist.Gain;
		dispatcher.Dispatch(new SetGainSignal(song, gain));
		return Task.CompletedTask;
	}
	
	[EffectMethod]
	public Task OnNightGainChanged(SetNightGainAction action, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase != GamePhase.Night)
			return Task.CompletedTask;

		var song = playlistState.Value.NightPlaylist.CurrentSong!;
		var gain = playlistState.Value.NightPlaylist.Gain;
		dispatcher.Dispatch(new SetGainSignal(song, gain));
		return Task.CompletedTask;
	}
}
