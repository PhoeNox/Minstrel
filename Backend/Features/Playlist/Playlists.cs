namespace Backend.Features.Playlist;

public static class Playlists
{
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

	public static (PlaylistEntry[] Entries, int[] Permutation) Shuffle(PlaylistEntry[] entries, Random random)
	{
		var permutation = Enumerable.Range(0, entries.Length).ToArray();
		for (var i = permutation.Length - 1; i > 0; i--)
		{
			var j = random.Next(i + 1);
			(permutation[i], permutation[j]) = (permutation[j], permutation[i]);
		}

		return (permutation.Select(index => entries[index]).ToArray(), permutation);
	}
}
