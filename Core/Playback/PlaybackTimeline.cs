namespace Core.Playback;

using Core;

public static class PlaybackTimeline
{
	public const double FadeSeconds = 5;

	public static PlaybackState Play(PlaybackState state, string songId, long now) =>
		PlayAt(state, songId, offset: 0, now);

	public static PlaybackState PlayAt(PlaybackState state, string songId, double offset, long now) =>
		state with { Position = new PositionAnchor(songId, offset, AnchorTimestamp: now, IsPlaying: true) };

	public static PlaybackState Pause(PlaybackState state, long now) =>
		state with
		{
			Position = state.Position with
			{
				Offset = DerivePosition(state.Position, now),
				AnchorTimestamp = now,
				IsPlaying = false,
			},
		};

	public static PlaybackState Resume(PlaybackState state, long now) =>
		state.Position.SongId is null
			? state
			: state with { Position = state.Position with { AnchorTimestamp = now, IsPlaying = true } };

	public static PlaybackState Select(PlaybackState state, GamePhase phase, int index, Track? track, long now)
	{
		var selected = WithPhase(state, phase, playback => playback with { Cursor = index, ResumeOffset = 0 });
		if (phase != state.ActivePhase)
			return selected;

		return track is { } song ? Play(selected, song.Id, now) : selected;
	}

	public static PlaybackState SwitchPhase(PlaybackState state, Track? activeCurrent, Track? targetCurrent, long now)
	{
		var target = state.ActivePhase == GamePhase.Day ? GamePhase.Night : GamePhase.Day;
		var remembered = RememberResumeOffset(state, activeCurrent, now) with { ActivePhase = target };
		return targetCurrent is null
			? remembered with { Position = PositionAnchor.Idle }
			: PlayAt(remembered, targetCurrent.Id, state.Phase(target).ResumeOffset, now);
	}

	public static PlaybackState SetGain(PlaybackState state, GamePhase phase, double gain) =>
		WithPhase(state, phase, playback => playback with { Gain = gain });

	public static PlaybackState ReindexAfterAdd(PlaybackState state, GamePhase phase) =>
		WithPhase(state, phase, playback => playback with { Cursor = playback.Cursor ?? 0 });

	public static PlaybackState ReindexAfterMove(PlaybackState state, GamePhase phase, int oldIndex, int newIndex, int length) =>
		WithCursor(state, phase, RemapCursor(state.Cursor(phase), oldIndex, newIndex, length));

	public static PlaybackState ReindexAfterShuffle(PlaybackState state, GamePhase phase, int[] permutation) =>
		WithCursor(
			state,
			phase,
			state.Cursor(phase) is { } cursor ? Array.IndexOf(permutation, cursor) : null);

	public static PlaybackState ReindexAfterRemove(
		PlaybackState state,
		GamePhase phase,
		int index,
		int oldLength,
		Track[] entriesAfter,
		long now)
	{
		if (index < 0 || index >= oldLength)
			return state;

		var oldCursor = state.Cursor(phase);
		var updated = WithCursor(state, phase, CursorAfterRemoval(oldCursor, oldLength, index));
		if (phase != state.ActivePhase || index != oldCursor)
			return updated;

		return At(entriesAfter, updated.Cursor(phase)) is { } next
			? Play(updated, next.Id, now)
			: updated with { Position = PositionAnchor.Idle };
	}

	public static PlaybackState Tick(PlaybackState state, Track[] activeTracks, long now)
	{
		if (!state.Position.IsPlaying)
			return state;

		var elapsed = DerivePosition(state.Position, now);
		var current = At(activeTracks, state.ActiveCursor);
		return current is not null && elapsed >= current.Length
			? Advance(state, activeTracks, now)
			: state with { Position = state.Position with { Offset = elapsed, AnchorTimestamp = now } };
	}

	public static bool PlaybackJumped(PlaybackState before, PlaybackState after, long now) =>
		before.CurrentSongId != after.CurrentSongId
		|| DerivePosition(after.Position, now) < DerivePosition(before.Position, now);

	public static double DerivePosition(PositionAnchor anchor, long now) =>
		anchor.IsPlaying
			? anchor.Offset + ((now - anchor.AnchorTimestamp) / 1000.0)
			: anchor.Offset;

	private static PlaybackState Advance(PlaybackState state, Track[] activeTracks, long now)
	{
		if (state.ActiveCursor is not { } index || activeTracks.Length == 0)
			return state;

		var nextIndex = (index + 1) % activeTracks.Length;
		return Select(state, state.ActivePhase, nextIndex, activeTracks[nextIndex], now);
	}

	private static PlaybackState RememberResumeOffset(PlaybackState state, Track? activeCurrent, long now)
	{
		if (state.Position.SongId is null)
			return state;

		var heard = DerivePosition(state.Position, now) + (state.Position.IsPlaying ? FadeSeconds : 0);
		var offset = activeCurrent is null ? heard : Math.Min(heard, activeCurrent.Length);
		return WithPhase(state, state.ActivePhase, playback => playback with { ResumeOffset = offset });
	}

	private static PlaybackState WithPhase(
		PlaybackState state,
		GamePhase phase,
		Func<PhasePlayback, PhasePlayback> transform) =>
		phase == GamePhase.Day
			? state with { Day = transform(state.Day) }
			: state with { Night = transform(state.Night) };

	private static PlaybackState WithCursor(PlaybackState state, GamePhase phase, int? cursor) =>
		WithPhase(state, phase, playback => playback with { Cursor = cursor });

	private static int? RemapCursor(int? cursor, int oldIndex, int newIndex, int length)
	{
		if (cursor is not { } current || oldIndex < 0 || oldIndex >= length)
			return cursor;

		var target = Math.Clamp(newIndex, 0, length - 1);
		if (current == oldIndex)
			return target;

		var withoutMoved = current < oldIndex ? current : current - 1;
		return withoutMoved >= target ? withoutMoved + 1 : withoutMoved;
	}

	private static int? CursorAfterRemoval(int? cursor, int oldLength, int index)
	{
		if (cursor is not { } current)
			return null;

		var remaining = oldLength - 1;
		if (remaining == 0)
			return null;
		if (index < current)
			return current - 1;
		if (index > current)
			return current;
		return index < remaining ? index : 0;
	}

	private static Track? At(Track[] tracks, int? index) =>
		index is { } i && i >= 0 && i < tracks.Length ? tracks[i] : null;
}
