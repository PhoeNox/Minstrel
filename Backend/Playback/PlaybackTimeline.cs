namespace Backend.Playback;

using Core;

public sealed record PositionAnchor(string? SongId, double Offset, long AnchorTimestamp, bool IsPlaying)
{
	public static PositionAnchor Idle { get; } = new(SongId: null, Offset: 0, AnchorTimestamp: 0, IsPlaying: false);
}

public sealed record TimelineSong(string Id, double Length);

public sealed record TimelinePlaylist(TimelineSong[] Songs, int? CurrentIndex, double Gain = 1.0, double ResumeOffset = 0)
{
	public static TimelinePlaylist Empty { get; } = new([], CurrentIndex: null);

	public TimelineSong? CurrentSong =>
		CurrentIndex is { } index && index >= 0 && index < Songs.Length ? Songs[index] : null;
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
	public const double FadeSeconds = 5;

	public static TimelineState Play(TimelineState state, string songId, long now) =>
		PlayAt(state, songId, offset: 0, now);

	public static TimelineState PlayAt(TimelineState state, string songId, double offset, long now) =>
		state with { Position = new PositionAnchor(songId, offset, AnchorTimestamp: now, IsPlaying: true) };

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

	public static TimelineState SelectSong(TimelineState state, GamePhase phase, int index, long now)
	{
		var selected = WithCurrentSong(state, phase, index);
		if (phase != state.ActivePhase)
			return selected;

		return selected.ActivePlaylist.CurrentSong is { } song ? Play(selected, song.Id, now) : selected;
	}

	public static TimelineState SwitchPhase(TimelineState state, long now)
	{
		var target = state.ActivePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		var targetPlaylist = target == GamePhase.Day ? state.Day : state.Night;
		if (targetPlaylist.CurrentSong is not { } song)
			return state;

		var remembered = RememberResumeOffset(state, now) with { ActivePhase = target };
		return PlayAt(remembered, song.Id, targetPlaylist.ResumeOffset, now);
	}

	public static TimelineState MoveSong(TimelineState state, GamePhase phase, int oldIndex, int newIndex) =>
		WithPlaylist(state, phase, playlist => playlist with
		{
			Songs = Reordered(playlist.Songs, oldIndex, newIndex),
			CurrentIndex = RemapCurrentIndex(playlist, oldIndex, newIndex),
		});

	public static TimelineState SetGain(TimelineState state, GamePhase phase, double gain) =>
		WithPlaylist(state, phase, playlist => playlist with { Gain = gain });

	public static TimelineState Tick(TimelineState state, long now)
	{
		if (!state.Position.IsPlaying)
			return state;

		var elapsed = DerivePosition(state.Position, now);
		var current = state.ActivePlaylist.CurrentSong;
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
		var playlist = state.ActivePlaylist;
		if (playlist.CurrentIndex is not { } index || playlist.Songs.Length == 0)
			return state;

		var nextIndex = (index + 1) % playlist.Songs.Length;
		return SelectSong(state, state.ActivePhase, nextIndex, now);
	}

	private static TimelineState RememberResumeOffset(TimelineState state, long now)
	{
		if (state.Position.SongId is null)
			return state;

		var heard = DerivePosition(state.Position, now) + (state.Position.IsPlaying ? FadeSeconds : 0);
		var song = state.ActivePlaylist.CurrentSong;
		var offset = song is null ? heard : Math.Min(heard, song.Length);
		return WithPlaylist(state, state.ActivePhase, playlist => playlist with { ResumeOffset = offset });
	}

	private static TimelineState WithCurrentSong(TimelineState state, GamePhase phase, int index) =>
		WithPlaylist(state, phase, playlist => playlist with { CurrentIndex = index, ResumeOffset = 0 });

	private static TimelineState WithPlaylist(
		TimelineState state,
		GamePhase phase,
		Func<TimelinePlaylist, TimelinePlaylist> transform) =>
		phase == GamePhase.Day
			? state with { Day = transform(state.Day) }
			: state with { Night = transform(state.Night) };

	private static int? RemapCurrentIndex(TimelinePlaylist playlist, int oldIndex, int newIndex)
	{
		if (playlist.CurrentIndex is not { } current || oldIndex < 0 || oldIndex >= playlist.Songs.Length)
			return playlist.CurrentIndex;

		var target = Math.Clamp(newIndex, 0, playlist.Songs.Length - 1);
		if (current == oldIndex)
			return target;

		var withoutMoved = current < oldIndex ? current : current - 1;
		return withoutMoved >= target ? withoutMoved + 1 : withoutMoved;
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
