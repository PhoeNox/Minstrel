namespace Features.Timer;

public class Effects(TimeProvider timeProvider, IState<State> state)
{
	[EffectMethod]
	public async Task StartTimer(StartTimerAction action, IDispatcher dispatcher)
	{
		dispatcher.Dispatch(new SetIsTimeRunningAction(true));
		dispatcher.Dispatch(new SetTimeLeftAction(action.Duration));

		var endTime = timeProvider.GetUtcNow() + action.Duration;
		
		using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), timeProvider);
		
		try
		{
			while (await timer.WaitForNextTickAsync())
			{
				var currentTime = timeProvider.GetUtcNow();
				var timeLeft = endTime - currentTime;
				
				dispatcher.Dispatch(new SetTimeLeftAction(timeLeft));

				if (timeLeft <= TimeSpan.Zero || !state.Value.IsRunning)
				{
					dispatcher.Dispatch(new SetIsTimeRunningAction(false));
					break;
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Timer was canceled
		}
	}
}
