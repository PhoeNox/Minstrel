import { derivePosition } from '$shared/position';
import { activePlaylist, type PlaybackState } from './state';

export type AudioOperation =
	| { type: 'fade-in'; songId: string; offset: number; gain: number }
	| { type: 'fade-out'; songId: string }
	| { type: 'set-gain'; songId: string; gain: number }
	| { type: 'prefetch'; songId: string };

export interface AudioGraph {
	playingSongId: string | null;
	playingPosition: number | null;
	loadedSongIds: string[];
	gain: number;
}

const RESTART_THRESHOLD_SECONDS = 1;

export function reconcile(desired: PlaybackState, current: AudioGraph, now: number): AudioOperation[] {
	const wanted = desired.isPlaying ? desired.currentSongId : null;
	const gain = activePlaylist(desired).gain;
	const position = derivePosition(desired.position, now);
	const operations: AudioOperation[] = [];

	const rewound =
		wanted !== null &&
		wanted === current.playingSongId &&
		current.playingPosition !== null &&
		current.playingPosition - position > RESTART_THRESHOLD_SECONDS;

	if (current.playingSongId !== null && (current.playingSongId !== wanted || rewound)) {
		operations.push({ type: 'fade-out', songId: current.playingSongId });
	}

	if (wanted !== null && (wanted !== current.playingSongId || rewound)) {
		operations.push({ type: 'fade-in', songId: wanted, offset: position, gain });
	} else if (wanted !== null && gain !== current.gain) {
		operations.push({ type: 'set-gain', songId: wanted, gain });
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
