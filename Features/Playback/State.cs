namespace Features.Playback;

[FeatureState]
public record State
{
	public Song? CurrentSong { get; set; }
	public HashSet<Song> LoadedSongs { get; set; } = [];
	
	public bool IsPlaying { get; set; }
	public float Gain { get; set; } = 1;
}
