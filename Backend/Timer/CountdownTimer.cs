namespace Backend.Timer;

public sealed record TimerAnchor(bool Running, long AnchorTimestamp, double DurationLeftAtAnchor)
{
	public static TimerAnchor Idle { get; } = new(Running: false, AnchorTimestamp: 0, DurationLeftAtAnchor: 0);
}

public static class CountdownTimer
{
	public static TimerAnchor Start(double durationSeconds, long now) =>
		new(Running: true, AnchorTimestamp: now, DurationLeftAtAnchor: durationSeconds);

	public static double DeriveTimeLeft(TimerAnchor anchor, long now) =>
		anchor.Running
			? Math.Max(0, anchor.DurationLeftAtAnchor - ((now - anchor.AnchorTimestamp) / 1000.0))
			: anchor.DurationLeftAtAnchor;

	public static bool HasExpired(TimerAnchor anchor, long now) =>
		anchor.Running && DeriveTimeLeft(anchor, now) <= 0;
}
