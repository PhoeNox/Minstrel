namespace Backend.Sessions;

public sealed class SessionClock(params Action[] ticks) : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(250);

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			foreach (var tick in ticks)
				tick();
		}
	}
}
