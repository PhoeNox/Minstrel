namespace Features.Playback;

[FeatureState]
public record State
{
	public Song? CurrentSong { get; set; }
	public HashSet<Song> LoadedSongs { get; init; } = [];
	
	public bool IsPlaying { get; init; }
	public float Gain { get; set; } = 1;
}
