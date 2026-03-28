namespace Core;

public static class SongExtensions
{
	extension(Song[] songs)
	{
		public Song[] WithSongAdded(Song song) => [..songs, song];

		public Song[] WithSongRemoved(Song song)
		{
			var list = songs.ToList();
			list.Remove(song);
			return list.ToArray();
		}

		public Song[] WithSongMoved(int oldIndex, int newIndex)
		{
			var list = songs.ToList();
			var song = list[oldIndex];
			list.RemoveAt(oldIndex);
			if (newIndex < list.Count)
				list.Insert(newIndex, song);
			else
				list.Add(song);
			return list.ToArray();
		}
	}
}
