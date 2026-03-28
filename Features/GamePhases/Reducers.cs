namespace Features.GamePhases;

public static class Reducers
{
	[ReducerMethod]
	public static State SetGamePhase(State state, SetGamePhaseAction action)
		=> state with { Phase = action.GamePhase };
}
