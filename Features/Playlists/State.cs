namespace Features.Playlists;

[FeatureState]
public record State
{
	public Playlist DayPlaylist { get; init; } = new([]);
	public Playlist NightPlaylist { get; init; } = new([]);
	public bool PlaylistsLoaded { get; init; } = false;
}
