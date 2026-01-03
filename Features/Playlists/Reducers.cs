namespace Features.Playlists;

public static class Reducers
{
	[ReducerMethod]
	public static State SetDatabase(State state, SetDatabaseAction action)
		=> state with {Database = action.Songs};

	[ReducerMethod]
	public static State SetPlaylists(State state, SetPlaylistsAction action)
	{
		var playlist = state.GetPlaylist(action.GamePhase);

		playlist = playlist with {Songs = action.Songs};

		if (!playlist.Songs.Contains(playlist.CurrentSong))
			playlist = playlist with {CurrentSong = playlist.Songs.FirstOrDefault()};

		if (action.GamePhase == GamePhase.Day)
			return state with {DayPlaylist = playlist};
		else
			return state with {NightPlaylist = playlist};
	}

	[ReducerMethod]
	public static State SetGain(State state, SetGainAction action)
	{
		if (action.GamePhase == GamePhase.Day)
			return state with {DayPlaylist = state.DayPlaylist with {Gain = action.Volume}};
		else
			return state with {NightPlaylist = state.NightPlaylist with {Gain = action.Volume}};
	}

	[ReducerMethod]
	public static State SetCurrentSong(State state, SetCurrentSongAction action)
	{
		if (action.GamePhase == GamePhase.Day)
			return state with {DayPlaylist = state.DayPlaylist with {CurrentSong = action.Song}};
		else
			return state with {NightPlaylist = state.NightPlaylist with {CurrentSong = action.Song}};
	}
}
