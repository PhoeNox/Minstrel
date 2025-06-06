namespace Features.Tests;

public static class Dummies
{
	public static Playlist Playlist
		=> new([Song]);
	
	public static Song Song
		=> new("Path", "Title", "Artist", "Album", TimeSpan.FromMinutes(1));
}
