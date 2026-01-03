namespace Features.Tests.GamePhases;

using Features.GamePhases;
using Features.Playlists;
using Microsoft.Extensions.DependencyInjection;
using State = Features.GamePhases.State;

public class SwitchingGamePhase
{
	[ClassDataSource<AppContext>]
	public required AppContext AppContext { get; init; }
	
	[Test]
	[Arguments(GamePhase.Day, GamePhase.Night)]
	[Arguments(GamePhase.Night, GamePhase.Day)]
	public async Task SwitchesToOtherPhase(GamePhase currentPhase, GamePhase nextPhase)
	{
		AppContext.Dispatcher.Dispatch(new SetGamePhaseAction(currentPhase));
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Day, [Dummies.Song]));
		AppContext.Dispatcher.Dispatch(new SetPlaylistsAction(GamePhase.Night, [Dummies.Song]));
		
		AppContext.Dispatcher.Dispatch(new SwitchGamePhaseAction());

		var state = AppContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.Phase).IsEqualTo(nextPhase);
	}
}
