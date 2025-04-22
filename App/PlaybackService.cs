namespace App;

using KristofferStrube.Blazor.WebAudio;
using Microsoft.JSInterop;

public class PlaybackService(IJSRuntime jsRuntime)
{
	private AudioContext context = null!;
	private AudioDestinationNode destination = null!;

	private CancellationTokenSource? cts;

	private string? currentSongPath;
	private readonly Dictionary<string, ActiveSong> activeSongs = new();
	
	private float currentGain = 1;

	private readonly TimeSpan fadeDuration = TimeSpan.FromSeconds(5);

	public event Action SongEnded = () => { };

	public SongRepository SongRepository { get; private set; } = null!;
	
	public async Task Initialize()
	{
		context = await AudioContext.CreateAsync(jsRuntime);
		destination = await context.GetDestinationAsync();

		SongRepository = new SongRepository(context);
	}

	public async Task PlaySong(Song song, float gain)
	{
		_ = StopCurrentlyPlayingSong(currentSongPath);

		currentSongPath = song.Path;
		currentGain = gain;

		if (cts is not null)
		{
			await cts.CancelAsync();
			cts.Dispose();
		}

		var activeSong = await GetOrCreateActiveSong(song);

		cts = new CancellationTokenSource();
		_ = MonitorSongEnd(song, activeSong.AlreadyPlayed, cts.Token);

		await Play(activeSong.SongNode);
		activeSongs[song.Path].LastStarted = DateTime.Now;
		await FadeIn(activeSong.GainNode);
	}

	private async Task StopCurrentlyPlayingSong(string? songPath)
	{
		if (songPath is null || !activeSongs.TryGetValue(songPath, out var activeSong))
			return;

		await Fadeout(activeSong.GainNode);
		activeSong.AlreadyPlayed += DateTime.Now - activeSong.LastStarted!.Value;
		await Pause(activeSong.SongNode);
	}

	private async Task<ActiveSong> GetOrCreateActiveSong(Song song)
	{
		if (activeSongs.TryGetValue(song.Path, out var existingSong))
			return existingSong;

		var songBuffer = await SongRepository.Load(song);

		var songNode = await context.CreateBufferSourceAsync();
		await songNode.SetBufferAsync(songBuffer);

		var gainNode = await context.CreateGainAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.SetValueAsync(0);

		await songNode.ConnectAsync(gainNode);
		await gainNode.ConnectAsync(destination);

		var activeSong = new ActiveSong(gainNode, songNode);
		activeSongs[song.Path] = activeSong;
		await activeSong.SongNode.StartAsync();
		return activeSong;
	}

	private async Task MonitorSongEnd(Song song, TimeSpan alreadyPlayed, CancellationToken ct)
	{
		var songLeft = song.Length - alreadyPlayed - fadeDuration;
		await Task.Delay(songLeft, ct);

		if (activeSongs.Remove(song.Path, out var activeSong))
		{
			await activeSong.GainNode.DisposeAsync();
			await activeSong.SongNode.DisposeAsync();
		}
		SongEnded();
	}

	private static async Task Play(AudioBufferSourceNode songNode)
	{
		var playbackRate = await songNode.GetPlaybackRateAsync();
		await playbackRate.SetValueAsync(1);
	}

	private static async Task Pause(AudioBufferSourceNode songNode)
	{
		var playbackRate = await songNode.GetPlaybackRateAsync();
		await playbackRate.SetValueAsync(0);
	}

	private async Task FadeIn(GainNode gainNode)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		var gainValue = await gain.GetValueAsync();
		await gain.SetValueAtTimeAsync(gainValue, currentTime);
		await gain.LinearRampToValueAtTimeAsync(currentGain, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration);
	}

	private async Task Fadeout(GainNode gainNode)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		var gainValue = await gain.GetValueAsync();
		await gain.SetValueAtTimeAsync(gainValue, currentTime);
		await gain.LinearRampToValueAtTimeAsync(0, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration);
	}

	public async Task SetGain(float value)
	{
		currentGain = value;

		if (currentSongPath is null || !activeSongs.TryGetValue(currentSongPath, out var activeSong))
			return;

		var gainNode = activeSong.GainNode;
		var gain = await gainNode.GetGainAsync();
		await gain.SetValueAsync(currentGain);
	}
	
	private record ActiveSong(GainNode GainNode, AudioBufferSourceNode SongNode)
	{
		public TimeSpan AlreadyPlayed { get; set; } = TimeSpan.Zero;
		public DateTime? LastStarted { get; set; }
	}
}
