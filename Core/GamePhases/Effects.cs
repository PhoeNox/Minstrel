namespace Core.GamePhases;

public class Effects(IState<State> state)
{
	[EffectMethod]
	public Task OnSwitchGamePhase(SwitchGamePhaseAction action, IDispatcher dispatcher)
	{
		var gamePhase = state.Value.Phase;
		var nextPhase = gamePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		dispatcher.Dispatch(new SetGamePhaseAction(nextPhase));
		return Task.CompletedTask;
	}
}
