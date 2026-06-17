namespace FileSystem.Tests;

using Core;

public sealed class FakeFileSystem(Song[] day, Song[] night, Song[]? alsoOnDisk = null)
		: IFileSystemProvider
{
	private readonly Dictionary<GamePhase, Song[]> playlists = new()
	{
			[GamePhase.Day] = day,
			[GamePhase.Night] = night,
	};

	private readonly Song[] onDisk = day.Concat(night).Concat(alsoOnDisk ?? []).ToArray();
	private readonly Dictionary<GamePhase, string[]> saved = new();

	public string MusicDirectory => "/music";

	public string[] Saved(GamePhase phase)
		=> saved[phase];

	public IEnumerable<string> EnumerateSongs()
		=> onDisk.Select(song => song.Path);

	public string[] ReadLines(string path)
	{
		var gamePhase = PhaseOf(path);
		return playlists[gamePhase].Select(song => song.Path).ToArray();
	}

	public Song[] ReadSongs(IEnumerable<string> paths)
	{
		var byPath = onDisk.ToDictionary(song => song.Path);
		return paths
				.Where(byPath.ContainsKey)
				.Select(path => byPath[path])
				.ToArray();
	}

	public void WriteLines(string path, IEnumerable<string> lines)
	{
		var gamePhase = PhaseOf(path);
		saved[gamePhase] = lines.ToArray();
	}

	private static GamePhase PhaseOf(string path)
		=> path.EndsWith("day.m3u") ? GamePhase.Day : GamePhase.Night;
}

public static class TestSongs
{
	public static Song At(string name, TimeSpan? length = null)
		=> new($"/music/{name}", Title: $"Title {name}", Artist: $"Artist {name}",
				Length: length ?? TimeSpan.FromMinutes(3));
}
