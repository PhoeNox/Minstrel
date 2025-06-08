namespace Features.GamePhases;

using Playback;

public class Effects(IState<State> state)
{
	[EffectMethod]
	public Task OnSwitchGamePhase(SwitchGamePhaseAction action, IDispatcher dispatcher)
	{
		var currentGamePhase = state.Value.Phase;
		var nextPhase = currentGamePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		dispatcher.Dispatch(new SetGamePhaseAction(nextPhase));
		dispatcher.Dispatch(new SwitchPlaylist(currentGamePhase, nextPhase));
		return Task.CompletedTask;
	}
}
