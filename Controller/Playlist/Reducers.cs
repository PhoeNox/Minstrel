namespace Controller.Playlist;

using Fluxor;

public static class Reducers
{
	[ReducerMethod]
	public static State SetPlaylist(State state, SetPlaylistAction action)
		=> state with { Playlist = action.Playlist };
}
