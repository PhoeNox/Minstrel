namespace Core.Tests.GamePhases;

using Core.GamePhases;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;
using Playlists;
using State = Core.GamePhases.State;

public class SwitchingGamePhase
{
	[Test]
	[Arguments(GamePhase.Day, GamePhase.Night)]
	[Arguments(GamePhase.Night, GamePhase.Day)]
	public async Task SwitchesToOtherPhase(GamePhase currentPhase, GamePhase nextPhase)
	{
		var testContext = new TestContext();
		var dispatcher = testContext.Services.GetRequiredService<IDispatcher>();
		dispatcher.Dispatch(new SetGamePhaseAction(currentPhase));
		dispatcher.Dispatch(new SetPlaylistsAction(
			new Playlist([Dummies.Song]),
			new Playlist([Dummies.Song])));
		
		dispatcher.Dispatch(new SwitchGamePhaseAction());

		var state = testContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.Phase).IsEqualTo(nextPhase);
	}
}
