namespace Features.Playback;

[FeatureState]
public record State
{
	public HashSet<Song> LoadedSongs { get; init; } = [];
	public bool IsPlaying { get; init; }
	public HashSet<PlayingSong> PlayingSongs { get; init; } = [];

	public bool IsSongOnOtherPlaylistAvailable(Playlists.State playlistState, GamePhases.State gamePhaseState)
	{
		var otherSong = gamePhaseState.Phase == GamePhase.Day
				? playlistState.NightPlaylist.CurrentSong
				: playlistState.DayPlaylist.CurrentSong;
		return otherSong is not null && LoadedSongs.Contains(otherSong);
	}
}

public record PlayingSong(Song Song, float Gain);
