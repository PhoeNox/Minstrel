namespace App;

using KristofferStrube.Blazor.WebAudio;
using Microsoft.JSInterop;

public class PlaybackService(IJSRuntime jsRuntime)
{
	private AudioContext context = null!;
	private AudioDestinationNode destination = null!;
	
	private bool didSongChange;

	private string? currentSongPath;
	private readonly Dictionary<string, SongNodes> activeNodes = new();

	private readonly TimeSpan fadeDuration = TimeSpan.FromSeconds(5);
	
	public event Action SongEnded = () => { };

	public SongRepository SongRepository { get; private set; } = null!;

	public async Task Initialize()
	{
		context = await AudioContext.CreateAsync(jsRuntime);
		destination = await context.GetDestinationAsync();

		SongRepository = new SongRepository(context);
	}

	public async Task PlaySong(Song song, DateTime? songStartedAt)
	{
		currentSongPath = song.Path;
		didSongChange = true;
		
		var songBuffer = await SongRepository.Load(song);
		
		var songNode = await context.CreateBufferSourceAsync();
		await songNode.SetBufferAsync(songBuffer);
		
		var gainNode = await context.CreateGainAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.SetValueAsync(0);
		
		await songNode.ConnectAsync(gainNode);
		await gainNode.ConnectAsync(destination);
		
		activeNodes[song.Path] = new SongNodes(gainNode, songNode);
		
		double? offset = songStartedAt is null
			? null
			: (DateTime.Now - songStartedAt.Value).TotalSeconds;
		await songNode.StartAsync(offset: offset);
		
		_ = MonitorSongEnd(song, songStartedAt);

		await FadeIn(gainNode);
	}
	
	private async Task MonitorSongEnd(Song song, DateTime? songStartedAt)
	{
		didSongChange = false;
		var songEnd = songStartedAt + song.Length - fadeDuration;
		while (didSongChange is false && DateTime.Now < songEnd)
			await Task.Delay(100);
		if (didSongChange is false)
			SongEnded();
	}

	public async Task StopCurrentlyPlayingSong()
	{
		if (currentSongPath is null || !activeNodes.TryGetValue(currentSongPath, out var nodes))
			return;
		
		await Fadeout(nodes.GainNode);
		await nodes.SongNode.StopAsync();
		await nodes.GainNode.DisposeAsync();
		await nodes.SongNode.DisposeAsync();
	}
	
	private async Task FadeIn(GainNode gainNode)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.LinearRampToValueAtTimeAsync(1, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration);
	}

	private async Task Fadeout(GainNode gainNode)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.SetValueAtTimeAsync(1, currentTime);
		await gain.LinearRampToValueAtTimeAsync(0, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration);
	}

	private record SongNodes(GainNode GainNode, AudioBufferSourceNode SongNode);
}
