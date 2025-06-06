namespace Features.Tests.Playlists;

using Features.Playlists;
using Fluxor;
using Microsoft.Extensions.DependencyInjection;

[UseAppContext]
public class SetGainForGamePhase(AppContext appContext, IDispatcher dispatcher)
{
	[Test]
	public async Task SetDayGain()
	{
		dispatcher.Dispatch(new SetDayGainAction(0.5f));

		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.DayPlaylist.Gain).IsEqualTo(0.5f);
	}
	
	[Test]
	public async Task SetNightGain()
	{
		dispatcher.Dispatch(new SetNightGainAction(0.5f));
		
		var state = appContext.Services.GetRequiredService<IState<State>>();
		await Assert.That(state.Value.NightPlaylist.Gain).IsEqualTo(0.5f);
	}
}
