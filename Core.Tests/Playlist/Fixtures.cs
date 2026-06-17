namespace Core.Tests.Playlist;

using Core.Playlist;

internal static class Fixtures
{
	public static PlaylistEntry Entry(string id, double length) =>
		new(id, Path: $"{id}.mp3", Title: id, Artist: id, length);
}
