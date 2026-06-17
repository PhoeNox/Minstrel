namespace Backend.Tests.Library;

using Core;
using Infrastructure.FileSystem;

public sealed class FakeFileSystem : IFileSystemProvider
{
	private readonly Dictionary<GamePhase, Song[]> playlists;
	private readonly Song[] alsoOnDisk;
	private readonly Dictionary<GamePhase, string[]> saved = new();

	public FakeFileSystem(Song[] day, Song[] night, Song[]? alsoOnDisk = null)
	{
		playlists = new Dictionary<GamePhase, Song[]>
		{
			[GamePhase.Day] = day,
			[GamePhase.Night] = night,
		};
		this.alsoOnDisk = alsoOnDisk ?? [];
	}

	public string[] Saved(GamePhase phase) => saved[phase];

	public Song[] LoadSongs() =>
		playlists.Values.SelectMany(songs => songs).Concat(alsoOnDisk).ToArray();

	public (Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists() =>
		(playlists[GamePhase.Day], playlists[GamePhase.Night]);

	public void SavePlaylist(GamePhase gamePhase, IReadOnlyList<string> songPaths) =>
		saved[gamePhase] = songPaths.ToArray();
}

public static class TestSongs
{
	public static Song At(string path, TimeSpan? length = null) =>
		new(path, Title: $"Title {path}", Artist: $"Artist {path}", Album: "Album", Length: length ?? TimeSpan.FromMinutes(3));
}
