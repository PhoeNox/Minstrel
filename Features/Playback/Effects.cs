namespace Features.Playback;

using Playlists;

public class Effects(
		IState<State> playbackState,
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

	private (Song oldSong, Song currentSong, float currentGain, Song nextSong)
			GetSongsAndGainForCurrentGamePhase()
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
		var playlist = playlistState.Value.GetPlaylist(gamePhaseState.Value.Phase);
		var nextSong = GetNextSong(signal.Song, playlist);
		dispatcher.Dispatch(new SetCurrentSongAction(gamePhaseState.Value.Phase, nextSong, signal.Song));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnSetCurrentSong(SetCurrentSongAction action, IDispatcher dispatcher)
	{
		dispatcher.Dispatch(new LoadSongSignal(action.Song));

		if (action.PreviousSong is not null)
			dispatcher.Dispatch(new PauseSongSignal(action.PreviousSong, Reset: true));

		if (playbackState.Value.IsPlaying && gamePhaseState.Value.Phase == action.GamePhase)
		{
			var playlist = playlistState.Value.GetPlaylist(action.GamePhase);
			dispatcher.Dispatch(new PlaySongSignal(action.Song, playlist.Gain));
			var songAfterNextSong = GetNextSong(action.Song, playlist);
			dispatcher.Dispatch(new LoadSongSignal(songAfterNextSong));
		}

		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnPlaylistSet(SetPlaylistsAction action, IDispatcher dispatcher)
	{
		var playlist = playlistState.Value.GetPlaylist(action.GamePhase);
		var currentSongIndex = Array.IndexOf(playlist.Songs, playlist.CurrentSong);
		var nextSongIndex = (currentSongIndex + 1) % playlist.Songs.Length;
		var nextSong = playlist.Songs[nextSongIndex];
		
		dispatcher.Dispatch(new LoadSongSignal(playlist.CurrentSong!));
		dispatcher.Dispatch(new LoadSongSignal(nextSong));
		
		return Task.CompletedTask;
	}
	
	[EffectMethod]
	public Task OnGainChanged(SetGainAction action, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase != action.GamePhase)
			return Task.CompletedTask;

		var playlist = playlistState.Value.GetPlaylist(action.GamePhase);
		var song = playlist.CurrentSong!;
		var gain = playlist.Gain;
		dispatcher.Dispatch(new SetGainSignal(song, gain));
		return Task.CompletedTask;
	}
}
