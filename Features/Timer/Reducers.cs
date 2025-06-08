namespace Features.Timer;

public static class Reducers
{
	[ReducerMethod]
	public static State SetTimeLeft(State state, SetTimeLeftAction action)
		=> state with { TimeLeft = action.TimeLeft };
	
	[ReducerMethod]
	public static State SetIsTimeRunning(State state, SetIsTimeRunningAction action)
	{
		var newState = state with {IsRunning = action.IsRunning};
		if (!action.IsRunning)
			newState = newState with {TimeLeft = TimeSpan.Zero};
		return newState;
	}
}
