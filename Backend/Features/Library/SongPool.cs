namespace Backend.Features.Library;

using Backend.Contracts;
using Core;
using Infrastructure.FileSystem;

public sealed record PoolEntry(string Id, Song Song);

public sealed class SongPool
{
	private readonly Dictionary<string, PoolEntry> entriesById;

	public PoolEntry[] Day { get; }
	public PoolEntry[] Night { get; }
	public PoolEntry[] All { get; }

	public SongPool(IFileSystemProvider fileSystem)
	{
		var (day, night) = fileSystem.LoadPlaylists();
		Day = day.Select(ToEntry).ToArray();
		Night = night.Select(ToEntry).ToArray();
		All = fileSystem.LoadSongs().Select(ToEntry).ToArray();
		entriesById = Day.Concat(Night).Concat(All)
			.GroupBy(entry => entry.Id)
			.ToDictionary(group => group.Key, group => group.First());
	}

	public PoolEntry Get(string songId) => entriesById[songId];

	public bool TryGetPath(string songId, out string path)
	{
		if (!entriesById.TryGetValue(songId, out var entry))
		{
			path = string.Empty;
			return false;
		}

		path = entry.Song.Path;
		return true;
	}

	public SongDto ToSongDto(string songId)
	{
		var song = entriesById[songId].Song;
		return new SongDto(songId, song.Title, song.Artist, song.Length.TotalSeconds);
	}

	private static PoolEntry ToEntry(Song song) => new(SongId.From(song.Path), song);
}
