namespace Infrastructure.FileSystem;

using Core;
using Microsoft.Extensions.Options;

public interface IFileSystemProvider
{
	Song[] LoadSongs();

	(Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists();

	void SavePlaylist(GamePhase gamePhase, IReadOnlyList<string> songPaths);
}

public class FileSystemProvider(IOptionsMonitor<MusicOptions> options) : IFileSystemProvider
{
	private string MusicDirectory
	{
		get
		{
			var configured = options.CurrentValue.Directory;
			return Path.IsPathRooted(configured)
				? configured
				: Path.Combine(AppContext.BaseDirectory, configured);
		}
	}

	public (Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists()
	{
		var daySongs = LoadPlaylist(GamePhase.Day);
		var nightSongs = LoadPlaylist(GamePhase.Night);
		return (daySongs, nightSongs);
	}

	public void SavePlaylist(GamePhase gamePhase, IReadOnlyList<string> songPaths)
	{
		var playlist = PlaylistPath(gamePhase);
		File.WriteAllLines(playlist, songPaths.Select(path => Path.GetRelativePath(MusicDirectory, path)));
	}

	private Song[] LoadPlaylist(GamePhase gamePhase)
	{
		var playlist = PlaylistPath(gamePhase);
		var songPaths = File.ReadAllLines(playlist);
		return songPaths.SelectMany(CreateSongFromFile).ToArray();
	}

	private string PlaylistPath(GamePhase gamePhase)
	{
		var fileName = gamePhase == GamePhase.Day ? "day.m3u" : "night.m3u";
		return Path.Combine(MusicDirectory, fileName);
	}

	public Song[] LoadSongs()
	{
		var files = Directory.EnumerateFiles(MusicDirectory, "*", SearchOption.AllDirectories);
		return files.SelectMany(CreateSongFromFile).ToArray();
	}

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
					Album: tags.Tag.Album ?? "Unknown Album",
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
