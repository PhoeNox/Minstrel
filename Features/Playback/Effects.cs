namespace Features.Playback;

using System.Diagnostics.CodeAnalysis;
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
		if (gamePhaseState.Value.Phase == GamePhase.Day)
		{
			var oldSong = playlistState.Value.NightPlaylist.CurrentSong;
			var song = playlistState.Value.DayPlaylist.CurrentSong;
			var gain = playlistState.Value.DayPlaylist.Gain;
			var nextSong = GetNextSong(song, playlistState.Value.DayPlaylist);
			return (oldSong, song, gain, nextSong);
		}
		else
		{
			var oldSong = playlistState.Value.DayPlaylist.CurrentSong;
			var song = playlistState.Value.NightPlaylist.CurrentSong;
			var gain = playlistState.Value.NightPlaylist.Gain;
			var nextSong = GetNextSong(song, playlistState.Value.NightPlaylist);
			return (oldSong, song, gain, nextSong);
		}
	}

	[return: NotNullIfNotNull(nameof(song))]
	private static Song? GetNextSong(Song? song, Playlist playlist)
	{
		if (song is null)
			return null;
		
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

		if (playlist.CurrentSong is not null)
			dispatcher.Dispatch(new LoadSongSignal(playlist.CurrentSong));

		if (playlist.Songs.Length > 1)
		{
			var currentSongIndex = Array.IndexOf(playlist.Songs, playlist.CurrentSong);
			var nextSongIndex = (currentSongIndex + 1) % playlist.Songs.Length;
			var nextSong = playlist.Songs[nextSongIndex];
			dispatcher.Dispatch(new LoadSongSignal(nextSong));
		}

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
