namespace Features.Playlists;

public static class Reducers
{
	[ReducerMethod]
	public static State SetDatabase(State state, SetDatabaseAction action)
		=> state with {Database = action.Songs};

	[ReducerMethod]
	public static State SetPlaylists(State state, SetPlaylistsAction action)
	{
		var (daySongs, nightSongs) = action;

		var dayPlaylist = state.DayPlaylist with {Songs = daySongs};
		if (!dayPlaylist.Songs.Contains(dayPlaylist.CurrentSong))
			dayPlaylist = dayPlaylist with {CurrentSong = dayPlaylist.Songs.FirstOrDefault()};

		var nightPlaylist = state.NightPlaylist with {Songs = nightSongs};
		if (!nightPlaylist.Songs.Contains(nightPlaylist.CurrentSong))
			nightPlaylist = nightPlaylist with {CurrentSong = nightPlaylist.Songs.FirstOrDefault()};

		return state with
		{
				DayPlaylist = dayPlaylist,
				NightPlaylist = nightPlaylist,
		};
	}

	[ReducerMethod]
	public static State SetDayGain(State state, SetDayGainAction action)
		=> state with {DayPlaylist = state.DayPlaylist with {Gain = action.Volume}};

	[ReducerMethod]
	public static State SetNightGain(State state, SetNightGainAction action)
		=> state with {NightPlaylist = state.NightPlaylist with {Gain = action.Volume}};

	[ReducerMethod]
	public static State MoveDaySong(State state, MoveDaySongAction action)
	{
		var song = state.DayPlaylist.Songs[action.OldIndex];

		var songs = state.DayPlaylist.Songs.ToList();
		songs.RemoveAt(action.OldIndex);
		if (action.NewIndex < songs.Count)
			songs.Insert(action.NewIndex, song);
		else
			songs.Add(song);

		return state with {DayPlaylist = state.DayPlaylist with {Songs = songs.ToArray()}};
	}

	[ReducerMethod]
	public static State AddSongToPlaylist(State state, AddSongToPlaylistAction action)
	{
		if (action.GamePhase == GamePhase.Day)
			return AddSongToDayPlaylist(state, action.Song);
		else
			return AddSongToNightPlaylist(state, action.Song);
	}

	private static State AddSongToDayPlaylist(State state, Song song)
	{
		return state with
		{
				DayPlaylist = state.DayPlaylist with
				{
						Songs = [..state.DayPlaylist.Songs, song],
				},
		};
	}
	
	private static State AddSongToNightPlaylist(State state, Song song)
	{
		return state with
		{
				NightPlaylist = state.NightPlaylist with
				{
						Songs = [..state.NightPlaylist.Songs, song],
				},
		};
	}

	[ReducerMethod]
	public static State MoveNightSong(State state, MoveNightSongAction action)
	{
		var song = state.NightPlaylist.Songs[action.OldIndex];

		var songs = state.NightPlaylist.Songs.ToList();
		songs.RemoveAt(action.OldIndex);
		if (action.NewIndex < songs.Count)
			songs.Insert(action.NewIndex, song);
		else
			songs.Add(song);

		return state with {NightPlaylist = state.NightPlaylist with {Songs = songs.ToArray()}};
	}

	[ReducerMethod]
	public static State SetCurrentSongForDay(State state, SetCurrentSongForDayAction action)
		=> state with {DayPlaylist = state.DayPlaylist with {CurrentSong = action.Song}};

	[ReducerMethod]
	public static State SetCurrentSongForNight(State state, SetCurrentSongForNightAction action)
		=> state with {NightPlaylist = state.NightPlaylist with {CurrentSong = action.Song}};
}
