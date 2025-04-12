namespace Controller.Playlist;

using Fluxor;
using Playlist = Shared.Playlist;

[FeatureState]
public record State
{
	public Playlists Playlists { get; init; } = new(new Playlist([]), new Playlist([]));
}
