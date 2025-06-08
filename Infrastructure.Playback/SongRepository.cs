namespace Infrastructure.Playback;

public class SongRepository(AudioContext audioContext)
{
	private readonly Dictionary<string, Task<AudioBuffer>> preloadedSongs = new();

	public event Action<Song> SongLoaded = _ => { };

	public Task<AudioBuffer> Load(Song song)
	{
		if (preloadedSongs.TryGetValue(song.Path, out var loadingTask))
			return loadingTask;

		var preloadTask = LoadInternal(song);
		preloadedSongs.Add(song.Path, preloadTask);
		return preloadTask;
	}

	private async Task<AudioBuffer> LoadInternal(Song song)
	{
		var buffer = await File.ReadAllBytesAsync(song.Path);
		var audioBuffer = await audioContext.DecodeAudioDataAsync(buffer);
		SongLoaded(song);
		return audioBuffer;
	}
}
