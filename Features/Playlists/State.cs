namespace Features.Playlists;

[FeatureState]
public record State
{
	public Playlist DayPlaylist { get; init; } = new([]);
	public Playlist NightPlaylist { get; init; } = new([]);
	
	public Song[]? Database { get; init; }
}
