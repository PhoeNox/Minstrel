namespace Backend.Tests.Library;

using Core;
using FileSystem;

public sealed class FakeFileSystem(Song[] day, Song[] night, Song[]? alsoOnDisk = null)
		: IFileSystemProvider
{
	private readonly Dictionary<GamePhase, Song[]> playlists = new()
	{
			[GamePhase.Day] = day,
			[GamePhase.Night] = night,
	};

	private readonly Song[] alsoOnDisk = alsoOnDisk ?? [];
	private readonly Dictionary<GamePhase, string[]> saved = new();

	public string[] Saved(GamePhase phase)
		=> saved[phase];

	public Song[] LoadSongs()
		=> playlists.Values.SelectMany(songs => songs).Concat(alsoOnDisk).ToArray();

	public (Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists()
		=> (playlists[GamePhase.Day], playlists[GamePhase.Night]);

	public void SavePlaylist(GamePhase gamePhase, IReadOnlyList<string> songPaths)
		=> saved[gamePhase] = songPaths.ToArray();
}

public static class TestSongs
{
	public static Song At(string path, TimeSpan? length = null)
		=> new(path, Title: $"Title {path}", Artist: $"Artist {path}", Album: "Album",
				Length: length ?? TimeSpan.FromMinutes(3));
}
