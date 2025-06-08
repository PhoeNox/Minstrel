namespace Features.Playback;

[FeatureState]
public record State
{
	public HashSet<Song> LoadedSongs { get; init; } = [];
	public bool IsPlaying { get; init; }
	public HashSet<PlayingSong> PlayingSongs { get; init; } = []; 
}

public record PlayingSong(Song Song, float Gain);
