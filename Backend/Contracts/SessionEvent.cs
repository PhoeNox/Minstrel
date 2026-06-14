namespace Backend.Contracts;

public abstract record SessionEvent;

public sealed record SnapshotEvent(StateSnapshot Snapshot) : SessionEvent;

public sealed record GongEvent : SessionEvent;
