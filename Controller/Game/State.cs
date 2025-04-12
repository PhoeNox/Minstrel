namespace Controller.Game;

using Fluxor;

[FeatureState]
public class State
{
	public GamePhase GamePhase { get; set; } = GamePhase.Day;
}
