namespace Controller.Playlist;

using Fluxor;

public static class Reducers
{
	[ReducerMethod]
	public static State SetPlaylists(State state, SetPlaylistsAction action)
		=> state with { Playlists = action.Playlists };
}
