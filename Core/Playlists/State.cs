namespace Core.Playlists;

[FeatureState]
public record State
{
	public Playlist DayPlaylist { get; set; } = new([]);
	public Playlist NightPlaylist { get; set; } = new([]);
}

public record Playlist(Song[] Songs)
{
	public Song? CurrentSong { get; set; } = Songs.FirstOrDefault();
	public float Gain { get; set; } = 1;
}
