namespace FileSystem;

using Core;
using Microsoft.Extensions.Options;

public interface IFileSystemProvider
{
	string MusicDirectory { get; }

	Song[] ReadSongs(IEnumerable<string> paths);

	IEnumerable<string> EnumerateSongs();

	string[] ReadLines(string path);

	void WriteLines(string path, IEnumerable<string> lines);
}

public class FileSystemProvider(IOptionsMonitor<MusicOptions> options) : IFileSystemProvider
{
	public string MusicDirectory
	{
		get
		{
			var configured = options.CurrentValue.Directory;
			return Path.IsPathRooted(configured)
				? configured
				: Path.Combine(AppContext.BaseDirectory, configured);
		}
	}

	public IEnumerable<string> EnumerateSongs()
		=> Directory.EnumerateFiles(MusicDirectory, "*", SearchOption.AllDirectories);

	public string[] ReadLines(string path)
		=> File.ReadAllLines(path);

	public void WriteLines(string path, IEnumerable<string> lines)
		=> File.WriteAllLines(path, lines);

	public Song[] ReadSongs(IEnumerable<string> paths)
		=> paths.SelectMany(CreateSongFromFile).ToArray();

	private IEnumerable<Song> CreateSongFromFile(string path)
	{
		var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(MusicDirectory, path);
		try
		{
			using var tags = TagLib.File.Create(fullPath);
			var song = new Song
			(
					Path: fullPath,
					Title: tags.Tag.Title ?? Path.GetFileNameWithoutExtension(fullPath),
					Artist: string.Join(", ", tags.Tag.Performers),
					Length: tags.Properties.Duration
			);
			return [song];
		}
		catch (Exception)
		{
			return [];
		}
	}
}
