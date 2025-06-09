namespace Features.Timer;

[FeatureState]
public record State
{
	public bool IsRunning { get; init; }
	public TimeSpan TimeLeft { get; init; }
	public float GongGain { get; init; } = 5;
}
