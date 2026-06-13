namespace Backend.Library;

using System.Security.Cryptography;
using System.Text;
using Core;
using Infrastructure.FileSystem;

public sealed record LibraryEntry(string Id, Song Song);

public sealed class SongLibrary
{
	private readonly Dictionary<string, Song> songsById;

	public IReadOnlyList<LibraryEntry> Entries { get; }

	public SongLibrary(IFileSystemProvider fileSystem)
	{
		var entries = fileSystem.LoadSongs()
			.Select(song => new LibraryEntry(SongId(song.Path), song))
			.ToArray();
		Entries = entries;
		songsById = entries.ToDictionary(entry => entry.Id, entry => entry.Song);
	}

	public bool TryGetPath(string songId, out string path)
	{
		if (songsById.TryGetValue(songId, out var song))
		{
			path = song.Path;
			return true;
		}

		path = string.Empty;
		return false;
	}

	private static string SongId(string path)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
		return Convert.ToHexStringLower(hash)[..16];
	}
}
