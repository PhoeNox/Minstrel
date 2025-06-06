namespace Infrastructure.FileSystem;

using Core;

public interface IPlaylistLoader
{
	Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists();
}

public class PlaylistLoader : IPlaylistLoader
{
	public async Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists()
	{
		var dayPlaylist = await LoadPlaylist("Day");
		var nightPlaylist = await LoadPlaylist("Night");
		return (dayPlaylist, nightPlaylist);
	}

	private static async Task<Song[]> LoadPlaylist(string playlistName)
	{
		var musicDirectory = Path.Combine("wwwroot", "Music");
		var playlistPath = Path.Combine(musicDirectory, playlistName, "playlist.txt");
		var songFiles = await File.ReadAllLinesAsync(playlistPath);
		return songFiles
			.Select(x => Path.Combine(musicDirectory, playlistName, x))
			.Select(CreateSong)
			.ToArray();
	}

	private static Song CreateSong(string path)
	{
		using var tags = TagLib.File.Create(path);
		return new Song
		(
			Path: path,
			Title: tags.Tag.Title ?? Path.GetFileNameWithoutExtension(path),
			Artist: string.Join(", ", tags.Tag.Performers),
			Album: tags.Tag.Album ?? "Unknown Album",
			Length: tags.Properties.Duration
		);
	}
}

