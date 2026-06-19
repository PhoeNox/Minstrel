namespace Backend.Sessions.Playback;

public abstract record PlaybackEvent;

// Carries the SSE frame already rendered once at broadcast time: the snapshot is identical
// for every subscriber, so the pump writes these bytes verbatim rather than re-mapping per client.
public sealed record SnapshotEvent(string Frame) : PlaybackEvent;
