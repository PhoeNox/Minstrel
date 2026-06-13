namespace Backend.Playback;

public sealed record TimelineState(string? CurrentSongId, bool IsPlaying)
{
	public static TimelineState Idle { get; } = new(CurrentSongId: null, IsPlaying: false);
}

public static class PlaybackTimeline
{
	public static TimelineState Play(TimelineState state, string songId) =>
		state with { CurrentSongId = songId, IsPlaying = true };
}
