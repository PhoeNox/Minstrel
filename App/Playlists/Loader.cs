namespace App.Playlists;

using Playlists = Core.Playlists;

public static class Loader
{
	public static Playlists LoadPlaylists()
	{
		var dayPlaylist = LoadPlaylist("Day");
		var nightPlaylist = LoadPlaylist("Night");
		return new Playlists(dayPlaylist, nightPlaylist);
	}

	private static Playlist LoadPlaylist(string playlistName)
	{
		var musicDirectory = Path.Combine("wwwroot", "Music");
		var playlistPath = Path.Combine(musicDirectory, playlistName, "playlist.txt");
		var songFiles = File.ReadAllLines(playlistPath);
		var songs = songFiles
			.Select(x => Path.Combine(musicDirectory, playlistName, x))
			.Select(CreateSong)
			.ToArray();
		return new Playlist(songs);
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

