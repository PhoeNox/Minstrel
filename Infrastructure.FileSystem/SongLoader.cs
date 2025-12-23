namespace Infrastructure.FileSystem;

using Core;
using Microsoft.Extensions.Options;

public interface ISongLoader
{
	Song[] LoadSongs();
}

public class SongLoader(IOptionsMonitor<MusicOptions> options) : ISongLoader
{
	public Song[] LoadSongs()
	{
		var files = Directory.EnumerateFiles(
				options.CurrentValue.Directory,
				"*",
				SearchOption.AllDirectories);
		return files.SelectMany(CreateSongFromFile).ToArray();
	}
	
	private static IEnumerable<Song> CreateSongFromFile(string path)
	{
		try
		{
			return [SongFactory.Create(path)];
		}
		catch
		{
			return [];
		}
	}
}
