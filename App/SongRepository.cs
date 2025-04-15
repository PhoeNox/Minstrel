namespace App;

using KristofferStrube.Blazor.WebAudio;

public class SongRepository(AudioContext audioContext)
{
	private readonly Dictionary<string, Task<AudioBuffer>> preloadedSongs = new();

	public Task<AudioBuffer> Load(Song song)
	{
		if (preloadedSongs.TryGetValue(song.Path, out var loadingTask))
			return loadingTask;

		var preloadTask = Load(song.Path);
		preloadedSongs.Add(song.Path, preloadTask);
		return preloadTask;
	}

	private async Task<AudioBuffer> Load(string path)
	{
		var buffer = await File.ReadAllBytesAsync(path);
		var audioBuffer = await audioContext.DecodeAudioDataAsync(buffer);
		return audioBuffer;
	}
}
