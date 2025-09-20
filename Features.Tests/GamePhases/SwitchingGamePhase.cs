namespace Features.Tests.GamePhases;

using Features.GamePhases;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.GamePhases.State;

[UseAppContext]
public class SwitchingGamePhase(AppContext appContext, IDispatcher dispatcher)
{
	[Test]
	[Arguments(GamePhase.Day, GamePhase.Night)]
	[Arguments(GamePhase.Night, GamePhase.Day)]
	public async Task SwitchesToOtherPhase(GamePhase currentPhase, GamePhase nextPhase)
	{
		dispatcher.Dispatch(new SetGamePhaseAction(currentPhase));
		dispatcher.Dispatch(new SetPlaylistsAction([Dummies.Song], [Dummies.Song]));
		
		dispatcher.Dispatch(new SwitchGamePhaseAction());

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.Phase).IsEqualTo(nextPhase);
	}
}
