namespace Infrastructure.Playback;

using Microsoft.JSInterop;

public class PlaybackService(IJSRuntime jsRuntime)
{
	private AudioContext context = null!;
	private AudioDestinationNode destination = null!;

	private readonly Dictionary<string, ActiveSong> activeSongs = new();
	private readonly TimeSpan fadeDuration = TimeSpan.FromSeconds(5);

	public event Action<Song> SongEnded = _ => { };

	public SongRepository SongRepository { get; private set; } = null!;
	
	public async Task Initialize()
	{
		context = await AudioContext.CreateAsync(jsRuntime);
		destination = await context.GetDestinationAsync();

		SongRepository = new SongRepository(context);
	}

	public async Task PlaySong(Song song, float gain)
	{
		var activeSong = await GetOrCreateActiveSong(song);
		if (activeSong.FadeOutCts is not null)
		{
			await activeSong.FadeOutCts.CancelAsync();
			activeSong.FadeOutCts = null;
		}
		activeSong.SongEndMonitoringCts = new CancellationTokenSource();
		_ = MonitorSongEnd(activeSong, activeSong.SongEndMonitoringCts!.Token);

		await Play(activeSong.SongNode);
		activeSongs[song.Path].LastStarted = DateTime.Now;
		await FadeIn(activeSong.GainNode, gain, activeSong.SongEndMonitoringCts!.Token);
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
		
		var activeSong = new ActiveSong(song, gainNode, songNode);
		activeSongs[song.Path] = activeSong;
		await activeSong.SongNode.StartAsync();
		return activeSong;
	}
	
	private async Task MonitorSongEnd(ActiveSong activeSong, CancellationToken ct)
	{
		var songLeft = activeSong.Song.Length - activeSong.AlreadyPlayed - fadeDuration;
		await Task.Delay(songLeft, ct);
		SongEnded(activeSong.Song);
	}

	public async Task PauseSong(Song song, bool reset)
	{
		var activeSong = activeSongs[song.Path];
		await activeSong.SongEndMonitoringCts!.CancelAsync();
		activeSong.FadeOutCts = new CancellationTokenSource();
		await Fadeout(activeSong.GainNode, activeSong.FadeOutCts!.Token);;
		activeSong.AlreadyPlayed += DateTime.Now - activeSong.LastStarted!.Value;
		await Pause(activeSong.SongNode);
		if (reset)
			activeSongs.Remove(song.Path);
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

	private async Task FadeIn(GainNode gainNode, float gainValue, CancellationToken ct)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.CancelScheduledValuesAsync(currentTime);
		var currentValue = await gain.GetValueAsync();
		await gain.SetValueAtTimeAsync(currentValue, currentTime);
		await gain.LinearRampToValueAtTimeAsync(gainValue, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration, ct);
	}

	private async Task Fadeout(GainNode gainNode, CancellationToken ct)
	{
		var currentTime = await context.GetCurrentTimeAsync();
		var gain = await gainNode.GetGainAsync();
		await gain.CancelScheduledValuesAsync(currentTime);
		var gainValue = await gain.GetValueAsync();
		await gain.SetValueAtTimeAsync(gainValue, currentTime);
		await gain.LinearRampToValueAtTimeAsync(0, currentTime + fadeDuration.TotalSeconds);
		await Task.Delay(fadeDuration, ct);
	}

	public async Task SetGain(Song song, float value)
	{
		var activeSong = activeSongs[song.Path];
		var gainNode = activeSong.GainNode;
		var gain = await gainNode.GetGainAsync();
		var currentTime = await context.GetCurrentTimeAsync();
		await gain.CancelScheduledValuesAsync(currentTime);
		await gain.SetValueAsync(value);
	}
	
	private record ActiveSong(
		Song Song,
		GainNode GainNode,
		AudioBufferSourceNode SongNode)
	{
		public TimeSpan AlreadyPlayed { get; set; } = TimeSpan.Zero;
		public DateTime? LastStarted { get; set; }
		public CancellationTokenSource? SongEndMonitoringCts { get; set; }
		public CancellationTokenSource? FadeOutCts { get; set; }
	}
}
