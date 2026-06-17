namespace Backend.Features.Playback;

using Core;

public sealed record PositionAnchor(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying)
{
	public static PositionAnchor Idle { get; } = new(SongId: null, Offset: 0, AnchorTimestamp: 0, IsPlaying: false);
}

public sealed record Track(string Id, double Length);

public sealed record PhasePlayback(int? Cursor = null, double Gain = 1.0, double ResumeOffset = 0)
{
	public static PhasePlayback Idle { get; } = new();
}

public sealed record PlaybackState(
	GamePhase ActivePhase,
	PhasePlayback Day,
	PhasePlayback Night,
	PositionAnchor Position)
{
	public static PlaybackState Idle { get; } =
		new(GamePhase.Day, PhasePlayback.Idle, PhasePlayback.Idle, PositionAnchor.Idle);

	public string? CurrentSongId => Position.SongId;

	public bool IsPlaying => Position.IsPlaying;

	public PhasePlayback Phase(GamePhase phase) => phase == GamePhase.Day ? Day : Night;

	public int? Cursor(GamePhase phase) => Phase(phase).Cursor;

	public int? ActiveCursor => Phase(ActivePhase).Cursor;
}
