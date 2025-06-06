namespace Features.GamePhases;

public record Reducers
{
	[ReducerMethod]
	public static State SetGamePhase(State state, SetGamePhaseAction action)
		=> state with { Phase = action.GamePhase };
}
