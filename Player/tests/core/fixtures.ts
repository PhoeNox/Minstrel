import * as playbackTimeline from '$shared/core/playbackTimeline';
import type { GamePhase, PlaybackState, PlaylistEntry, Rng, Track } from '$shared/core';

export function entry(id: string, length: number): PlaylistEntry {
	return { id, path: `${id}.mp3`, title: id, artist: id, length };
}

export function track(id: string, length: number): Track {
	return { id, length };
}

export function tracks(...songs: [string, number][]): Track[] {
	return songs.map(([id, length]) => ({ id, length }));
}

export interface Book {
	day: PlaylistEntry[];
	night: PlaylistEntry[];
}

export function book(day: PlaylistEntry[], night: PlaylistEntry[]): Book {
	return { day, night };
}

export function trackAt(source: Book, phase: GamePhase, index: number | null): Track | null {
	const entries = phase === 'Day' ? source.day : source.night;
	return index !== null && index >= 0 && index < entries.length
		? { id: entries[index].id, length: entries[index].length }
		: null;
}

// The C# Backend resolves both phases' current tracks from the PlaylistBook before
// calling the pure SwitchPhase; these helpers mirror that wiring for the parity tests.
export function switchPhase(source: Book, state: PlaybackState, now: number): PlaybackState {
	const target: GamePhase = state.activePhase === 'Day' ? 'Night' : 'Day';
	const activeCurrent = trackAt(source, state.activePhase, playbackTimeline.activeCursor(state));
	const targetCurrent = trackAt(source, target, playbackTimeline.cursorOf(state, target));
	return playbackTimeline.switchPhase(state, activeCurrent, targetCurrent, now);
}

export function selectIn(
	source: Book,
	state: PlaybackState,
	phase: GamePhase,
	index: number,
	now: number
): PlaybackState {
	return playbackTimeline.select(state, phase, index, trackAt(source, phase, index), now);
}

// A deterministic [0, 1) generator (mulberry32) so `shuffle` is reproducible in tests.
export function seededRng(seed: number): Rng {
	let state = seed >>> 0;
	return () => {
		state |= 0;
		state = (state + 0x6d2b79f5) | 0;
		let t = Math.imul(state ^ (state >>> 15), 1 | state);
		t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
		return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
	};
}
