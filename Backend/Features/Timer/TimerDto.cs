namespace Backend.Features.Timer;

public sealed record TimerDto(bool Running, long AnchorTimestamp, double DurationLeftAtAnchor);
