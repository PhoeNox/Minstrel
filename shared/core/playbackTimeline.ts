// Pure playback transforms — the TS mirror of C# `Core.Playback.PlaybackTimeline`.
// The cursor stays index-based (ADR-0005): each reindex carries the structural detail
// so the Current Entry follows its song across add/remove/move/shuffle.

import {
	IDLE_POSITION,
	type GamePhase,
	type PhasePlayback,
	type PlaybackState,
	type PositionAnchor,
	type Track
} from './types';

export const FADE_SECONDS = 5;

export function play(state: PlaybackState, songId: string, now: number): PlaybackState {
	return playAt(state, songId, 0, now);
}

export function playAt(
	state: PlaybackState,
	songId: string,
	offset: number,
	now: number
): PlaybackState {
	return { ...state, position: { songId, offset, anchorTimestamp: now, isPlaying: true } };
}

export function pause(state: PlaybackState, now: number): PlaybackState {
	return {
		...state,
		position: {
			...state.position,
			offset: derivePosition(state.position, now),
			anchorTimestamp: now,
			isPlaying: false
		}
	};
}

export function resume(state: PlaybackState, now: number): PlaybackState {
	return state.position.songId === null
		? state
		: { ...state, position: { ...state.position, anchorTimestamp: now, isPlaying: true } };
}

export function select(
	state: PlaybackState,
	phase: GamePhase,
	index: number,
	track: Track | null,
	now: number
): PlaybackState {
	const selected = withPhase(state, phase, (playback) => ({
		...playback,
		cursor: index,
		resumeOffset: 0
	}));
	if (phase !== state.activePhase) {
		return selected;
	}
	return track !== null ? play(selected, track.id, now) : selected;
}

export function switchPhase(
	state: PlaybackState,
	activeCurrentTrack: Track | null,
	targetCurrentTrack: Track | null,
	now: number
): PlaybackState {
	const target: GamePhase = state.activePhase === 'Day' ? 'Night' : 'Day';
	const remembered: PlaybackState = {
		...rememberResumeOffset(state, activeCurrentTrack, now),
		activePhase: target
	};
	return targetCurrentTrack === null
		? { ...remembered, position: IDLE_POSITION }
		: playAt(remembered, targetCurrentTrack.id, phaseOf(state, target).resumeOffset, now);
}

export function setGain(state: PlaybackState, phase: GamePhase, gain: number): PlaybackState {
	return withPhase(state, phase, (playback) => ({ ...playback, gain }));
}

export function reindexAfterAdd(state: PlaybackState, phase: GamePhase): PlaybackState {
	return withPhase(state, phase, (playback) => ({ ...playback, cursor: playback.cursor ?? 0 }));
}

export function reindexAfterMove(
	state: PlaybackState,
	phase: GamePhase,
	oldIndex: number,
	newIndex: number,
	length: number
): PlaybackState {
	return withCursor(state, phase, remapCursor(cursorOf(state, phase), oldIndex, newIndex, length));
}

export function reindexAfterShuffle(
	state: PlaybackState,
	phase: GamePhase,
	permutation: number[]
): PlaybackState {
	const cursor = cursorOf(state, phase);
	return withCursor(state, phase, cursor === null ? null : permutation.indexOf(cursor));
}

export function reindexAfterRemove(
	state: PlaybackState,
	phase: GamePhase,
	index: number,
	oldLength: number,
	entriesAfter: Track[],
	now: number
): PlaybackState {
	if (index < 0 || index >= oldLength) {
		return state;
	}
	const oldCursor = cursorOf(state, phase);
	const updated = withCursor(state, phase, cursorAfterRemoval(oldCursor, oldLength, index));
	if (phase !== state.activePhase || index !== oldCursor) {
		return updated;
	}
	const next = at(entriesAfter, cursorOf(updated, phase));
	return next !== null ? play(updated, next.id, now) : { ...updated, position: IDLE_POSITION };
}

export function tick(state: PlaybackState, activeTracks: Track[], now: number): PlaybackState {
	if (!state.position.isPlaying) {
		return state;
	}
	const elapsed = derivePosition(state.position, now);
	const current = at(activeTracks, activeCursor(state));
	return current !== null && elapsed >= current.length
		? advance(state, activeTracks, now)
		: { ...state, position: { ...state.position, offset: elapsed, anchorTimestamp: now } };
}

export function advance(state: PlaybackState, activeTracks: Track[], now: number): PlaybackState {
	const index = activeCursor(state);
	if (index === null || activeTracks.length === 0) {
		return state;
	}
	const nextIndex = (index + 1) % activeTracks.length;
	return select(state, state.activePhase, nextIndex, activeTracks[nextIndex], now);
}

export function playbackJumped(
	before: PlaybackState,
	after: PlaybackState,
	now: number
): boolean {
	return (
		before.position.songId !== after.position.songId ||
		derivePosition(after.position, now) < derivePosition(before.position, now)
	);
}

export function derivePosition(anchor: PositionAnchor, now: number): number {
	return anchor.isPlaying ? anchor.offset + (now - anchor.anchorTimestamp) / 1000 : anchor.offset;
}

export function phaseOf(state: PlaybackState, phase: GamePhase): PhasePlayback {
	return phase === 'Day' ? state.day : state.night;
}

export function cursorOf(state: PlaybackState, phase: GamePhase): number | null {
	return phaseOf(state, phase).cursor;
}

export function activeCursor(state: PlaybackState): number | null {
	return phaseOf(state, state.activePhase).cursor;
}

function rememberResumeOffset(
	state: PlaybackState,
	activeCurrentTrack: Track | null,
	now: number
): PlaybackState {
	if (state.position.songId === null) {
		return state;
	}
	const heard = derivePosition(state.position, now) + (state.position.isPlaying ? FADE_SECONDS : 0);
	const offset = activeCurrentTrack === null ? heard : Math.min(heard, activeCurrentTrack.length);
	return withPhase(state, state.activePhase, (playback) => ({ ...playback, resumeOffset: offset }));
}

function withPhase(
	state: PlaybackState,
	phase: GamePhase,
	transform: (playback: PhasePlayback) => PhasePlayback
): PlaybackState {
	return phase === 'Day'
		? { ...state, day: transform(state.day) }
		: { ...state, night: transform(state.night) };
}

function withCursor(state: PlaybackState, phase: GamePhase, cursor: number | null): PlaybackState {
	return withPhase(state, phase, (playback) => ({ ...playback, cursor }));
}

function remapCursor(
	cursor: number | null,
	oldIndex: number,
	newIndex: number,
	length: number
): number | null {
	if (cursor === null || oldIndex < 0 || oldIndex >= length) {
		return cursor;
	}
	const target = clamp(newIndex, 0, length - 1);
	if (cursor === oldIndex) {
		return target;
	}
	const withoutMoved = cursor < oldIndex ? cursor : cursor - 1;
	return withoutMoved >= target ? withoutMoved + 1 : withoutMoved;
}

function cursorAfterRemoval(cursor: number | null, oldLength: number, index: number): number | null {
	if (cursor === null) {
		return null;
	}
	const remaining = oldLength - 1;
	if (remaining === 0) {
		return null;
	}
	if (index < cursor) {
		return cursor - 1;
	}
	if (index > cursor) {
		return cursor;
	}
	return index < remaining ? index : 0;
}

function at(tracks: Track[], index: number | null): Track | null {
	return index !== null && index >= 0 && index < tracks.length ? tracks[index] : null;
}

function clamp(value: number, low: number, high: number): number {
	return Math.min(Math.max(value, low), high);
}
