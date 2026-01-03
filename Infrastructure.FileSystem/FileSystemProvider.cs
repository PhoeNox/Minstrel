namespace Infrastructure.FileSystem;

using Core;
using Microsoft.Extensions.Options;

public interface IFileSystemProvider
{
	Song[] LoadSongs();

	(Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists();

	void SavePlaylist(GamePhase gamePhase, Song[] songs);
}

public class FileSystemProvider(IOptionsMonitor<MusicOptions> options) : IFileSystemProvider
{
	public (Song[] DayPlaylist, Song[] NightPlaylist) LoadPlaylists()
	{
		var daySongs = LoadPlaylist(GamePhase.Day);
		var nightSongs = LoadPlaylist(GamePhase.Night);
		return (daySongs, nightSongs);
	}

	public void SavePlaylist(GamePhase gamePhase, Song[] songs)
	{
		var fileName = GetPlaylistFileName(gamePhase);
		File.WriteAllLines(fileName, songs.Select(song => song.Path));
	}

	private static Song[] LoadPlaylist(GamePhase gamePhase)
	{
		var playlist = GetPlaylistFileName(gamePhase);
		var songPaths = File.ReadAllLines(playlist);
		return songPaths.SelectMany(CreateSongFromFile).ToArray();
	}

	private static string GetPlaylistFileName(GamePhase gamePhase)
	{
		return gamePhase == GamePhase.Day ? "day.m3u" : "night.m3u";
	}

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
			using var tags = TagLib.File.Create(path);
			var song = new Song
			(
					Path: path,
					Title: tags.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
					Artist: string.Join(", ", tags.Tag.Performers),
					Album: tags.Tag.Album ?? "Unknown Album",
					Length: tags.Properties.Duration
			);
			return [song];
		}
		catch
		{
			return [];
		}
	}
}
