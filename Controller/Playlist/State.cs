namespace Controller.Playlist;

using Fluxor;
using Playlist = Shared.Playlist;

[FeatureState]
public record State
{
	public Playlist Playlist { get; init; } = new([]);
}
