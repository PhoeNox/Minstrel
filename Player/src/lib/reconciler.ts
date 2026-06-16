import { derivePosition } from '$shared/position';
import { activePlaylist, type PlaybackState } from './state';

export const FADE_SECONDS = 5;

export type AudioOperation =
	| { type: 'play'; songId: string; offset: number; gain: number }
	| { type: 'set-gain'; gain: number }
	| { type: 'stop' }
	| { type: 'prefetch'; songId: string };

export interface AudioGraph {
	leadSongId: string | null;
	leadPosition: number | null;
	leadGain: number | null;
	loadedSongIds: string[];
}

export function reconcile(desired: PlaybackState, current: AudioGraph, now: number): AudioOperation[] {
	const wanted = desired.isPlaying ? desired.currentSongId : null;
	if (wanted === null) {
		return current.leadSongId === null ? [] : [{ type: 'stop' }];
	}

	const gain = activePlaylist(desired).gain;
	const next = nextSongId(desired);
	const operations: AudioOperation[] = [];

	if (next !== null && !current.loadedSongIds.includes(next)) {
		operations.push({ type: 'prefetch', songId: next });
	}

	const anticipated = next !== null && current.leadSongId === next;
	if (current.leadSongId !== wanted && !anticipated) {
		operations.push({ type: 'play', songId: wanted, offset: derivePosition(desired.position, now), gain });
		return operations;
	}

	if (current.leadSongId === wanted && nearingEnd(desired, current, wanted, next)) {
		operations.push({ type: 'play', songId: next as string, offset: 0, gain });
		return operations;
	}

	if (gain !== current.leadGain) {
		operations.push({ type: 'set-gain', gain });
	}

	return operations;
}

function nearingEnd(
	desired: PlaybackState,
	current: AudioGraph,
	songId: string,
	next: string | null
): boolean {
	const length = songLength(desired, songId);
	return (
		next !== null &&
		current.loadedSongIds.includes(next) &&
		current.leadPosition !== null &&
		length !== null &&
		length > FADE_SECONDS &&
		current.leadPosition >= length - FADE_SECONDS
	);
}

function nextSongId(desired: PlaybackState): string | null {
	const songs = activePlaylist(desired).songs;
	if (desired.currentSongId === null || songs.length === 0) {
		return null;
	}

	const index = songs.findIndex((song) => song.id === desired.currentSongId);
	return index < 0 ? null : songs[(index + 1) % songs.length].id;
}

function songLength(desired: PlaybackState, songId: string): number | null {
	const song = activePlaylist(desired).songs.find((entry) => entry.id === songId);
	return song ? song.length : null;
}
