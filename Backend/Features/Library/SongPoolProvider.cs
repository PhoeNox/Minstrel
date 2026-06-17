namespace Backend.Features.Library;

using Core;
using Core.Library;
using FileSystem;

public class SongPoolProvider
{
	public SongPool SongPool { get; }

	public SongPoolProvider(IFileSystemProvider fileSystem)
	{
		var (day, night) = fileSystem.LoadPlaylists();
		var all = fileSystem.LoadSongs();
		SongPool = new SongPool(
				Day: day.Select(ToEntry).ToArray(),
				Night: night.Select(ToEntry).ToArray(),
				All: all.Select(ToEntry).ToArray());
	}
	
	private static PoolEntry ToEntry(Song song) 
		=> new(SongId.From(song.Path), song);
}
