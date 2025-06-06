namespace Features.Playlists;

public static class Reducers
{
	[ReducerMethod]
	public static State SetPlaylists(State state, SetPlaylistsAction action)
		=> state with
		{
			DayPlaylist = action.DayPlaylist,
			NightPlaylist = action.NightPlaylist,
		};
	
	[ReducerMethod]
	public static State SetDayGain(State state, SetDayGainAction action)
		=> state with { DayPlaylist = state.DayPlaylist with { Gain = action.Volume } };
	
	[ReducerMethod]
	public static State SetNightGain(State state, SetNightGainAction action)
		=> state with { NightPlaylist = state.NightPlaylist with { Gain = action.Volume } };
}
