namespace Core.Tests;

public static class Dummies
{
	public static Song Song
		=> new("Path", "Title", "Artist", "Album", TimeSpan.FromMinutes(1));
}
