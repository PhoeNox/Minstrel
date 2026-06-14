namespace Backend.Library;

using System.Security.Cryptography;
using System.Text;
using Backend.Contracts;
using Core;
using Infrastructure.FileSystem;

public sealed record LibraryEntry(string Id, Song Song);

public sealed class SongLibrary
{
	private readonly IFileSystemProvider fileSystem;
	private readonly Dictionary<string, Song> songsById;

	public IReadOnlyList<LibraryEntry> Day { get; }

	public IReadOnlyList<LibraryEntry> Night { get; }

	public IReadOnlyList<LibraryEntry> All { get; }

	public SongLibrary(IFileSystemProvider fileSystem)
	{
		this.fileSystem = fileSystem;
		var (day, night) = fileSystem.LoadPlaylists();
		Day = day.Select(ToEntry).ToArray();
		Night = night.Select(ToEntry).ToArray();
		All = fileSystem.LoadSongs().Select(ToEntry).ToArray();
		songsById = Day.Concat(Night).Concat(All)
			.GroupBy(entry => entry.Id)
			.ToDictionary(group => group.Key, group => group.First().Song);
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

	public SongDto ToSongDto(string songId)
	{
		var song = songsById[songId];
		return new SongDto(songId, song.Title, song.Artist, song.Length.TotalSeconds);
	}

	public void SaveOrder(GamePhase phase, IReadOnlyList<string> orderedSongIds)
	{
		var songs = orderedSongIds.Select(id => songsById[id]).ToArray();
		fileSystem.SavePlaylist(phase, songs);
	}

	private static LibraryEntry ToEntry(Song song) => new(SongId(song.Path), song);

	private static string SongId(string path)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(path));
		return Convert.ToHexStringLower(hash)[..16];
	}
}
