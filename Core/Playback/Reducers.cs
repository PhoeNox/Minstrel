namespace Core.Playback;

public static class Reducers
{
	[ReducerMethod]
	public static State SetCurrentSongLoaded(State state, SongLoadedAction action)
	{
		HashSet<Song> loadedSongs = [..state.LoadedSongs, action.Song];
		return state with { LoadedSongs = loadedSongs };
	}
	
	[ReducerMethod]
	public static State Play(State state, PlayAction action)
	=> state with { IsPlaying = true };
	
	[ReducerMethod]
	public static State Pause(State state, PauseAction action)
		=> state with { IsPlaying = false };
}
