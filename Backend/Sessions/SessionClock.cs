namespace Backend.Sessions;

public sealed class SessionClock(Action tick) : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			tick();
		}
	}
}
