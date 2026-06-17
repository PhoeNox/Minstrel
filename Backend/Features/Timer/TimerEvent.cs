namespace Backend.Features.Timer;

public abstract record TimerEvent;

public sealed record TimerSnapshotEvent(TimerDto? Timer) : TimerEvent;

public sealed record GongEvent : TimerEvent;
