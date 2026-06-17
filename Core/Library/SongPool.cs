namespace Core.Library;

using Core;

public record PoolEntry(string Id, Song Song);

public record SongPool(
		PoolEntry[] Day,
		PoolEntry[] Night,
		PoolEntry[] All)
{
	public Dictionary<string, PoolEntry> EntriesById { get; } =
		Day.Concat(Night).Concat(All)
				.GroupBy(entry => entry.Id)
				.ToDictionary(group => group.Key, group => group.First());
}
