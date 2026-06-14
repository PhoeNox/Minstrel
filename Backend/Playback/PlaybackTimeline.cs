namespace Backend.Playback;

using Core;

public sealed record PositionAnchor(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying)
{
	public static PositionAnchor Idle { get; } = new(SongId: null, Offset: 0, AnchorTimestamp: 0, IsPlaying: false);
}

public sealed record TimelineSong(string Id, double Length);

public sealed record TimelinePlaylist(TimelineSong[] Songs, string? CurrentSongId, double Gain = 1.0)
{
	public static TimelinePlaylist Empty { get; } = new([], CurrentSongId: null);
}

public sealed record TimelineState(
	GamePhase ActivePhase,
	TimelinePlaylist Day,
	TimelinePlaylist Night,
	PositionAnchor Position)
{
	public static TimelineState Idle { get; } =
		new(GamePhase.Day, TimelinePlaylist.Empty, TimelinePlaylist.Empty, PositionAnchor.Idle);

	public string? CurrentSongId => Position.SongId;

	public bool IsPlaying => Position.IsPlaying;

	public TimelinePlaylist ActivePlaylist => ActivePhase == GamePhase.Day ? Day : Night;
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

	public static TimelineState SelectSong(TimelineState state, GamePhase phase, string songId, long now)
	{
		var selected = WithCurrentSong(state, phase, songId);
		return phase == state.ActivePhase ? Play(selected, songId, now) : selected;
	}

	public static TimelineState SwitchPhase(TimelineState state, long now)
	{
		var target = state.ActivePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		var targetPlaylist = target == GamePhase.Day ? state.Day : state.Night;
		return targetPlaylist.CurrentSongId is { } songId
			? Play(state with { ActivePhase = target }, songId, now)
			: state;
	}

	public static TimelineState MoveSong(TimelineState state, GamePhase phase, int oldIndex, int newIndex) =>
		WithPlaylist(state, phase, playlist => playlist with { Songs = Reordered(playlist.Songs, oldIndex, newIndex) });

	public static TimelineState SetGain(TimelineState state, GamePhase phase, double gain) =>
		WithPlaylist(state, phase, playlist => playlist with { Gain = gain });

	public static TimelineState Tick(TimelineState state, long now)
	{
		if (!state.Position.IsPlaying)
			return state;

		var elapsed = DerivePosition(state.Position, now);
		var current = FindSong(state.ActivePlaylist, state.Position.SongId);
		return current is not null && elapsed >= current.Length
			? Advance(state, now)
			: state with { Position = state.Position with { Offset = elapsed, AnchorTimestamp = now } };
	}

	public static double DerivePosition(PositionAnchor anchor, long now) =>
		anchor.IsPlaying
			? anchor.Offset + ((now - anchor.AnchorTimestamp) / 1000.0)
			: anchor.Offset;

	private static TimelineState Advance(TimelineState state, long now)
	{
		var next = NextSong(state.ActivePlaylist, state.Position.SongId);
		return next is null ? state : SelectSong(state, state.ActivePhase, next.Id, now);
	}

	private static TimelineState WithCurrentSong(TimelineState state, GamePhase phase, string songId) =>
		WithPlaylist(state, phase, playlist => playlist with { CurrentSongId = songId });

	private static TimelineState WithPlaylist(
		TimelineState state,
		GamePhase phase,
		Func<TimelinePlaylist, TimelinePlaylist> transform) =>
		phase == GamePhase.Day
			? state with { Day = transform(state.Day) }
			: state with { Night = transform(state.Night) };

	private static TimelineSong? FindSong(TimelinePlaylist playlist, string? songId) =>
		Array.Find(playlist.Songs, song => song.Id == songId);

	private static TimelineSong? NextSong(TimelinePlaylist playlist, string? songId)
	{
		var index = Array.FindIndex(playlist.Songs, song => song.Id == songId);
		return index < 0 ? null : playlist.Songs[(index + 1) % playlist.Songs.Length];
	}

	private static TimelineSong[] Reordered(TimelineSong[] songs, int oldIndex, int newIndex)
	{
		if (oldIndex < 0 || oldIndex >= songs.Length)
			return songs;

		var list = songs.ToList();
		var song = list[oldIndex];
		list.RemoveAt(oldIndex);
		var target = Math.Clamp(newIndex, 0, list.Count);
		list.Insert(target, song);
		return list.ToArray();
	}
}
