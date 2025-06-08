namespace Features.Playlists;

public static class Reducers
{
	[ReducerMethod]
	public static State SetPlaylists(State state, SetPlaylistsAction action)
	{
		var (daySongs, nightSongs) = action;
		
		var dayPlaylist = state.DayPlaylist with { Songs = daySongs };
		if (!dayPlaylist.Songs.Contains(dayPlaylist.CurrentSong))
			dayPlaylist = dayPlaylist with {CurrentSong = dayPlaylist.Songs.FirstOrDefault()};
		
		var nightPlaylist = state.NightPlaylist with { Songs = nightSongs };
		if (!nightPlaylist.Songs.Contains(nightPlaylist.CurrentSong))
			nightPlaylist = nightPlaylist with {CurrentSong = nightPlaylist.Songs.FirstOrDefault()};
		
		return state with { DayPlaylist = dayPlaylist, NightPlaylist = nightPlaylist };
	}

	[ReducerMethod]
	public static State SetDayGain(State state, SetDayGainAction action)
		=> state with { DayPlaylist = state.DayPlaylist with { Gain = action.Volume } };
	
	[ReducerMethod]
	public static State SetNightGain(State state, SetNightGainAction action)
		=> state with { NightPlaylist = state.NightPlaylist with { Gain = action.Volume } };
	
	[ReducerMethod]
	public static State SetCurrentSongForDay(State state, SetCurrentSongForDayAction action)
	=> state with { DayPlaylist = state.DayPlaylist with { CurrentSong = action.Song } };
	
	[ReducerMethod]
	public static State SetCurrentSongForNight(State state, SetCurrentSongForNightAction action)
		=> state with { NightPlaylist = state.NightPlaylist with { CurrentSong = action.Song } };
}
