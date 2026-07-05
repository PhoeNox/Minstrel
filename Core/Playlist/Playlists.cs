namespace Core.Playlist;

public static class Playlists
{
	public static int? IndexOfSong(PlaylistEntry[] entries, string songId)
	{
		var index = Array.FindIndex(entries, entry => entry.Id == songId);
		return index < 0 ? null : index;
	}

	public static PlaylistEntry[] Add(PlaylistEntry[] entries, PlaylistEntry entry) => [..entries, entry];

	public static PlaylistEntry[] RemoveAt(PlaylistEntry[] entries, int index)
	{
		if (index < 0 || index >= entries.Length)
			return entries;

		var list = entries.ToList();
		list.RemoveAt(index);
		return list.ToArray();
	}

	public static PlaylistEntry[] Move(PlaylistEntry[] entries, int oldIndex, int newIndex)
	{
		if (oldIndex < 0 || oldIndex >= entries.Length)
			return entries;

		var list = entries.ToList();
		var entry = list[oldIndex];
		list.RemoveAt(oldIndex);
		list.Insert(Math.Clamp(newIndex, 0, list.Count), entry);
		return list.ToArray();
	}

	public static PlaylistEntry[] Shuffle(PlaylistEntry[] entries, int? pinnedIndex, Random random)
	{
		var order = Enumerable.Range(0, entries.Length).ToArray();
		var start = 0;
		if (pinnedIndex is { } pinned)
		{
			(order[0], order[pinned]) = (order[pinned], order[0]);
			start = 1;
		}

		for (var i = order.Length - 1; i > start; i--)
		{
			var j = start + random.Next(i - start + 1);
			(order[i], order[j]) = (order[j], order[i]);
		}

		return order.Select(index => entries[index]).ToArray();
	}
}
