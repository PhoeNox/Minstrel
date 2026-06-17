namespace Backend.Sessions;

using Core.Timer;

public abstract record TimerEvent;

public sealed record TimerSnapshotEvent(TimerAnchor Timer) : TimerEvent;

public sealed record GongEvent : TimerEvent;
