namespace Backend.Features.Playback;

public sealed class PlaybackClock(PlaybackSession session) : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			session.Tick();
		}
	}
}
