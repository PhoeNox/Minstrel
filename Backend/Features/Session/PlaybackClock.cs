namespace Backend.Features.Session;

public sealed class PlaybackClock(LiveSession session) : BackgroundService
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
