namespace Features.Playlists;

[FeatureState]
public record State
{
	public Playlist DayPlaylist { get; set; } = new([]);
	public Playlist NightPlaylist { get; set; } = new([]);
}
