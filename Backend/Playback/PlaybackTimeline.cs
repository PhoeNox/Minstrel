namespace Backend.Playback;

public sealed record PositionAnchor(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying)
{
	public static PositionAnchor Idle { get; } = new(SongId: null, Offset: 0, AnchorTimestamp: 0, IsPlaying: false);
}

public sealed record TimelineState(PositionAnchor Position)
{
	public static TimelineState Idle { get; } = new(PositionAnchor.Idle);

	public string? CurrentSongId => Position.SongId;

	public bool IsPlaying => Position.IsPlaying;
}

public static class PlaybackTimeline
{
	public static TimelineState Play(TimelineState state, string songId, long now) =>
		state with { Position = new PositionAnchor(songId, Offset: 0, AnchorTimestamp: now, IsPlaying: true) };

	public static TimelineState Pause(TimelineState state, long now) =>
		state with
		{
			Position = state.Position with
			{
				Offset = DerivePosition(state.Position, now),
				AnchorTimestamp = now,
				IsPlaying = false,
			},
		};

	public static TimelineState Resume(TimelineState state, long now) =>
		state.Position.SongId is null
			? state
			: state with { Position = state.Position with { AnchorTimestamp = now, IsPlaying = true } };

	public static TimelineState Tick(TimelineState state, long now) =>
		state.Position.IsPlaying
			? state with
			{
				Position = state.Position with { Offset = DerivePosition(state.Position, now), AnchorTimestamp = now },
			}
			: state;

	public static double DerivePosition(PositionAnchor anchor, long now) =>
		anchor.IsPlaying
			? anchor.Offset + ((now - anchor.AnchorTimestamp) / 1000.0)
			: anchor.Offset;
}
