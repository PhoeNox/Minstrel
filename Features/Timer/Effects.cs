namespace Features.Timer;

using Playback;

public class Effects(TimeProvider timeProvider, IState<State> state)
{
	private static readonly Song GongSong = new("wwwroot/audio/Gong.mp3", "Gong", "Gong", "Gong", TimeSpan.FromSeconds(11));
	
	[EffectMethod]
	public async Task StartTimer(StartTimerAction action, IDispatcher dispatcher)
	{
		dispatcher.Dispatch(new SetIsTimeRunningAction(true));
		dispatcher.Dispatch(new SetTimeLeftAction(action.Duration));
		dispatcher.Dispatch(new LoadSongSignal(GongSong));

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
					dispatcher.Dispatch(new PlaySoundSignal(GongSong, state.Value.GongGain));
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
