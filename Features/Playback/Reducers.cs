namespace Features.Playback;

public static class Reducers
{
	[ReducerMethod]
	public static State SetCurrentSongLoaded(State state, SongLoadedSignal signal)
	{
		HashSet<Song> loadedSongs = [..state.LoadedSongs, signal.Song];
		return state with { LoadedSongs = loadedSongs };
	}
	
	[ReducerMethod]
	public static State Play(State state, PlayAction action)
	=> state with { IsPlaying = true };
	
	[ReducerMethod]
	public static State Pause(State state, PauseAction action)
		=> state with { IsPlaying = false };

	[ReducerMethod]
	public static State OnPlaySignal(State state, PlaySignal signal)
	{
		var playingSongs = state.PlayingSongs;
		playingSongs.Add(new PlayingSong(signal.Song, signal.Gain));
		return state with { PlayingSongs = playingSongs };
	}
}
