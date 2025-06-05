namespace Core.GamePhases;

public class Effects(IState<State> state)
{
	[EffectMethod]
	public Task OnSwitchGamePhase(SwitchGamePhaseAction action, IDispatcher dispatcher)
	{
		var gamePhase = state.Value.Phase;
		dispatcher.Dispatch(new SetGamePhaseAction(gamePhase));
		return Task.CompletedTask;
	}
}
