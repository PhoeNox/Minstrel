namespace Features.Timer;

public record StartTimerAction(TimeSpan Duration);

public record SetTimeLeftAction(TimeSpan TimeLeft);
public record SetIsTimeRunningAction(bool IsRunning);