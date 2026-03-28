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
		if (currentSong is not null)
			dispatcher.Dispatch(new PlaySongSignal(currentSong, gain));
		if (nextSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(nextSong));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task Pause(PauseAction action, IDispatcher dispatcher)
	{
		var song = gamePhaseState.Value.Phase == GamePhase.Day
				? playlistState.Value.DayPlaylist.CurrentSong
				: playlistState.Value.NightPlaylist.CurrentSong;
		if (song is not null)
			dispatcher.Dispatch(new PauseSongSignal(song));
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnPlaylistSwitched(SwitchPlaylist action, IDispatcher dispatcher)
	{
		var (oldSong, currentSong, gain, nextSong) = GetSongsAndGainForCurrentGamePhase();
		if (oldSong is not null)
			dispatcher.Dispatch(new PauseSongSignal(oldSong));
		if (currentSong is not null)
			dispatcher.Dispatch(new PlaySongSignal(currentSong, gain));
		if (nextSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(nextSong));
		return Task.CompletedTask;
	}

	private (Song? oldSong, Song? currentSong, float currentGain, Song? nextSong)
			GetSongsAndGainForCurrentGamePhase()
	{
		var (active, other) = gamePhaseState.Value.Phase == GamePhase.Day
				? (playlistState.Value.DayPlaylist, playlistState.Value.NightPlaylist)
				: (playlistState.Value.NightPlaylist, playlistState.Value.DayPlaylist);
		return GetSongsAndGain(active, other);
	}

	private static (Song? oldSong, Song? currentSong, float gain, Song? nextSong)
			GetSongsAndGain(Playlist active, Playlist other)
	{
		var nextSong = active.CurrentSong is not null
				? active.Songs.GetNextSong(active.CurrentSong)
				: null;
		return (other.CurrentSong, active.CurrentSong, active.Gain, nextSong);
	}

	[EffectMethod]
	public Task OnSongEnded(SongEndedSignal signal, IDispatcher dispatcher)
	{
		var playlist = playlistState.Value.GetPlaylist(gamePhaseState.Value.Phase);
		var nextSong = playlist.Songs.GetNextSong(signal.Song);
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
			dispatcher.Dispatch(new LoadSongSignal(playlist.Songs.GetNextSong(action.Song)));
		}

		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnPlaylistSet(SetPlaylistsAction action, IDispatcher dispatcher)
	{
		var playlist = playlistState.Value.GetPlaylist(action.GamePhase);

		if (playlist.CurrentSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(playlist.CurrentSong));

		if (playlist.Songs.Length > 1 && playlist.CurrentSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(playlist.Songs.GetNextSong(playlist.CurrentSong)));

		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnGainChanged(SetGainAction action, IDispatcher dispatcher)
	{
		var playlist = playlistState.Value.GetPlaylist(action.GamePhase);
		if (gamePhaseState.Value.Phase == action.GamePhase && playlist.CurrentSong is not null)
			dispatcher.Dispatch(new SetGainSignal(playlist.CurrentSong, playlist.Gain));
		return Task.CompletedTask;
	}
}
