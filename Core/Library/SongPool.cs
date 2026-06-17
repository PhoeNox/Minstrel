namespace Core.Library;

using Core;

public record PoolEntry(string Id, Song Song);

public record SongPool(
		PoolEntry[] Day,
		PoolEntry[] Night,
		PoolEntry[] All)
{
	public static SongPool From(Song[] day, Song[] night, Song[] all)
		=> new(Pool(day), Pool(night), Pool(all));

	public Dictionary<string, PoolEntry> EntriesById { get; } =
		Day.Concat(Night).Concat(All)
				.GroupBy(entry => entry.Id)
				.ToDictionary(group => group.Key, group => group.First());

	private static PoolEntry[] Pool(Song[] songs)
		=> songs.Select(song => new PoolEntry(SongId.From(song.Path), song)).ToArray();
}
