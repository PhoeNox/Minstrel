namespace Infrastructure.FileSystem;

using Core;

public interface IPlaylistLoader
{
	Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists();
}

public class PlaylistLoader : IPlaylistLoader
{
	public Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists()
	{
		Song[] dayPlaylist = [CreateSong("DemoMusic/Clocktower - Cloud Seed.mp3")];
		Song[] nightPlaylist = [
				CreateSong("DemoMusic/Bloodlust - Deflate.mp3"),
				CreateSong("DemoMusic/Graveyard - The Liquid Kitchen.mp3"),
				CreateSong("DemoMusic/I'm Growing Fangs - Great White Buffalo.mp3"),
		];
		return Task.FromResult((dayPlaylist, nightPlaylist));
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

