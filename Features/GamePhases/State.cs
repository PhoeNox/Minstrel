namespace Features.GamePhases;

[FeatureState]
public record State
{
	public GamePhase Phase { get; init; } = GamePhase.Day;
}

public enum GamePhase
{
	Day,
	Night,
}