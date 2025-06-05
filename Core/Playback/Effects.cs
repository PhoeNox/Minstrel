namespace Core.Playback;

using GamePhases;
using Playlists;

public class Effects(
	IState<State> state,
	IState<Playlists.State> playlistState,
	IState<GamePhases.State> gamePhaseState)
{
	[EffectMethod]
	public Task Play(PlayAction action, IDispatcher dispatcher)
	{
		SetCurrentSong(dispatcher);
		return Task.CompletedTask;
	}

	[EffectMethod]
	public Task OnGamePhaseChanged(SwitchGamePhaseAction action, IDispatcher dispatcher)
	{
		SetCurrentSong(dispatcher);
		return Task.CompletedTask;
	}

	private void SetCurrentSong(IDispatcher dispatcher)
	{
		var (currentSong, nextSong, songOnOtherPlaylist, gain) = GetSongsAndGainForCurrentGamePhase();
		dispatcher.Dispatch(new CurrentSongChangedAction(currentSong, nextSong, songOnOtherPlaylist, gain));
	}

	private (Song CurrentSong, Song NextSong, Song SongOnOtherPlaylist, float Gain) GetSongsAndGainForCurrentGamePhase()
	{
		if (gamePhaseState.Value.Phase == GamePhase.Day)
		{
			var currentSong = playlistState.Value.DayPlaylist.CurrentSong!;
			var nextSong = playlistState.Value.DayPlaylist.CurrentSong!;
			var songOnOtherPlaylist = playlistState.Value.NightPlaylist.CurrentSong!;
			var gain = playlistState.Value.DayPlaylist.Gain;
			return (currentSong, nextSong, songOnOtherPlaylist, gain);
		}
		else
		{
			var currentSong = playlistState.Value.NightPlaylist.CurrentSong!;
			var nextSong = playlistState.Value.NightPlaylist.CurrentSong!;
			var songOnOtherPlaylist = playlistState.Value.DayPlaylist.CurrentSong!;
			var gain = playlistState.Value.NightPlaylist.Gain;
			return (currentSong, nextSong, songOnOtherPlaylist, gain);
		}
	}
	
	[EffectMethod]
	public Task OnDayGainChanged(SetDayGainAction action, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase != GamePhase.Day)
			return Task.CompletedTask;
		
		var gain = playlistState.Value.DayPlaylist.Gain;
		dispatcher.Dispatch(new GainChangedAction(gain));
		return Task.CompletedTask;
	}
	
	[EffectMethod]
	public Task OnNightGainChanged(SetNightGainAction action, IDispatcher dispatcher)
	{
		if (gamePhaseState.Value.Phase != GamePhase.Night)
			return Task.CompletedTask;
		
		var gain = playlistState.Value.NightPlaylist.Gain;
		dispatcher.Dispatch(new GainChangedAction(gain));
		return Task.CompletedTask;
	}
}
