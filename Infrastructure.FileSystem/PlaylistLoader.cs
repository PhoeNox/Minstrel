namespace Infrastructure.FileSystem;

using Core;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

public interface IPlaylistLoader
{
	ILocalStorageService? LocalStorage { get; set; }
	
	Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists();
}

public class PlaylistLoader(IOptionsMonitor<MusicOptions> options) : IPlaylistLoader
{
	public ILocalStorageService? LocalStorage { get; set; }

	public async Task<(Song[] DayPlaylist, Song[] NightPlaylist)> LoadPlaylists()
	{
		var daySongPaths = await LoadPlaylist("day");
		var daySongs = daySongPaths.Select(CreateSong).ToArray();
		
		var nightSongPaths = await LoadPlaylist("night");
		var nightSongs = nightSongPaths.Select(CreateSong).ToArray();
		
		return (daySongs, nightSongs);
	}

	private async Task<string[]> LoadPlaylist(string playlistId)
	{
		return await LocalStorage!.GetItemAsync<string[]>(playlistId) ?? DefaultPlaylist();

		string[] DefaultPlaylist()
		{
			return playlistId == "day"
					? ["Clocktower - Cloud Seed.mp3"]
					:
					[
							"Bloodlust - Deflate.mp3",
							"Graveyard - The Liquid Kitchen.mp3",
							"I'm Growing Fangs - Great White Buffalo.mp3",
					];
		}
	}

	private Song CreateSong(string path)
	{
		var fullPath = Path.Join(options.CurrentValue.Directory, path);
		using var tags = TagLib.File.Create(fullPath);
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
