import { derivePosition } from '$shared/position';
import { activePlaylist, type PlaybackState } from './state';

export type AudioOperation =
	| { type: 'fade-in'; songId: string; offset: number }
	| { type: 'fade-out'; songId: string }
	| { type: 'prefetch'; songId: string };

export interface AudioGraph {
	playingSongId: string | null;
	loadedSongIds: string[];
}

export function reconcile(desired: PlaybackState, current: AudioGraph, now: number): AudioOperation[] {
	const wanted = desired.isPlaying ? desired.currentSongId : null;
	const operations: AudioOperation[] = [];

	if (current.playingSongId !== null && current.playingSongId !== wanted) {
		operations.push({ type: 'fade-out', songId: current.playingSongId });
	}

	if (wanted !== null && wanted !== current.playingSongId) {
		operations.push({ type: 'fade-in', songId: wanted, offset: derivePosition(desired.position, now) });
	}

	const next = nextSongId(desired);
	if (next !== null && !current.loadedSongIds.includes(next)) {
		operations.push({ type: 'prefetch', songId: next });
	}

	return operations;
}

function nextSongId(desired: PlaybackState): string | null {
	const songs = activePlaylist(desired).songs;
	if (desired.currentSongId === null || songs.length === 0) {
		return null;
	}

	const index = songs.findIndex((song) => song.id === desired.currentSongId);
	return index < 0 ? null : songs[(index + 1) % songs.length].id;
}
