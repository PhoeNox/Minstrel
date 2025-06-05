namespace Core.GamePhases;

public record Reducers
{
	[ReducerMethod]
	public static State SwitchGamePhase(State state, SwitchGamePhaseAction action)
	{
		var gamePhase = state.Phase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		return state with { Phase = gamePhase };
	}
}
