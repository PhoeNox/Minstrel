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
	public static State OnPlaySignal(State state, PlaySongSignal songSignal)
	{
		HashSet<PlayingSong> playingSongs = [..state.PlayingSongs, new(songSignal.Song, songSignal.Gain)];
		return state with { PlayingSongs = playingSongs };
	}

	[ReducerMethod]
	public static State OnResetSignal(State state, PauseSongSignal songSignal)
	{
		if (songSignal.Reset is false)
			return state;
		
		var playingSongs = new HashSet<PlayingSong>(state.PlayingSongs);
		playingSongs.RemoveWhere(x => x.Song == songSignal.Song);
		return state with { PlayingSongs = playingSongs };
	}
}
